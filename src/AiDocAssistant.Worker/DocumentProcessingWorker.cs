using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Application.Interfaces;
using AiDocAssistant.Domain.Entities;
using AiDocAssistant.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AiDocAssistant.Worker
{
    public sealed class DocumentProcessingWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<DocumentProcessingWorker> _logger;

        public DocumentProcessingWorker(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<DocumentProcessingWorker> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Doküman Vektörleştirme Arka Plan Servisi başlatıldı. Kuyruk izleniyor...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var repository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
                        var parser = scope.ServiceProvider.GetRequiredService<IDocumentParser>();
                        var chunker = scope.ServiceProvider.GetRequiredService<ITextChunker>();
                        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
                        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

                        // 1. Veritabanından sıradaki ilk "Processing" durumundaki dökümanı bul
                        var allDocs = await repository.GetAllAsync(stoppingToken);
                        var document = allDocs.FirstOrDefault(d => d.Status == DocumentStatus.Processing && !string.IsNullOrEmpty(d.StoragePath));

                        if (document != null)
                        {
                            _logger.LogInformation("İşlenecek doküman bulundu. ID: {DocumentId}, Dosya: {FileName}", document.Id, document.FileName);

                            try
                            {
                                // 2. Dosya akışını oku ve metni ayıkla
                                using (var stream = await fileStorage.GetFileStreamAsync(document.StoragePath!))
                                {
                                    var text = await parser.ExtractTextAsync(stream, stoppingToken);
                                    if (string.IsNullOrWhiteSpace(text))
                                    {
                                        throw new InvalidOperationException("Dokümandan hiçbir metin okunamadı.");
                                    }

                                    // 3. Metni parçalara böl (max 1000 karakter, 200 overlap)
                                    var chunks = chunker.Split(text, maxChunkSize: 1000, overlapSize: 200);

                                    // 4. Her parça için vektör üretip dökümana ekle
                                    for (int i = 0; i < chunks.Count; i++)
                                    {
                                        var chunkContent = chunks[i];
                                        var embedding = await embeddingService.GenerateEmbeddingAsync(chunkContent, stoppingToken);
                                        document.AddChunk(chunkContent, order: i + 1, embedding: embedding);
                                    }
                                }

                                // 5. Dosyayı ortak diskten sil, storagePath'i temizle ve durumunu completed olarak kaydet
                                try
                                {
                                    await fileStorage.DeleteFileAsync(document.StoragePath!);
                                }
                                catch (Exception cleanupEx)
                                {
                                    _logger.LogWarning(cleanupEx, "Geçici dosya silinirken hata oluştu: {FilePath}", document.StoragePath);
                                }

                                document.ClearStoragePath();
                                document.MarkAsCompleted();
                                await repository.SaveChangesAsync(stoppingToken);

                                _logger.LogInformation("Doküman başarıyla işlendi ve vektör veritabanına kaydedildi. ID: {DocumentId}", document.Id);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Doküman işlenirken hata oluştu. ID: {DocumentId}", document.Id);
                                
                                // Hata durumunda geçici dökümanı silmeye çalış
                                try
                                {
                                    await fileStorage.DeleteFileAsync(document.StoragePath!);
                                }
                                catch {}

                                document.ClearStoragePath();
                                document.MarkAsFailed(ex.Message);
                                await repository.SaveChangesAsync(stoppingToken);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Kuyruk kontrolü sırasında beklenmeyen bir hata oluştu.");
                }

                // Her 2 saniyede bir veritabanı kontrolü yap
                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}

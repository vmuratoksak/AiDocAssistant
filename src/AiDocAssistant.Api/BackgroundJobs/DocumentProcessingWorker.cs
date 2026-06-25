using System;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Application.Interfaces;
using AiDocAssistant.Application.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace AiDocAssistant.Api.BackgroundJobs
{
    public sealed class DocumentProcessingWorker : BackgroundService
    {
        private readonly IDocumentQueue _queue;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public DocumentProcessingWorker(
            IDocumentQueue queue,
            IServiceScopeFactory serviceScopeFactory)
        {
            _queue = queue;
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                DocumentQueueItem? item = null;
                try
                {
                    // Kuyruktan yeni bir iş al (İş gelene kadar bekler)
                    item = await _queue.DequeueAsync(stoppingToken);

                    // Scoped servisleri çözmek için geçici bir Scope oluşturuyoruz
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var repository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
                        var parser = scope.ServiceProvider.GetRequiredService<IDocumentParser>();
                        var chunker = scope.ServiceProvider.GetRequiredService<ITextChunker>();
                        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
                        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DocumentProcessingWorker>>();

                        logger.LogInformation("Doküman işleme başladı. ID: {DocumentId}, Dosya: {FileName}", item.DocumentId, item.FileName);

                        var document = await repository.GetByIdAsync(item.DocumentId, stoppingToken);
                        if (document == null)
                        {
                            logger.LogWarning("Doküman veritabanında bulunamadı. ID: {DocumentId}", item.DocumentId);
                            continue;
                        }

                        try
                        {
                            // 1. Dosya akışını al ve metni çıkar
                            using (var stream = await fileStorage.GetFileStreamAsync(item.FilePath))
                            {
                                var text = await parser.ExtractTextAsync(stream, stoppingToken);
                                if (string.IsNullOrWhiteSpace(text))
                                {
                                    throw new InvalidOperationException("Dokümandan hiçbir metin okunamadı.");
                                }

                                // 2. Üst üste binen (overlap) parçalara böl (max 1000 karakter, 200 overlap)
                                var chunks = chunker.Split(text, maxChunkSize: 1000, overlapSize: 200);

                                // 3. Her parça için vektör üretip dokümana ekle
                                for (int i = 0; i < chunks.Count; i++)
                                {
                                    var chunkContent = chunks[i];
                                    var embedding = await embeddingService.GenerateEmbeddingAsync(chunkContent, stoppingToken);
                                    document.AddChunk(chunkContent, order: i + 1, embedding: new Vector(embedding));
                                }
                            }

                            // 4. İşlem başarıyla tamamlandı olarak işaretle
                            document.MarkAsCompleted();
                            await repository.SaveChangesAsync(stoppingToken);

                            logger.LogInformation("Doküman başarıyla işlendi ve vektör veritabanına kaydedildi. ID: {DocumentId}", item.DocumentId);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Doküman işlenirken hata oluştu. ID: {DocumentId}", item.DocumentId);
                            document.MarkAsFailed(ex.Message);
                            await repository.SaveChangesAsync(stoppingToken);
                        }
                        finally
                        {
                            // Disk üzerindeki geçici yüklenen dosyayı temizle
                            try
                            {
                                await fileStorage.DeleteFileAsync(item.FilePath);
                            }
                            catch (Exception cleanupEx)
                            {
                                logger.LogWarning(cleanupEx, "Geçici dosya silinirken hata oluştu: {FilePath}", item.FilePath);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Genel kuyruk hataları
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}

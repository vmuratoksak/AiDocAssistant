using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Application.Interfaces;
using AiDocAssistant.Application.Models;
using AiDocAssistant.Domain.Entities;

namespace AiDocAssistant.Application.UseCases
{
    public sealed class UploadDocumentUseCase
    {
        private readonly IFileStorage _fileStorage;
        private readonly IDocumentRepository _documentRepository;
        private readonly IDocumentQueue _documentQueue;

        public UploadDocumentUseCase(
            IFileStorage fileStorage,
            IDocumentRepository documentRepository,
            IDocumentQueue documentQueue)
        {
            _fileStorage = fileStorage;
            _documentRepository = documentRepository;
            _documentQueue = documentQueue;
        }

        public async Task<Guid> ExecuteAsync(
            string fileName,
            string contentType,
            Stream fileStream,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Dosya adı gereklidir.", nameof(fileName));
            if (fileStream == null || fileStream.Length == 0)
                throw new ArgumentException("Dosya içeriği boş olamaz.", nameof(fileStream));

            // 1. Dosyayı geçici disk depolamasına kaydet
            var tempFilePath = await _fileStorage.SaveFileAsync(fileName, fileStream, cancellationToken);

            // 2. Veritabanında ilk "Processing" (İşleniyor) durumundaki kaydı oluştur
            var document = new Document(fileName, contentType);
            await _documentRepository.AddAsync(document, cancellationToken);
            await _documentRepository.SaveChangesAsync(cancellationToken);

            // 3. İş parçacığını arka plan işçisine göndermek üzere kuyruğa ekle
            var queueItem = new DocumentQueueItem(document.Id, tempFilePath, fileName, contentType);
            await _documentQueue.EnqueueAsync(queueItem);

            return document.Id;
        }
    }
}

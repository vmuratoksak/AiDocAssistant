using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Application.Interfaces;
using AiDocAssistant.Domain.Entities;

namespace AiDocAssistant.Application.UseCases
{
    public sealed class UploadDocumentUseCase
    {
        private readonly IFileStorage _fileStorage;
        private readonly IDocumentRepository _documentRepository;

        public UploadDocumentUseCase(
            IFileStorage fileStorage,
            IDocumentRepository documentRepository)
        {
            _fileStorage = fileStorage;
            _documentRepository = documentRepository;
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

            // 1. Dosyayı geçici ortak disk depolamasına kaydet
            var tempFilePath = await _fileStorage.SaveFileAsync(fileName, fileStream, cancellationToken);

            // 2. Veritabanında ilk "Processing" durumundaki kaydı ve dosya yolunu oluştur
            var document = new Document(fileName, contentType, tempFilePath);
            await _documentRepository.AddAsync(document, cancellationToken);
            await _documentRepository.SaveChangesAsync(cancellationToken);

            return document.Id;
        }
    }
}

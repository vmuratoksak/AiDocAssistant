using System;

namespace AiDocAssistant.Application.Models
{
    public sealed class DocumentQueueItem
    {
        public Guid DocumentId { get; }
        public string FilePath { get; }
        public string FileName { get; }
        public string ContentType { get; }

        public DocumentQueueItem(Guid documentId, string filePath, string fileName, string contentType)
        {
            DocumentId = documentId;
            FilePath = string.IsNullOrWhiteSpace(filePath) ? throw new ArgumentException("File path is required.", nameof(filePath)) : filePath;
            FileName = string.IsNullOrWhiteSpace(fileName) ? throw new ArgumentException("File name is required.", nameof(fileName)) : fileName;
            ContentType = contentType;
        }
    }
}

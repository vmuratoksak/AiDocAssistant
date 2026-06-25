using System;
using System.Collections.Generic;
using AiDocAssistant.Domain.Enums;

namespace AiDocAssistant.Domain.Entities
{
    public sealed class Document
    {
        public Guid Id { get; private set; }
        public string FileName { get; private set; } = null!;
        public string ContentType { get; private set; } = null!;
        public DateTime UploadedAt { get; private set; }
        public string? StoragePath { get; private set; }
        
        // Dokümanın işlenme durumunu temsil eden alan: Processing, Completed, Failed
        public DocumentStatus Status { get; private set; } = DocumentStatus.Processing;
        public string? ErrorMessage { get; private set; }

        private readonly List<DocumentChunk> _chunks = new();
        public IReadOnlyCollection<DocumentChunk> Chunks => _chunks;

        private Document() { }

        public Document(string fileName, string contentType, string storagePath)
        {
            Id = Guid.NewGuid();
            FileName = string.IsNullOrWhiteSpace(fileName)
                ? throw new ArgumentException("File name is required.")
                : fileName;
            ContentType = contentType;
            StoragePath = string.IsNullOrWhiteSpace(storagePath)
                ? throw new ArgumentException("Storage path is required.")
                : storagePath;
            UploadedAt = DateTime.UtcNow;
            Status = DocumentStatus.Processing;
        }

        public void AddChunk(string content, int order, float[] embedding)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Chunk content cannot be empty.");

            _chunks.Add(new DocumentChunk(Id, content, order, embedding));
        }

        public void ClearStoragePath()
        {
            StoragePath = null;
        }

        public void MarkAsCompleted()
        {
            Status = DocumentStatus.Completed;
            ErrorMessage = null;
        }

        public void MarkAsFailed(string errorMessage)
        {
            Status = DocumentStatus.Failed;
            ErrorMessage = errorMessage;
        }
    }
}

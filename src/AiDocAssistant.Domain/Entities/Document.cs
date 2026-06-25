using System;
using System.Collections.Generic;
using Pgvector;

namespace AiDocAssistant.Domain.Entities
{
    public sealed class Document
    {
        public Guid Id { get; private set; }
        public string FileName { get; private set; }
        public string ContentType { get; private set; }
        public DateTime UploadedAt { get; private set; }
        
        // Dokümanın işlenme durumunu temsil eden alan: "Processing", "Completed", "Failed"
        public string Status { get; private set; } = "Processing";
        public string? ErrorMessage { get; private set; }

        private readonly List<DocumentChunk> _chunks = new();
        public IReadOnlyCollection<DocumentChunk> Chunks => _chunks;

        private Document() { }

        public Document(string fileName, string contentType)
        {
            Id = Guid.NewGuid();
            FileName = string.IsNullOrWhiteSpace(fileName)
                ? throw new ArgumentException("File name is required.")
                : fileName;
            ContentType = contentType;
            UploadedAt = DateTime.UtcNow;
            Status = "Processing";
        }

        public void AddChunk(string content, int order, Vector embedding)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Chunk content cannot be empty.");

            _chunks.Add(new DocumentChunk(Id, content, order, embedding));
        }

        public void MarkAsCompleted()
        {
            Status = "Completed";
            ErrorMessage = null;
        }

        public void MarkAsFailed(string errorMessage)
        {
            Status = "Failed";
            ErrorMessage = errorMessage;
        }
    }
}

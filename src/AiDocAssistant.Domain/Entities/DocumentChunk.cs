using System;
using Pgvector;

namespace AiDocAssistant.Domain.Entities
{
    public sealed class DocumentChunk
    {
        public Guid Id { get; private set; }
        public Guid DocumentId { get; private set; }
        public string Content { get; private set; }
        public int Order { get; private set; }
        
        // C# içindeki Pgvector.Vector tipi, pgvector veritabanındaki vector sütununa doğrudan eşlenir.
        public Vector Embedding { get; private set; }

        private DocumentChunk() { }

        public DocumentChunk(Guid documentId, string content, int order, Vector embedding)
        {
            Id = Guid.NewGuid();
            DocumentId = documentId;
            Content = string.IsNullOrWhiteSpace(content)
                ? throw new ArgumentException("Chunk content cannot be empty.")
                : content;
            Order = order;
            Embedding = embedding ?? throw new ArgumentNullException(nameof(embedding));
        }
    }
}

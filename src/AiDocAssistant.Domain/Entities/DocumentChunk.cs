using System;

namespace AiDocAssistant.Domain.Entities
{
    public sealed class DocumentChunk
    {
        public Guid Id { get; private set; }
        public Guid DocumentId { get; private set; }
        public string Content { get; private set; } = null!;
        public int Order { get; private set; }
        
        // Vektör verileri float dizisi olarak saklanır (Altyapı bağımsızlığı için)
        public float[] Embedding { get; private set; } = null!;

        private DocumentChunk() { }

        public DocumentChunk(Guid documentId, string content, int order, float[] embedding)
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

using System;

namespace AiDocAssistant.Application.DTOs
{
    public sealed class DocumentChunkDto
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public int Order { get; set; }
        public string Content { get; set; } = null!;
        public int EmbeddingDimension { get; set; }
    }
}

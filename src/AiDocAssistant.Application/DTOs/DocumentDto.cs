using System;

namespace AiDocAssistant.Application.DTOs
{
    public sealed class DocumentDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public DateTime UploadedAt { get; set; }
        public string Status { get; set; } = null!;
        public string? ErrorMessage { get; set; }
        public int ChunkCount { get; set; }
    }
}

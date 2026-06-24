using System;
using System.Collections.Generic;
using AiDocAssistant.Application.Interfaces;

namespace AiDocAssistant.Infrastructure.AI
{
    public class TextChunker : ITextChunker
    {
        public IReadOnlyList<string> Split(string text, int maxChunkSize, int overlapSize)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            if (maxChunkSize <= 0)
                throw new ArgumentException("Max chunk size must be greater than zero.", nameof(maxChunkSize));

            if (overlapSize < 0 || overlapSize >= maxChunkSize)
                throw new ArgumentException("Overlap size must be non-negative and less than max chunk size.", nameof(overlapSize));

            var chunks = new List<string>();
            int index = 0;
            int textLength = text.Length;

            while (index < textLength)
            {
                int length = Math.Min(maxChunkSize, textLength - index);
                var chunk = text.Substring(index, length);
                chunks.Add(chunk);

                // Overlap'i hesaba katarak sonraki indeks adımını hesapla
                int step = maxChunkSize - overlapSize;
                if (step <= 0)
                    break;

                index += step;
            }

            return chunks;
        }
    }
}

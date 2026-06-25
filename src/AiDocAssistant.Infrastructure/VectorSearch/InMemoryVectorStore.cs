using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Application.Interfaces;
using AiDocAssistant.Domain.Entities;
using AiDocAssistant.Domain.Enums;

namespace AiDocAssistant.Infrastructure.VectorSearch
{
    public class InMemoryVectorStore : IVectorStore
    {
        public Task<IReadOnlyList<DocumentChunk>> SearchRelevantChunksAsync(
            float[] queryEmbedding,
            int topK,
            Guid? documentId,
            CancellationToken cancellationToken)
        {
            lock (Persistence.InMemoryDocumentRepository.Documents)
            {
                var docId = documentId;
                if (!docId.HasValue)
                {
                    // Default to the latest completed document
                    docId = Persistence.InMemoryDocumentRepository.Documents
                        .Where(d => d.Status == DocumentStatus.Completed)
                        .OrderByDescending(d => d.UploadedAt)
                        .Select(d => d.Id)
                        .FirstOrDefault();
                }

                var query = Persistence.InMemoryDocumentRepository.Documents
                    .Where(d => d.Status == DocumentStatus.Completed);

                if (docId.HasValue && docId.Value != Guid.Empty)
                {
                    query = query.Where(d => d.Id == docId.Value);
                }

                var allChunks = query
                    .SelectMany(d => d.Chunks)
                    .ToList();

                if (!allChunks.Any())
                    return Task.FromResult<IReadOnlyList<DocumentChunk>>(Array.Empty<DocumentChunk>());

                // Bellekte C# ile Cosine Similarity (Açısal Benzerlik) hesaplama
                // Cosine Similarity = (A . B) / (||A|| * ||B||)
                // Cosine Distance = 1 - Cosine Similarity (Küçük mesafe = Yüksek benzerlik)
                var sortedChunks = allChunks
                    .Select(chunk => new
                    {
                        Chunk = chunk,
                        Distance = CalculateCosineDistance(chunk.Embedding, queryEmbedding)
                    })
                    .OrderBy(x => x.Distance)
                    .Take(topK)
                    .Select(x => x.Chunk)
                    .ToList();

                return Task.FromResult<IReadOnlyList<DocumentChunk>>(sortedChunks);
            }
        }

        private double CalculateCosineDistance(float[] vectorA, float[] vectorB)
        {
            if (vectorA.Length != vectorB.Length)
                return 1.0;

            double dotProduct = 0;
            double normA = 0;
            double normB = 0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                normA += vectorA[i] * vectorA[i];
                normB += vectorB[i] * vectorB[i];
            }

            if (normA == 0 || normB == 0)
                return 1.0;

            double similarity = dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
            return 1.0 - similarity;
        }
    }
}

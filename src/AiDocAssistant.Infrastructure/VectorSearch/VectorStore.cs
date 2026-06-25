using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Application.Interfaces;
using AiDocAssistant.Domain.Entities;
using AiDocAssistant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore; // CosineDistance extension metodu için gerekli

namespace AiDocAssistant.Infrastructure.VectorSearch
{
    public class VectorStore : IVectorStore
    {
        private readonly AppDbContext _context;

        public VectorStore(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<DocumentChunk>> SearchRelevantChunksAsync(
            float[] queryEmbedding,
            int topK,
            System.Guid? documentId,
            CancellationToken cancellationToken)
        {
            var queryVector = new Vector(queryEmbedding);
            var query = _context.DocumentChunks.AsQueryable();

            if (documentId.HasValue)
            {
                query = query.Where(c => c.DocumentId == documentId.Value);
            }
            else
            {
                // Default to the latest completed document if no documentId is specified
                var latestDocId = await _context.Documents
                    .Where(d => d.Status == "Completed")
                    .OrderByDescending(d => d.UploadedAt)
                    .Select(d => d.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (latestDocId != System.Guid.Empty)
                {
                    query = query.Where(c => c.DocumentId == latestDocId);
                }
                else
                {
                    query = query.Where(c => _context.Documents.Any(d => d.Id == c.DocumentId && d.Status == "Completed"));
                }
            }

            var chunks = await query
                .OrderBy(c => c.Embedding.CosineDistance(queryVector))
                .Take(topK)
                .ToListAsync(cancellationToken);

            return chunks;
        }
    }
}

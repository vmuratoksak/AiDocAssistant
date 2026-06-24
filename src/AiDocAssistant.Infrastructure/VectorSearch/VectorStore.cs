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
            CancellationToken cancellationToken)
        {
            // pgvector CosineDistance metodu ile cosine mesafesini hesaplayıp en yakınları OrderBy ile küçükten büyüğe sıralar.
            // float[] tipi için Pgvector.EntityFrameworkCore içindeki CosineDistance uzantı metodu kullanılır.
            var queryVector = new Vector(queryEmbedding);
            var chunks = await _context.DocumentChunks
                .OrderBy(c => c.Embedding.CosineDistance(queryVector))
                .Take(topK)
                .ToListAsync(cancellationToken);

            return chunks;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Application.Interfaces;
using AiDocAssistant.Domain.Entities;
using AiDocAssistant.Domain.Enums;
using AiDocAssistant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
            var queryVector = new Pgvector.Vector(queryEmbedding);
            System.Guid targetDocId = System.Guid.Empty;

            if (documentId.HasValue)
            {
                targetDocId = documentId.Value;
            }
            else
            {
                // Default to the latest completed document if no documentId is specified
                targetDocId = await _context.Documents
                    .Where(d => d.Status == DocumentStatus.Completed)
                    .OrderByDescending(d => d.UploadedAt)
                    .Select(d => d.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            List<DocumentChunk> chunks;
            if (targetDocId != System.Guid.Empty)
            {
                // PostgreSQL'de pgvector ile Cosine Similarity araması <=> operatörü ile gerçekleştirilir.
                // Raw SQL kullanımı, veritabanına özgü Pgvector.Vector nesnesi ve CosineDistance metodunun LINQ ağacında hata vermesini önler.
                chunks = await _context.DocumentChunks
                    .FromSqlRaw("SELECT * FROM \"DocumentChunks\" WHERE \"DocumentId\" = {0} ORDER BY \"Embedding\" <=> {1} LIMIT {2}", 
                        targetDocId, queryVector, topK)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                // Henüz işlenmiş tüm dokümanlar içinden benzerlik sorgusu yap
                chunks = await _context.DocumentChunks
                    .FromSqlRaw("SELECT c.* FROM \"DocumentChunks\" c INNER JOIN \"Documents\" d ON c.\"DocumentId\" = d.\"Id\" WHERE d.\"Status\" = 'Completed' ORDER BY c.\"Embedding\" <=> {0} LIMIT {1}", 
                        queryVector, topK)
                    .ToListAsync(cancellationToken);
            }

            return chunks;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Application.Interfaces;
using AiDocAssistant.Domain.Entities;

namespace AiDocAssistant.Infrastructure.Persistence
{
    public class InMemoryDocumentRepository : IDocumentRepository
    {
        public static readonly List<Document> Documents = new();

        public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            lock (Documents)
            {
                var doc = Documents.FirstOrDefault(d => d.Id == id);
                return Task.FromResult(doc);
            }
        }

        public Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken)
        {
            lock (Documents)
            {
                var docs = Documents.OrderByDescending(d => d.UploadedAt).ToList();
                return Task.FromResult<IReadOnlyList<Document>>(docs);
            }
        }

        public Task AddAsync(Document document, CancellationToken cancellationToken)
        {
            lock (Documents)
            {
                Documents.Add(document);
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Document document, CancellationToken cancellationToken)
        {
            lock (Documents)
            {
                Documents.Remove(document);
            }
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

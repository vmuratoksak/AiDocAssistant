using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Domain.Entities;

namespace AiDocAssistant.Application.Interfaces
{
    public interface IDocumentRepository
    {
        Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken);
        Task AddAsync(Document document, CancellationToken cancellationToken);
        Task DeleteAsync(Document document, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}

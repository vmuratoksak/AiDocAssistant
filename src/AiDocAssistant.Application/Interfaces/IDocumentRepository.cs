using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Domain.Entities;

namespace AiDocAssistant.Application.Interfaces
{
    public interface IDocumentRepository
    {
        Task AddAsync(Document document, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}

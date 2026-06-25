using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Application.Models;

namespace AiDocAssistant.Application.Interfaces
{
    public interface IDocumentQueue
    {
        ValueTask EnqueueAsync(DocumentQueueItem item);
        ValueTask<DocumentQueueItem> DequeueAsync(CancellationToken cancellationToken);
    }
}

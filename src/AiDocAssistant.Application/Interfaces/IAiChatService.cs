using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiDocAssistant.Application.Interfaces
{
    public interface IAiChatService
    {
        Task<string> AskAsync(
            string question,
            IReadOnlyList<string> contextChunks,
            CancellationToken cancellationToken);
    }
}

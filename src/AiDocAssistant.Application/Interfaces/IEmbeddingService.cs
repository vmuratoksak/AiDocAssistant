using System.Threading;
using System.Threading.Tasks;

namespace AiDocAssistant.Application.Interfaces
{
    public interface IEmbeddingService
    {
        Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken);
    }
}

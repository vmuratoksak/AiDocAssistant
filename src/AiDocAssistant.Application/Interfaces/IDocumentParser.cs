using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AiDocAssistant.Application.Interfaces
{
    public interface IDocumentParser
    {
        Task<string> ExtractTextAsync(Stream fileStream, CancellationToken cancellationToken);
    }
}

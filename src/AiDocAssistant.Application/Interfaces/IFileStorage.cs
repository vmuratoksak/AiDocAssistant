using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AiDocAssistant.Application.Interfaces
{
    public interface IFileStorage
    {
        Task<string> SaveFileAsync(string fileName, Stream stream, CancellationToken cancellationToken);
        Task DeleteFileAsync(string filePath);
        Task<Stream> GetFileStreamAsync(string filePath);
    }
}

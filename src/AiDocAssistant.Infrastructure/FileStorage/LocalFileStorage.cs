using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Application.Interfaces;

namespace AiDocAssistant.Infrastructure.FileStorage
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly string _storageDirectory;

        public LocalFileStorage()
        {
            _storageDirectory = Path.Combine(AppContext.BaseDirectory, "App_Data", "Uploads");
            if (!Directory.Exists(_storageDirectory))
            {
                Directory.CreateDirectory(_storageDirectory);
            }
        }

        public async Task<string> SaveFileAsync(string fileName, Stream stream, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Dosya adı boş olamaz.", nameof(fileName));

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
            var filePath = Path.Combine(_storageDirectory, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                if (stream.CanSeek)
                    stream.Position = 0;

                await stream.CopyToAsync(fileStream, cancellationToken);
            }

            return filePath;
        }

        public Task DeleteFileAsync(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return Task.CompletedTask;
        }

        public Task<Stream> GetFileStreamAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Dosya bulunamadı: {filePath}");

            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            return Task.FromResult<Stream>(stream);
        }
    }
}

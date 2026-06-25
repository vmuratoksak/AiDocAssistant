using System;
using System.IO;
using System.Threading.Tasks;
using AiDocAssistant.Application.Interfaces;

namespace AiDocAssistant.Infrastructure.Prompts
{
    public class FilePromptProvider : IPromptProvider
    {
        private readonly string _promptsDirectory;

        public FilePromptProvider()
        {
            // Çalışan uygulamanın çıktı dizinindeki "Prompts" klasörünü temel alıyoruz
            _promptsDirectory = Path.Combine(AppContext.BaseDirectory, "Prompts");
            
            if (!Directory.Exists(_promptsDirectory))
            {
                Directory.CreateDirectory(_promptsDirectory);
            }
        }

        public async Task<string> GetPromptAsync(string promptName)
        {
            if (string.IsNullOrWhiteSpace(promptName))
                throw new ArgumentException("Prompt ismi boş olamaz.", nameof(promptName));

            var fileName = promptName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) 
                ? promptName 
                : $"{promptName}.txt";

            var filePath = Path.Combine(_promptsDirectory, fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Prompt dosyası '{fileName}' şu dizinde bulunamadı: {_promptsDirectory}");
            }

            return await File.ReadAllTextAsync(filePath);
        }
    }
}

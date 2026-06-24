using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Application.Interfaces;
using UglyToad.PdfPig;

namespace AiDocAssistant.Infrastructure.Parser
{
    public class DocumentParser : IDocumentParser
    {
        public async Task<string> ExtractTextAsync(Stream fileStream, CancellationToken cancellationToken)
        {
            if (fileStream == null || fileStream.Length == 0)
                return string.Empty;

            if (fileStream.CanSeek)
                fileStream.Position = 0;

            // Akışın PDF mi yoksa düz metin mi olduğunu ilk 4 bayttan (magic bytes) kontrol ediyoruz: %PDF
            bool isPdf = false;
            byte[] header = new byte[4];
            if (fileStream.CanSeek && fileStream.Length >= 4)
            {
                int read = await fileStream.ReadAsync(header, 0, 4, cancellationToken);
                fileStream.Position = 0;
                
                isPdf = read == 4 && 
                        header[0] == 0x25 && // %
                        header[1] == 0x50 && // P
                        header[2] == 0x44 && // D
                        header[3] == 0x46;   // F
            }

            if (isPdf)
            {
                return ExtractTextFromPdf(fileStream);
            }
            else
            {
                // Düz metin okuyucu
                using (var reader = new StreamReader(fileStream, Encoding.UTF8, true, 1024, true))
                {
                    return await reader.ReadToEndAsync(cancellationToken);
                }
            }
        }

        private string ExtractTextFromPdf(Stream stream)
        {
            var sb = new StringBuilder();
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                var bytes = memoryStream.ToArray();

                using (var document = PdfDocument.Open(bytes))
                {
                    foreach (var page in document.GetPages())
                    {
                        var text = page.Text;
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            sb.AppendLine(text);
                        }
                    }
                }
            }
            return sb.ToString();
        }
    }
}

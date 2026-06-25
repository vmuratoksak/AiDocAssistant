using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Application.Interfaces;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

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
                        var words = page.GetWords();
                        if (words != null && words.Any())
                        {
                            // Sort words primarily top-to-bottom, then left-to-right
                            var sortedWords = words
                                .OrderByDescending(w => w.BoundingBox.Bottom)
                                .ThenBy(w => w.BoundingBox.Left)
                                .ToList();

                            var linesList = new List<List<Word>>();
                            foreach (var word in sortedWords)
                            {
                                var added = false;
                                foreach (var line in linesList)
                                {
                                    // Check if this word is on the same vertical level (tolerance of 3 points)
                                    var avgBottom = line.Average(w => w.BoundingBox.Bottom);
                                    if (Math.Abs(word.BoundingBox.Bottom - avgBottom) <= 3.0)
                                    {
                                        line.Add(word);
                                        added = true;
                                        break;
                                    }
                                }
                                if (!added)
                                {
                                    linesList.Add(new List<Word> { word });
                                }
                            }

                            // Order lines top-to-bottom, and words in each line left-to-right
                            var sortedLines = linesList
                                .OrderByDescending(line => line.Average(w => w.BoundingBox.Bottom))
                                .Select(line => string.Join(" ", line.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)));

                            var text = string.Join(Environment.NewLine, sortedLines);
                            sb.AppendLine(text);
                        }
                    }
                }
            }
            return sb.ToString();
        }
    }
}

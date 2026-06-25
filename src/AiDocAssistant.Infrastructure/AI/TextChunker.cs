using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AiDocAssistant.Application.Interfaces;

namespace AiDocAssistant.Infrastructure.AI
{
    public class TextChunker : ITextChunker
    {
        public IReadOnlyList<string> Split(string text, int maxChunkSize, int overlapSize)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            if (maxChunkSize <= 0)
                throw new ArgumentException("Max chunk size must be greater than zero.", nameof(maxChunkSize));

            if (overlapSize < 0 || overlapSize >= maxChunkSize)
                throw new ArgumentException("Overlap size must be non-negative and less than max chunk size.", nameof(overlapSize));

            // Cümle sınırlarına göre metni böl (nokta, ünlem, soru işaretinden sonraki boşluklar)
            // Satır başlarını (\n\n veya \r\n\r\n) da cümle sınırı olarak kabul ediyoruz
            var rawSentences = Regex.Split(text, @"(?<=[.!?])\s+|(?<=\n)\s*\n+");
            var sentences = new List<string>();

            foreach (var s in rawSentences)
            {
                var trimmed = s.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    // Eğer tek bir cümle maxChunkSize'dan büyükse, onu karakter bazında alt parçalara bölelim (güvenlik önlemi)
                    if (trimmed.Length > maxChunkSize)
                    {
                        int index = 0;
                        while (index < trimmed.Length)
                        {
                            int len = Math.Min(maxChunkSize, trimmed.Length - index);
                            sentences.Add(trimmed.Substring(index, len));
                            index += len;
                        }
                    }
                    else
                    {
                        sentences.Add(trimmed);
                    }
                }
            }

            var chunks = new List<string>();
            var currentChunk = new List<string>();
            int currentLength = 0;

            for (int i = 0; i < sentences.Count; i++)
            {
                var sentence = sentences[i];
                
                // Eğer bu cümleyi eklediğimizde limit aşılıyorsa, mevcut chunk'ı kaydet
                if (currentLength + sentence.Length + (currentChunk.Count > 0 ? 1 : 0) > maxChunkSize)
                {
                    if (currentChunk.Count > 0)
                    {
                        chunks.Add(string.Join(" ", currentChunk));
                    }

                    // Overlap oluşturmak için geriye doğru git ve overlap limitine sığacak kadar cümle seç
                    var overlapChunk = new List<string>();
                    int overlapLength = 0;
                    int j = i - 1;
                    
                    while (j >= 0)
                    {
                        var prevSentence = sentences[j];
                        if (overlapLength + prevSentence.Length + (overlapChunk.Count > 0 ? 1 : 0) <= overlapSize)
                        {
                            overlapChunk.Insert(0, prevSentence);
                            overlapLength += prevSentence.Length + (overlapChunk.Count > 0 ? 1 : 0);
                            j--;
                        }
                        else
                        {
                            break;
                        }
                    }

                    currentChunk = overlapChunk;
                    currentLength = overlapLength;
                }

                currentChunk.Add(sentence);
                currentLength += sentence.Length + (currentChunk.Count > 1 ? 1 : 0);
            }

            if (currentChunk.Count > 0)
            {
                chunks.Add(string.Join(" ", currentChunk));
            }

            return chunks;
        }
    }
}

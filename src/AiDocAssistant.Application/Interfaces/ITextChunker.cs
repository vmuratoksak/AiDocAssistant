using System.Collections.Generic;

namespace AiDocAssistant.Application.Interfaces
{
    public interface ITextChunker
    {
        IReadOnlyList<string> Split(string text, int maxChunkSize, int overlapSize);
    }
}

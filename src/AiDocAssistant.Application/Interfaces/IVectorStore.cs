using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Domain.Entities;

namespace AiDocAssistant.Application.Interfaces
{
    public interface IVectorStore
    {
        Task<IReadOnlyList<DocumentChunk>> SearchRelevantChunksAsync(
            float[] queryEmbedding,
            int topK,
            CancellationToken cancellationToken);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Application.Interfaces;

namespace AiDocAssistant.Application.UseCases
{
    public sealed class AskDocumentQuestionUseCase
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorStore _vectorStore;
        private readonly IAiChatService _aiChatService;

        public AskDocumentQuestionUseCase(
            IEmbeddingService embeddingService,
            IVectorStore vectorStore,
            IAiChatService aiChatService)
        {
            _embeddingService = embeddingService;
            _vectorStore = vectorStore;
            _aiChatService = aiChatService;
        }

        public async Task<AskQuestionResult> ExecuteAsync(
            string question,
            Guid? documentId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new ArgumentException("Question is required.");

            // 1. Soru için embedding vektörü üret
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(question, cancellationToken);

            // 2. Vector DB'den en yakın 10 parça (chunk) dokümanı bul
            var relevantChunks = await _vectorStore.SearchRelevantChunksAsync(
                queryEmbedding,
                topK: 10,
                documentId: documentId,
                cancellationToken);

            if (relevantChunks == null || relevantChunks.Count == 0)
            {
                return new AskQuestionResult(
                    "Bu soruya cevap verebilmek için sistemde ilgili doküman içeriği bulunamadı.",
                    Array.Empty<QuestionSourceResult>());
            }

            // 3. Eşleşen chunk metinlerini topla ve sırala
            var contextTexts = relevantChunks
                .OrderBy(c => c.Order)
                .Select(c => c.Content)
                .ToList();

            // 4. AI Servisine soruyu ve bağlamı göndererek cevap üret
            var answer = await _aiChatService.AskAsync(question, contextTexts, cancellationToken);

            // 5. Her parça için cosine distance hesapla
            var sources = relevantChunks.Select(c =>
            {
                var distance = CalculateCosineDistance(c.Embedding.ToArray(), queryEmbedding);
                return new QuestionSourceResult(c.Id, c.Order, c.Content, distance);
            }).ToList();

            return new AskQuestionResult(answer, sources);
        }

        private float CalculateCosineDistance(float[] vectorA, float[] vectorB)
        {
            if (vectorA == null || vectorB == null || vectorA.Length != vectorB.Length)
                return 1.0f;

            double dotProduct = 0;
            double normA = 0;
            double normB = 0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                normA += vectorA[i] * vectorA[i];
                normB += vectorB[i] * vectorB[i];
            }

            if (normA == 0 || normB == 0)
                return 1.0f;

            double similarity = dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
            return (float)(1.0 - similarity);
        }
    }

    public sealed class AskQuestionResult
    {
        public string Answer { get; }
        public IReadOnlyList<QuestionSourceResult> Sources { get; }

        public AskQuestionResult(string answer, IReadOnlyList<QuestionSourceResult> sources)
        {
            Answer = answer;
            Sources = sources;
        }
    }

    public sealed class QuestionSourceResult
    {
        public Guid ChunkId { get; }
        public int Order { get; }
        public string Content { get; }
        public float CosineDistance { get; }

        public QuestionSourceResult(Guid chunkId, int order, string content, float cosineDistance)
        {
            ChunkId = chunkId;
            Order = order;
            Content = content;
            CosineDistance = cosineDistance;
        }
    }
}

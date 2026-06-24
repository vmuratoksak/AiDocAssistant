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

        public async Task<string> ExecuteAsync(
            string question,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new ArgumentException("Question is required.");

            // 1. Soru için embedding vektörü üret
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(question, cancellationToken);

            // 2. Vector DB'den en yakın 5 parça (chunk) dokümanı bul
            var relevantChunks = await _vectorStore.SearchRelevantChunksAsync(
                queryEmbedding,
                topK: 5,
                cancellationToken);

            if (relevantChunks == null || relevantChunks.Count == 0)
                return "Bu soruya cevap verebilmek için sistemde ilgili doküman içeriği bulunamadı.";

            // 3. Eşleşen chunk metinlerini topla ve sırala
            var contextTexts = relevantChunks
                .OrderBy(c => c.Order)
                .Select(c => c.Content)
                .ToList();

            // 4. AI Servisine soruyu ve bağlamı göndererek cevap üret
            return await _aiChatService.AskAsync(question, contextTexts, cancellationToken);
        }
    }
}

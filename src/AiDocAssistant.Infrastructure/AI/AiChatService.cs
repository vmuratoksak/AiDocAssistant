using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Application.Interfaces;
using Microsoft.Extensions.AI;

namespace AiDocAssistant.Infrastructure.AI
{
    public sealed class AiChatService : IAiChatService
    {
        private readonly IChatClient _chatClient;

        public AiChatService(IChatClient chatClient)
        {
            _chatClient = chatClient;
        }

        public async Task<string> AskAsync(
            string question,
            IReadOnlyList<string> contextChunks,
            CancellationToken cancellationToken)
        {
            var context = string.Join("\n\n---\n\n", contextChunks);

            var prompt = $"""
            Sen kurumsal dokümanlara göre cevap veren bir asistansın.

            Kurallar:
            - Sadece verilen bağlama göre cevap ver.
            - Emin değilsen "Bu bilgi verilen dokümanda yok" de.
            - Cevabı kısa, net ve profesyonel yaz.

            Bağlam:
            {context}

            Soru:
            {question}
            """;

            var response = await _chatClient.GetResponseAsync(
                prompt,
                cancellationToken: cancellationToken);

            return response.Text ?? "Cevap üretilemedi.";
        }
    }
}

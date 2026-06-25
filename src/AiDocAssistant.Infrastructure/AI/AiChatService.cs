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
        private readonly IPromptProvider _promptProvider;

        public AiChatService(IChatClient chatClient, IPromptProvider promptProvider)
        {
            _chatClient = chatClient;
            _promptProvider = promptProvider;
        }

        public async Task<string> AskAsync(
            string question,
            IReadOnlyList<string> contextChunks,
            CancellationToken cancellationToken)
        {
            var context = string.Join("\n\n---\n\n", contextChunks);

            // Harici prompt dosyasını dinamik olarak yüklüyoruz
            var promptTemplate = await _promptProvider.GetPromptAsync("document-qa");

            // Şablonu bağlam (context) ve soru ile biçimlendiriyoruz
            var prompt = string.Format(promptTemplate, context, question);

            var response = await _chatClient.GetResponseAsync(
                prompt,
                cancellationToken: cancellationToken);

            return response.Text ?? "Cevap üretilemedi.";
        }
    }
}

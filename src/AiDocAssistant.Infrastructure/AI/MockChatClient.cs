using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace AiDocAssistant.Infrastructure.AI
{
    public sealed class MockChatClient : IChatClient
    {
        public ChatClientMetadata Metadata { get; } = new ChatClientMetadata("MockChatClient");

        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(500, cancellationToken);

            var responseText = "Mock Yanıt: Dokümanlarınız başarıyla tarandı. Bu bir test yanıtıdır. Gerçek cevaplar için lütfen Ollama veya OpenAI sağlayıcısını aktif edin.";

            return new ChatResponse(new ChatMessage(ChatRole.Assistant, responseText))
            {
                ModelId = "mock-model"
            };
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Streaming is not implemented in MockChatClient.");
        }

        public void Dispose()
        {
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            return serviceType.IsInstanceOfType(this) ? this : null;
        }
    }
}

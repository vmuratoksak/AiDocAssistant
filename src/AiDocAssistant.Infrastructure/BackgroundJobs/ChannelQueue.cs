using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AiDocAssistant.Application.Interfaces;
using AiDocAssistant.Application.Models;

namespace AiDocAssistant.Infrastructure.BackgroundJobs
{
    public class ChannelQueue : IDocumentQueue
    {
        private readonly Channel<DocumentQueueItem> _channel;

        public ChannelQueue()
        {
            var options = new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            };
            _channel = Channel.CreateUnbounded<DocumentQueueItem>(options);
        }

        public async ValueTask EnqueueAsync(DocumentQueueItem item)
        {
            await _channel.Writer.WriteAsync(item);
        }

        public async ValueTask<DocumentQueueItem> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}

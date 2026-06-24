using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace AiDocAssistant.Infrastructure.AI
{
    public sealed class MockEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
    {
        public EmbeddingGeneratorMetadata Metadata { get; } = new EmbeddingGeneratorMetadata("MockEmbeddingGenerator");

        public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(100, cancellationToken);

            var embeddings = new List<Embedding<float>>();
            var random = new Random();

            foreach (var val in values)
            {
                // pgvector testi için 384 boyutlu rastgele bir vektör üretiyoruz (All-MiniLM boyutu)
                var vector = new float[384];
                for (int i = 0; i < vector.Length; i++)
                {
                    vector[i] = (float)random.NextDouble();
                }
                embeddings.Add(new Embedding<float>(vector));
            }

            return new GeneratedEmbeddings<Embedding<float>>(embeddings);
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

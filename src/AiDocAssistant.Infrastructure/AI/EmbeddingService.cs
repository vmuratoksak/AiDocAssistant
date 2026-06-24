using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Application.Interfaces;
using Microsoft.Extensions.AI;

namespace AiDocAssistant.Infrastructure.AI
{
    public sealed class EmbeddingService : IEmbeddingService
    {
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

        public EmbeddingService(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
        {
            _embeddingGenerator = embeddingGenerator;
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<float>();

            var response = await _embeddingGenerator.GenerateAsync(new[] { text }, cancellationToken: cancellationToken);
            if (response == null || !response.Any())
                throw new InvalidOperationException("Failed to generate embedding.");

            // Embedding vektörünü float[] dizisine dönüştürerek döner
            return response.First().Vector.ToArray();
        }
    }
}

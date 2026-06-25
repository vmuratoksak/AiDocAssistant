using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Application.DTOs;
using AiDocAssistant.Application.Interfaces;

namespace AiDocAssistant.Application.UseCases
{
    public sealed class GetDocumentChunksUseCase
    {
        private readonly IDocumentRepository _repository;

        public GetDocumentChunksUseCase(IDocumentRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<DocumentChunkDto>?> ExecuteAsync(Guid documentId, CancellationToken cancellationToken)
        {
            var document = await _repository.GetByIdAsync(documentId, cancellationToken);
            if (document == null)
                return null;

            return document.Chunks
                .OrderBy(c => c.Order)
                .Select(c => new DocumentChunkDto
                {
                    Id = c.Id,
                    DocumentId = c.DocumentId,
                    Order = c.Order,
                    Content = c.Content,
                    EmbeddingDimension = c.Embedding != null ? c.Embedding.Length : 0
                }).ToList();
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Application.DTOs;
using AiDocAssistant.Application.Interfaces;

namespace AiDocAssistant.Application.UseCases
{
    public sealed class GetDocumentsUseCase
    {
        private readonly IDocumentRepository _repository;

        public GetDocumentsUseCase(IDocumentRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<DocumentDto>> ExecuteAsync(CancellationToken cancellationToken)
        {
            var documents = await _repository.GetAllAsync(cancellationToken);
            return documents.Select(d => new DocumentDto
            {
                Id = d.Id,
                FileName = d.FileName,
                ContentType = d.ContentType,
                UploadedAt = d.UploadedAt,
                Status = d.Status.ToString(),
                ErrorMessage = d.ErrorMessage,
                ChunkCount = d.Chunks?.Count ?? 0
            }).ToList();
        }
    }
}

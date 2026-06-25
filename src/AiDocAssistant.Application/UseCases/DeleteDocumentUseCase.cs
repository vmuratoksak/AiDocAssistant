using System;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Application.Interfaces;

namespace AiDocAssistant.Application.UseCases
{
    public sealed class DeleteDocumentUseCase
    {
        private readonly IDocumentRepository _repository;

        public DeleteDocumentUseCase(IDocumentRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> ExecuteAsync(Guid documentId, CancellationToken cancellationToken)
        {
            var document = await _repository.GetByIdAsync(documentId, cancellationToken);
            if (document == null)
                return false;

            await _repository.DeleteAsync(document, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}

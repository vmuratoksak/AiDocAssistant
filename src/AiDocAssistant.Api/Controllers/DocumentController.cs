using System;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Application.UseCases;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AiDocAssistant.Api.Controllers
{
    [ApiController]
    [Route("api/documents")]
    public sealed class DocumentController : ControllerBase
    {
        private readonly UploadDocumentUseCase _uploadDocumentUseCase;
        private readonly GetDocumentsUseCase _getDocumentsUseCase;
        private readonly GetDocumentChunksUseCase _getDocumentChunksUseCase;
        private readonly DeleteDocumentUseCase _deleteDocumentUseCase;

        public DocumentController(
            UploadDocumentUseCase uploadDocumentUseCase,
            GetDocumentsUseCase getDocumentsUseCase,
            GetDocumentChunksUseCase getDocumentChunksUseCase,
            DeleteDocumentUseCase deleteDocumentUseCase)
        {
            _uploadDocumentUseCase = uploadDocumentUseCase;
            _getDocumentsUseCase = getDocumentsUseCase;
            _getDocumentChunksUseCase = getDocumentChunksUseCase;
            _deleteDocumentUseCase = deleteDocumentUseCase;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Lütfen geçerli bir dosya yükleyin.");

            using var stream = file.OpenReadStream();
            var documentId = await _uploadDocumentUseCase.ExecuteAsync(
                file.FileName,
                file.ContentType,
                stream,
                cancellationToken);

            return Ok(new { DocumentId = documentId, Message = "Doküman başarıyla yüklendi, parçalandı ve kaydedildi." });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _getDocumentsUseCase.ExecuteAsync(cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id}/chunks")]
        public async Task<IActionResult> GetChunks(Guid id, CancellationToken cancellationToken)
        {
            var result = await _getDocumentChunksUseCase.ExecuteAsync(id, cancellationToken);
            if (result == null)
                return NotFound("Doküman bulunamadı.");

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var success = await _deleteDocumentUseCase.ExecuteAsync(id, cancellationToken);
            if (!success)
                return NotFound("Doküman bulunamadı.");

            return Ok(new { Message = "Doküman ve bağlı tüm parçaları başarıyla silindi." });
        }
    }
}

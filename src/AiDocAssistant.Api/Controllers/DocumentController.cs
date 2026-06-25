using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Application.Interfaces;
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
        private readonly IDocumentRepository _repository;

        public DocumentController(
            UploadDocumentUseCase uploadDocumentUseCase,
            IDocumentRepository repository)
        {
            _uploadDocumentUseCase = uploadDocumentUseCase;
            _repository = repository;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Lütfen geçerli bir dosya yükleyin.");

            try
            {
                using var stream = file.OpenReadStream();
                var documentId = await _uploadDocumentUseCase.ExecuteAsync(
                    file.FileName,
                    file.ContentType,
                    stream,
                    cancellationToken);

                return Ok(new { DocumentId = documentId, Message = "Doküman başarıyla yüklendi, parçalandı ve kaydedildi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Dosya işlenirken hata oluştu: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var docs = await _repository.GetAllAsync(cancellationToken);
            var result = docs.Select(d => new
            {
                d.Id,
                d.FileName,
                d.ContentType,
                d.UploadedAt,
                d.Status,
                d.ErrorMessage,
                ChunkCount = d.Chunks.Count
            });
            return Ok(result);
        }

        [HttpGet("{id}/chunks")]
        public async Task<IActionResult> GetChunks(Guid id, CancellationToken cancellationToken)
        {
            var doc = await _repository.GetByIdAsync(id, cancellationToken);
            if (doc == null)
                return NotFound("Doküman bulunamadı.");

            var result = doc.Chunks.OrderBy(c => c.Order).Select(c => new
            {
                c.Id,
                c.DocumentId,
                c.Order,
                c.Content,
                EmbeddingDimension = c.Embedding != null ? 768 : 0 // nomic-embed-text generates 768 dimensions
            });
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var doc = await _repository.GetByIdAsync(id, cancellationToken);
            if (doc == null)
                return NotFound("Doküman bulunamadı.");

            await _repository.DeleteAsync(doc, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            return Ok(new { Message = "Doküman ve bağlı tüm parçaları başarıyla silindi." });
        }
    }
}

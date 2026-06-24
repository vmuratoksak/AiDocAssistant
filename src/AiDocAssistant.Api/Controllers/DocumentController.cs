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

        public DocumentController(UploadDocumentUseCase uploadDocumentUseCase)
        {
            _uploadDocumentUseCase = uploadDocumentUseCase;
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
    }
}

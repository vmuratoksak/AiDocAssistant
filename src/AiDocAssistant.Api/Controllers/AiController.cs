using System;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace AiDocAssistant.Api.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public sealed class AiController : ControllerBase
    {
        private readonly AskDocumentQuestionUseCase _askDocumentQuestionUseCase;

        public AiController(AskDocumentQuestionUseCase askDocumentQuestionUseCase)
        {
            _askDocumentQuestionUseCase = askDocumentQuestionUseCase;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask(
            [FromBody] AskQuestionRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request?.Question))
                return BadRequest("Soru boş olamaz.");

            try
            {
                var result = await _askDocumentQuestionUseCase.ExecuteAsync(
                    request.Question,
                    request.DocumentId,
                    cancellationToken);

                return Ok(new AskQuestionResponse(
                    result.Answer,
                    result.Sources.Select(s => new QuestionSourceResponse(s.ChunkId, s.Order, s.Content, s.CosineDistance)).ToList()
                ));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"AI yanıtı üretilirken hata oluştu: {ex.Message}");
            }
        }
    }

    public sealed record AskQuestionRequest(string Question, Guid? DocumentId = null);
    
    public sealed record AskQuestionResponse(
        string Answer, 
        System.Collections.Generic.IReadOnlyList<QuestionSourceResponse> Sources);

    public sealed record QuestionSourceResponse(
        Guid ChunkId, 
        int Order, 
        string Content, 
        float CosineDistance);
}

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
                var answer = await _askDocumentQuestionUseCase.ExecuteAsync(
                    request.Question,
                    cancellationToken);

                return Ok(new AskQuestionResponse(answer));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"AI yanıtı üretilirken hata oluştu: {ex.Message}");
            }
        }
    }

    public sealed record AskQuestionRequest(string Question);
    public sealed record AskQuestionResponse(string Answer);
}

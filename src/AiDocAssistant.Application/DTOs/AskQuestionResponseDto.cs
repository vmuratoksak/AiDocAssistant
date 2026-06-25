using System;
using System.Collections.Generic;

namespace AiDocAssistant.Application.DTOs
{
    public sealed record AskQuestionResponseDto(
        string Answer, 
        IReadOnlyList<QuestionSourceDto> Sources);

    public sealed record QuestionSourceDto(
        Guid ChunkId, 
        int Order, 
        string Content, 
        float CosineDistance);
}

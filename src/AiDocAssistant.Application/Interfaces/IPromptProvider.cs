using System.Threading.Tasks;

namespace AiDocAssistant.Application.Interfaces
{
    public interface IPromptProvider
    {
        Task<string> GetPromptAsync(string promptName);
    }
}

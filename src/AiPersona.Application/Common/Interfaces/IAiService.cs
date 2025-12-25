using AiPersona.Domain.Entities;

namespace AiPersona.Application.Common.Interfaces;

public record AiResponse(
    string Text,
    int TokensUsed,
    string? Sentiment = null,
    bool IsFallback = false,
    string? ErrorType = null);

public interface IAiService
{
    Task<AiResponse> GenerateResponseAsync(
        Persona persona,
        List<ChatMessage> conversationHistory,
        string userMessage,
        CancellationToken cancellationToken = default);
}

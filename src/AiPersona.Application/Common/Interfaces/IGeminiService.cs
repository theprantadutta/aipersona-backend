using AiPersona.Domain.Entities;

namespace AiPersona.Application.Common.Interfaces;

public record GeminiResponse(
    string Text,
    int TokensUsed,
    string? Sentiment = null,
    bool IsFallback = false,
    string? ErrorType = null);

public interface IGeminiService
{
    Task<GeminiResponse> GenerateResponseAsync(
        Persona persona,
        List<ChatMessage> conversationHistory,
        string userMessage,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamResponseAsync(
        Persona persona,
        List<ChatMessage> conversationHistory,
        string userMessage,
        CancellationToken cancellationToken = default);
}

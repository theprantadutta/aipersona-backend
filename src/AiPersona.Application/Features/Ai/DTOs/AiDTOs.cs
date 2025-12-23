namespace AiPersona.Application.Features.Ai.DTOs;

public record AiResponseDto(
    string Response,
    int TokensUsed,
    string Model,
    double ProcessingTimeMs);

public record StreamResponseDto(
    string SessionId,
    string Status,
    string Message);

public record SentimentAnalysisDto(
    string Sentiment,
    double Confidence,
    Dictionary<string, double> Scores);

public record ConversationHistoryItem(
    string Role,
    string Content);

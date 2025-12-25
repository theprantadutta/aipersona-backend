using MediatR;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Ai.DTOs;
using AiPersona.Domain.Entities;

namespace AiPersona.Application.Features.Ai.Commands.AnalyzeSentiment;

public record AnalyzeSentimentCommand(string Text) : IRequest<Result<SentimentAnalysisDto>>;

public class AnalyzeSentimentCommandHandler : IRequestHandler<AnalyzeSentimentCommand, Result<SentimentAnalysisDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IAiService _aiService;

    public AnalyzeSentimentCommandHandler(
        ICurrentUserService currentUser,
        IAiService aiService)
    {
        _currentUser = currentUser;
        _aiService = aiService;
    }

    public async Task<Result<SentimentAnalysisDto>> Handle(AnalyzeSentimentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<SentimentAnalysisDto>.Failure("Unauthorized", 401);

        if (string.IsNullOrWhiteSpace(request.Text))
            return Result<SentimentAnalysisDto>.Failure("Text is required", 400);

        // Create a temporary persona for sentiment analysis
        var sentimentPersona = new Persona
        {
            Id = Guid.Empty,
            Name = "Sentiment Analyzer",
            Prompt = "You are a sentiment analysis assistant. Only respond with valid JSON."
        };

        var prompt = @"Analyze the sentiment of the following text and respond ONLY with a JSON object in this exact format:
{""sentiment"": ""positive|negative|neutral"", ""confidence"": 0.0-1.0, ""scores"": {""positive"": 0.0-1.0, ""negative"": 0.0-1.0, ""neutral"": 0.0-1.0}}

Text to analyze: " + request.Text;

        var result = await _aiService.GenerateResponseAsync(
            sentimentPersona,
            new List<ChatMessage>(),
            prompt,
            cancellationToken);

        try
        {
            var cleanResponse = result.Text.Trim();
            if (cleanResponse.StartsWith("```json"))
                cleanResponse = cleanResponse[7..];
            if (cleanResponse.StartsWith("```"))
                cleanResponse = cleanResponse[3..];
            if (cleanResponse.EndsWith("```"))
                cleanResponse = cleanResponse[..^3];
            cleanResponse = cleanResponse.Trim();

            var doc = System.Text.Json.JsonDocument.Parse(cleanResponse);
            var root = doc.RootElement;

            var sentiment = root.GetProperty("sentiment").GetString() ?? "neutral";
            var confidence = root.GetProperty("confidence").GetDouble();
            var scores = new Dictionary<string, double>
            {
                ["positive"] = root.GetProperty("scores").GetProperty("positive").GetDouble(),
                ["negative"] = root.GetProperty("scores").GetProperty("negative").GetDouble(),
                ["neutral"] = root.GetProperty("scores").GetProperty("neutral").GetDouble()
            };

            return Result<SentimentAnalysisDto>.Success(new SentimentAnalysisDto(sentiment, confidence, scores));
        }
        catch
        {
            return Result<SentimentAnalysisDto>.Success(new SentimentAnalysisDto(
                "neutral",
                0.5,
                new Dictionary<string, double>
                {
                    ["positive"] = 0.33,
                    ["negative"] = 0.33,
                    ["neutral"] = 0.34
                }));
        }
    }
}

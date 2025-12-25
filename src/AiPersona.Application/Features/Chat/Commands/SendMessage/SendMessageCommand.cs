using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Chat.DTOs;
using AiPersona.Domain.Entities;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Chat.Commands.SendMessage;

public record SendMessageCommand(Guid SessionId, string Message, double? Temperature = null)
    : IRequest<Result<SendMessageResponseDto>>;

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Message).NotEmpty().MaximumLength(10000);
        RuleFor(x => x.Temperature).InclusiveBetween(0.0, 1.0).When(x => x.Temperature.HasValue);
    }
}

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, Result<SendMessageResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAiService _aiService;
    private readonly IDateTimeService _dateTime;

    public SendMessageCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IAiService aiService,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _aiService = aiService;
        _dateTime = dateTime;
    }

    public async Task<Result<SendMessageResponseDto>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<SendMessageResponseDto>.Failure("Unauthorized", 401);

        var session = await _context.ChatSessions
            .Include(s => s.Persona)
            .ThenInclude(p => p!.KnowledgeBases)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == _currentUser.UserId, cancellationToken);

        if (session == null)
            return Result<SendMessageResponseDto>.Failure("Session not found", 404);

        if (session.Persona == null)
            return Result<SendMessageResponseDto>.Failure("Persona not found", 404);

        // Check if this is a greeting request (persona introduces themselves)
        var isGreeting = request.Message.Trim() == "[GREETING]";

        string responseText;
        int tokensUsed = 0;
        bool isAiFallback = false;
        string? aiErrorType = null;

        if (isGreeting)
        {
            // Check if we already have a greeting stored for this user+persona
            var existingGreeting = await _context.PersonaGreetings
                .FirstOrDefaultAsync(pg => pg.UserId == _currentUser.UserId && pg.PersonaId == session.Persona.Id, cancellationToken);

            // Check if the cached greeting is a fallback/error message (corrupted data)
            var isBadGreeting = existingGreeting != null &&
                (existingGreeting.GreetingText.Contains("I apologize") ||
                 existingGreeting.GreetingText.Contains("trouble connecting") ||
                 existingGreeting.GreetingText.Contains("try again") ||
                 existingGreeting.TokensUsed == 0);

            if (isBadGreeting)
            {
                // Remove the bad cached greeting
                _context.PersonaGreetings.Remove(existingGreeting!);
                existingGreeting = null;
            }

            if (existingGreeting != null)
            {
                // Use the stored greeting - no AI call needed
                responseText = existingGreeting.GreetingText;
                tokensUsed = existingGreeting.TokensUsed;
            }
            else
            {
                // Generate a new greeting via AI
                var greetingPrompt = "Please introduce yourself in character. Give a brief, engaging greeting that shows your personality.";
                var response = await _aiService.GenerateResponseAsync(
                    session.Persona, new List<ChatMessage>(), greetingPrompt, cancellationToken);

                responseText = response.Text;
                tokensUsed = response.TokensUsed;
                isAiFallback = response.IsFallback;
                aiErrorType = response.ErrorType;

                // Only store the greeting if it's not a fallback (actual AI response)
                if (!response.IsFallback)
                {
                    var newGreeting = new PersonaGreeting
                    {
                        UserId = _currentUser.UserId.Value,
                        PersonaId = session.Persona.Id,
                        GreetingText = responseText,
                        TokensUsed = tokensUsed,
                        CreatedAt = _dateTime.UtcNow
                    };
                    _context.PersonaGreetings.Add(newGreeting);
                }
            }
        }
        else
        {
            // Regular message - get conversation history and generate response
            var history = await _context.ChatMessages
                .Where(m => m.SessionId == session.Id)
                .OrderBy(m => m.CreatedAt)
                .Take(20)
                .ToListAsync(cancellationToken);

            var response = await _aiService.GenerateResponseAsync(
                session.Persona, history, request.Message, cancellationToken);

            responseText = response.Text;
            tokensUsed = response.TokensUsed;
            isAiFallback = response.IsFallback;
            aiErrorType = response.ErrorType;
        }

        // Create user message (hidden system message for greetings)
        var userMessage = new ChatMessage
        {
            SessionId = session.Id,
            SenderType = SenderType.User,
            Text = isGreeting ? "[System: User opened chat]" : request.Message,
            MessageType = isGreeting ? MessageType.System : MessageType.Text,
            CreatedAt = _dateTime.UtcNow
        };
        _context.ChatMessages.Add(userMessage);

        // Create AI message
        var aiMessage = new ChatMessage
        {
            SessionId = session.Id,
            SenderType = SenderType.Ai,
            Text = responseText,
            MessageType = MessageType.Text,
            TokensUsed = tokensUsed,
            CreatedAt = _dateTime.UtcNow
        };
        _context.ChatMessages.Add(aiMessage);

        // Update session
        session.MessageCount += 2;
        session.LastMessageAt = _dateTime.UtcNow;
        session.UpdatedAt = _dateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<SendMessageResponseDto>.Success(new SendMessageResponseDto(
            new ChatMessageDto(userMessage.Id, userMessage.SessionId, _currentUser.UserId,
                userMessage.SenderType.ToString(), userMessage.Text, userMessage.MessageType.ToString(),
                userMessage.TokensUsed, userMessage.CreatedAt),
            new ChatMessageDto(aiMessage.Id, aiMessage.SessionId, null,
                aiMessage.SenderType.ToString(), aiMessage.Text, aiMessage.MessageType.ToString(),
                aiMessage.TokensUsed, aiMessage.CreatedAt),
            isAiFallback,
            aiErrorType));
    }
}

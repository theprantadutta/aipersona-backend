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
    private readonly IGeminiService _geminiService;
    private readonly IDateTimeService _dateTime;

    public SendMessageCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IGeminiService geminiService,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _geminiService = geminiService;
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

        // Get conversation history
        var history = await _context.ChatMessages
            .Where(m => m.SessionId == session.Id)
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        // Create user message
        var userMessage = new ChatMessage
        {
            SessionId = session.Id,
            SenderType = SenderType.User,
            Text = request.Message,
            MessageType = MessageType.Text,
            CreatedAt = _dateTime.UtcNow
        };
        _context.ChatMessages.Add(userMessage);

        // Generate AI response
        var response = await _geminiService.GenerateResponseAsync(
            session.Persona, history, request.Message, cancellationToken);

        // Create AI message
        var aiMessage = new ChatMessage
        {
            SessionId = session.Id,
            SenderType = SenderType.Ai,
            Text = response.Text,
            MessageType = MessageType.Text,
            TokensUsed = response.TokensUsed,
            CreatedAt = _dateTime.UtcNow
        };
        _context.ChatMessages.Add(aiMessage);

        // Update session
        session.MessageCount += 2;
        session.LastMessageAt = _dateTime.UtcNow;
        session.UpdatedAt = _dateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<SendMessageResponseDto>.Success(new SendMessageResponseDto(
            new ChatMessageDto(userMessage.Id, userMessage.SessionId, userMessage.SenderType.ToString(),
                userMessage.Text, userMessage.MessageType.ToString(), userMessage.TokensUsed, userMessage.CreatedAt),
            new ChatMessageDto(aiMessage.Id, aiMessage.SessionId, aiMessage.SenderType.ToString(),
                aiMessage.Text, aiMessage.MessageType.ToString(), aiMessage.TokensUsed, aiMessage.CreatedAt)));
    }
}

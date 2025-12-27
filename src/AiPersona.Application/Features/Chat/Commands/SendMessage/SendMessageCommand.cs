using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<SendMessageCommandHandler> _logger;

    public SendMessageCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IAiService aiService,
        IDateTimeService dateTime,
        ILogger<SendMessageCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _aiService = aiService;
        _dateTime = dateTime;
        _logger = logger;
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

        // ===== MESSAGE LIMIT ENFORCEMENT =====
        // Only enforce for non-greeting messages
        if (!isGreeting)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

            if (user != null)
            {
                var (messageLimit, _, _, _) = GetLimitsForTier(user.SubscriptionTier);

                // -1 means unlimited
                if (messageLimit > 0)
                {
                    var today = _dateTime.UtcNow.Date;
                    var tomorrow = today.AddDays(1);

                    // Count ALL messages (user + AI) sent today, excluding system messages
                    var userSessionIds = await _context.ChatSessions
                        .Where(s => s.UserId == _currentUser.UserId)
                        .Select(s => s.Id)
                        .ToListAsync(cancellationToken);

                    var messagesToday = await _context.ChatMessages
                        .Where(m => userSessionIds.Contains(m.SessionId)
                            && m.MessageType != MessageType.System  // Exclude system/greeting messages
                            && m.CreatedAt >= today
                            && m.CreatedAt < tomorrow)
                        .CountAsync(cancellationToken);

                    if (messagesToday >= messageLimit)
                    {
                        _logger.LogWarning("User {UserId} has exceeded daily message limit ({Count}/{Limit})",
                            _currentUser.UserId, messagesToday, messageLimit);
                        return Result<SendMessageResponseDto>.Failure(
                            $"Daily message limit reached ({messageLimit} messages). Upgrade your plan for more messages.",
                            429); // 429 Too Many Requests
                    }
                }
            }
        }
        // ===== END MESSAGE LIMIT ENFORCEMENT =====

        string responseText;
        int tokensUsed = 0;
        bool isAiFallback = false;
        string? aiErrorType = null;

        if (isGreeting)
        {
            // FIRST: Check if session already has messages (greeting already sent)
            var existingSessionMessages = await _context.ChatMessages
                .Where(m => m.SessionId == session.Id)
                .OrderBy(m => m.CreatedAt)
                .Take(2)
                .ToListAsync(cancellationToken);

            if (existingSessionMessages.Count > 0)
            {
                // Session already has messages - return the existing greeting without adding duplicates
                var existingAiGreeting = existingSessionMessages.FirstOrDefault(m => m.SenderType == SenderType.Ai);
                var existingUserMsg = existingSessionMessages.FirstOrDefault(m => m.SenderType == SenderType.User);

                if (existingAiGreeting != null && existingUserMsg != null)
                {
                    // Return existing messages - no new records created
                    return Result<SendMessageResponseDto>.Success(new SendMessageResponseDto(
                        new ChatMessageDto(existingUserMsg.Id, existingUserMsg.SessionId, _currentUser.UserId,
                            existingUserMsg.SenderType.ToString(), existingUserMsg.Text, existingUserMsg.MessageType.ToString(),
                            existingUserMsg.TokensUsed, existingUserMsg.CreatedAt),
                        new ChatMessageDto(existingAiGreeting.Id, existingAiGreeting.SessionId, null,
                            existingAiGreeting.SenderType.ToString(), existingAiGreeting.Text, existingAiGreeting.MessageType.ToString(),
                            existingAiGreeting.TokensUsed, existingAiGreeting.CreatedAt),
                        false, null));
                }
            }

            // Session is empty - generate greeting
            // Check if we have a cached greeting for this user+persona
            var existingGreeting = await _context.PersonaGreetings
                .FirstOrDefaultAsync(pg => pg.UserId == _currentUser.UserId && pg.PersonaId == session.Persona.Id, cancellationToken);

            // Check if the cached greeting is a fallback/error message (corrupted data)
            var isBadGreeting = existingGreeting != null &&
                (existingGreeting.GreetingText.Contains("I apologize") ||
                 existingGreeting.GreetingText.Contains("trouble connecting") ||
                 existingGreeting.GreetingText.Contains("try again"));

            if (isBadGreeting)
            {
                // Remove the bad cached greeting
                _context.PersonaGreetings.Remove(existingGreeting!);
                existingGreeting = null;
            }

            if (existingGreeting != null)
            {
                // Use the stored greeting text - no AI call needed
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

                // Only cache the greeting if it's not a fallback (actual AI response)
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

        // Update usage tracking (only for non-greeting messages)
        if (!isGreeting)
        {
            var today = _dateTime.UtcNow.Date;
            var usage = await _context.UsageTrackings
                .FirstOrDefaultAsync(u => u.UserId == _currentUser.UserId, cancellationToken);

            if (usage == null)
            {
                _logger.LogInformation("Creating new UsageTracking for user {UserId}", _currentUser.UserId);
                usage = new UsageTracking
                {
                    UserId = _currentUser.UserId.Value,
                    MessagesToday = 1,
                    GeminiApiCallsToday = 1,
                    GeminiTokensUsedTotal = tokensUsed,
                    MessagesCountResetAt = _dateTime.UtcNow,
                    CreatedAt = _dateTime.UtcNow,
                    UpdatedAt = _dateTime.UtcNow
                };
                _context.UsageTrackings.Add(usage);
            }
            else
            {
                // Reset count if it's a new day
                if (usage.MessagesCountResetAt.Date != today)
                {
                    _logger.LogInformation("Resetting daily count for user {UserId}, old date: {OldDate}, today: {Today}",
                        _currentUser.UserId, usage.MessagesCountResetAt.Date, today);
                    usage.MessagesToday = 1;
                    usage.GeminiApiCallsToday = 1;
                    usage.MessagesCountResetAt = _dateTime.UtcNow;
                }
                else
                {
                    usage.MessagesToday++;
                    usage.GeminiApiCallsToday++;
                    _logger.LogInformation("Updated MessagesToday to {Count} for user {UserId}",
                        usage.MessagesToday, _currentUser.UserId);
                }
                usage.GeminiTokensUsedTotal += tokensUsed;
                usage.UpdatedAt = _dateTime.UtcNow;
            }
        }
        else
        {
            _logger.LogInformation("Skipping usage tracking for greeting message");
        }

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

    /// <summary>
    /// Get subscription tier limits. Returns (messageLimit, personaLimit, storageMb, historyDays).
    /// -1 means unlimited.
    /// </summary>
    private static (int messageLimit, int personaLimit, int storageMb, int historyDays) GetLimitsForTier(SubscriptionTier tier)
    {
        return tier switch
        {
            SubscriptionTier.Pro => (-1, -1, 102400, -1),      // Unlimited messages/personas, 100GB storage
            SubscriptionTier.Premium => (500, -1, 10240, -1),  // 500 messages/day, unlimited personas, 10GB storage
            SubscriptionTier.Basic => (200, 10, 1024, 30),     // 200 messages/day, 10 personas, 1GB, 30 days
            SubscriptionTier.Lifetime => (-1, -1, 102400, -1), // Same as Pro
            _ => (20, 3, 100, 7)                               // Free: 20 messages/day, 3 personas, 100MB, 7 days
        };
    }
}

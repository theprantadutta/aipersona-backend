using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Notifications.DTOs;

namespace AiPersona.Application.Features.Notifications.Commands.SendNotification;

public record SendNotificationCommand(
    Guid? UserId,
    string Title,
    string Body,
    Dictionary<string, string>? Data = null) : IRequest<Result<SendNotificationResultDto>>;

public class SendNotificationCommandHandler : IRequestHandler<SendNotificationCommand, Result<SendNotificationResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IFcmService _fcmService;

    public SendNotificationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IFcmService fcmService)
    {
        _context = context;
        _currentUser = currentUser;
        _fcmService = fcmService;
    }

    public async Task<Result<SendNotificationResultDto>> Handle(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<SendNotificationResultDto>.Failure("Unauthorized", 401);

        var targetUserId = request.UserId ?? _currentUser.UserId.Value;

        var tokens = await _context.FcmTokens
            .Where(t => t.UserId == targetUserId)
            .Select(t => t.Token)
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0)
            return Result<SendNotificationResultDto>.Success(new SendNotificationResultDto(
                true, 0, "No devices registered"));

        var sentCount = 0;
        foreach (var token in tokens)
        {
            try
            {
                await _fcmService.SendNotificationAsync(token, request.Title, request.Body, request.Data);
                sentCount++;
            }
            catch
            {
                // Continue with other tokens if one fails
            }
        }

        return Result<SendNotificationResultDto>.Success(new SendNotificationResultDto(
            sentCount > 0, sentCount, $"Sent to {sentCount}/{tokens.Count} devices"));
    }
}

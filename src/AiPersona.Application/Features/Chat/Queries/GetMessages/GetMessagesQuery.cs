using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Chat.DTOs;

namespace AiPersona.Application.Features.Chat.Queries.GetMessages;

public record GetMessagesQuery(
    Guid SessionId,
    int Page = 1,
    int PageSize = 50,
    DateTime? Before = null,
    DateTime? After = null) : IRequest<Result<ChatMessageListDto>>;

public class GetMessagesQueryHandler : IRequestHandler<GetMessagesQuery, Result<ChatMessageListDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMessagesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<ChatMessageListDto>> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<ChatMessageListDto>.Failure("Unauthorized", 401);

        var session = await _context.ChatSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == _currentUser.UserId, cancellationToken);

        if (session == null)
            return Result<ChatMessageListDto>.Failure("Session not found", 404);

        var query = _context.ChatMessages
            .Where(m => m.SessionId == request.SessionId)
            .AsNoTracking();

        if (request.Before.HasValue)
            query = query.Where(m => m.CreatedAt < request.Before);

        if (request.After.HasValue)
            query = query.Where(m => m.CreatedAt > request.After);

        var total = await query.CountAsync(cancellationToken);
        var skip = (request.Page - 1) * request.PageSize;

        var messages = await query
            .OrderBy(m => m.CreatedAt)
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = messages.Select(m => new ChatMessageDto(
            m.Id, m.SessionId, m.SenderType.ToString(), m.Text,
            m.MessageType.ToString(), m.TokensUsed, m.CreatedAt)).ToList();

        return Result<ChatMessageListDto>.Success(new ChatMessageListDto(
            dtos, total, request.Page, request.PageSize, request.Before, request.After));
    }
}

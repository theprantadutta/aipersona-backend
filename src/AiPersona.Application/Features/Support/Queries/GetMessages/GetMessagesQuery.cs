using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Support.DTOs;

namespace AiPersona.Application.Features.Support.Queries.GetMessages;

public record GetMessagesQuery(Guid TicketId, int Page = 1, int PageSize = 50) : IRequest<Result<SupportMessageListDto>>;

public class GetMessagesQueryHandler : IRequestHandler<GetMessagesQuery, Result<SupportMessageListDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMessagesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<SupportMessageListDto>> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<SupportMessageListDto>.Failure("Unauthorized", 401);

        var ticket = await _context.SupportTickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket == null)
            return Result<SupportMessageListDto>.Failure("Ticket not found", 404);

        if (!_currentUser.IsAdmin && ticket.UserId != _currentUser.UserId)
            return Result<SupportMessageListDto>.Failure("Access denied", 403);

        var query = _context.SupportTicketMessages
            .Include(m => m.Sender)
            .Where(m => m.TicketId == request.TicketId)
            .AsNoTracking();

        var total = await query.CountAsync(cancellationToken);
        var skip = (request.Page - 1) * request.PageSize;

        var messages = await query
            .OrderBy(m => m.CreatedAt)
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = messages.Select(m => new SupportMessageDto(
            m.Id,
            m.TicketId,
            m.SenderId,
            m.Sender?.DisplayName ?? m.Sender?.Email ?? "Unknown",
            m.IsStaffReply,
            m.Content,
            m.Attachments,
            m.CreatedAt)).ToList();

        return Result<SupportMessageListDto>.Success(new SupportMessageListDto(dtos, total, request.Page, request.PageSize));
    }
}

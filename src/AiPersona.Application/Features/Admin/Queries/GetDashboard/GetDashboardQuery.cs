using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Admin.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Admin.Queries.GetDashboard;

public record GetDashboardQuery : IRequest<Result<AdminDashboardDto>>;

public class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, Result<AdminDashboardDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public GetDashboardQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<AdminDashboardDto>> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null || !_currentUser.IsAdmin)
            return Result<AdminDashboardDto>.Failure("Admin access required", 403);

        var today = _dateTime.UtcNow.Date;

        var totalUsers = await _context.Users.CountAsync(cancellationToken);
        var activeUsersToday = await _context.Users.Where(u => u.LastActiveAt >= today).CountAsync(cancellationToken);
        var totalPersonas = await _context.Personas.CountAsync(cancellationToken);
        var totalMessages = await _context.ChatMessages.CountAsync(cancellationToken);
        var pendingReports = await _context.ContentReports.Where(r => r.Status == ReportStatus.Pending).CountAsync(cancellationToken);
        var openTickets = await _context.SupportTickets.Where(t => t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress).CountAsync(cancellationToken);

        var recentReports = await _context.ContentReports
            .Include(r => r.Reporter)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new AdminReportDto(
                r.Id,
                r.ReporterId,
                r.Reporter != null ? r.Reporter.DisplayName ?? r.Reporter.Email : "Unknown",
                r.ContentType.ToString(),
                r.ContentId,
                r.Reason,
                r.Description,
                r.Status.ToString(),
                r.ResolvedById,
                null,
                r.Resolution,
                r.CreatedAt,
                r.ResolvedAt))
            .ToListAsync(cancellationToken);

        var newestUsers = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .Select(u => new AdminUserDto(
                u.Id,
                u.Email,
                u.DisplayName,
                u.ProfileImage,
                u.SubscriptionTier.ToString(),
                u.IsAdmin,
                u.IsSuspended,
                u.SuspendedUntil,
                0,
                0,
                u.CreatedAt,
                u.LastActiveAt))
            .ToListAsync(cancellationToken);

        return Result<AdminDashboardDto>.Success(new AdminDashboardDto(
            totalUsers,
            activeUsersToday,
            totalPersonas,
            totalMessages,
            pendingReports,
            openTickets,
            0,
            recentReports,
            newestUsers));
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Admin.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Admin.Queries.GetReports;

public record GetReportsQuery(
    string? Status = null,
    string? ContentType = null,
    int Page = 1,
    int PageSize = 50) : IRequest<Result<AdminReportListDto>>;

public class GetReportsQueryHandler : IRequestHandler<GetReportsQuery, Result<AdminReportListDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetReportsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<AdminReportListDto>> Handle(GetReportsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null || !_currentUser.IsAdmin)
            return Result<AdminReportListDto>.Failure("Admin access required", 403);

        var query = _context.ContentReports
            .Include(r => r.Reporter)
            .Include(r => r.ResolvedBy)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<ReportStatus>(request.Status, true, out var status))
            query = query.Where(r => r.Status == status);

        if (!string.IsNullOrEmpty(request.ContentType) && Enum.TryParse<ContentType>(request.ContentType, true, out var contentType))
            query = query.Where(r => r.ContentType == contentType);

        var total = await query.CountAsync(cancellationToken);
        var skip = (request.Page - 1) * request.PageSize;

        var reports = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = reports.Select(r => new AdminReportDto(
            r.Id,
            r.ReporterId,
            r.Reporter?.DisplayName ?? r.Reporter?.Email ?? "Unknown",
            r.ContentType.ToString(),
            r.ContentId,
            r.Reason,
            r.Description,
            r.Status.ToString(),
            r.ResolvedById,
            r.ResolvedBy?.DisplayName ?? r.ResolvedBy?.Email,
            r.Resolution,
            r.CreatedAt,
            r.ResolvedAt)).ToList();

        return Result<AdminReportListDto>.Success(new AdminReportListDto(dtos, total, request.Page, request.PageSize));
    }
}

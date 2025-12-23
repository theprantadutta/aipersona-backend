using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Admin.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Admin.Commands.ResolveReport;

public record ResolveReportCommand(
    Guid ReportId,
    string Status,
    string? Resolution = null) : IRequest<Result<ResolveReportResultDto>>;

public class ResolveReportCommandHandler : IRequestHandler<ResolveReportCommand, Result<ResolveReportResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public ResolveReportCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<ResolveReportResultDto>> Handle(ResolveReportCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null || !_currentUser.IsAdmin)
            return Result<ResolveReportResultDto>.Failure("Admin access required", 403);

        var report = await _context.ContentReports
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);

        if (report == null)
            return Result<ResolveReportResultDto>.Failure("Report not found", 404);

        if (!Enum.TryParse<ReportStatus>(request.Status, true, out var status))
            return Result<ResolveReportResultDto>.Failure("Invalid status", 400);

        report.Status = status;
        report.ResolvedById = _currentUser.UserId;
        report.Resolution = request.Resolution;
        report.ResolvedAt = _dateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<ResolveReportResultDto>.Success(new ResolveReportResultDto(
            true, "Report resolved"));
    }
}

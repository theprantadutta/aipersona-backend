using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Social.DTOs;
using AiPersona.Domain.Entities;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Social.Commands.ReportContent;

public record ReportContentCommand(
    string ContentType,
    Guid ContentId,
    string Reason,
    string? Description = null) : IRequest<Result<ReportResultDto>>;

public class ReportContentCommandHandler : IRequestHandler<ReportContentCommand, Result<ReportResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public ReportContentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<ReportResultDto>> Handle(ReportContentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<ReportResultDto>.Failure("Unauthorized", 401);

        if (!Enum.TryParse<ContentType>(request.ContentType, true, out var contentType))
            return Result<ReportResultDto>.Failure("Invalid content type", 400);

        // Verify content exists
        var contentExists = contentType switch
        {
            ContentType.Persona => await _context.Personas.AnyAsync(p => p.Id == request.ContentId, cancellationToken),
            ContentType.User => await _context.Users.AnyAsync(u => u.Id == request.ContentId, cancellationToken),
            ContentType.Message => await _context.ChatMessages.AnyAsync(m => m.Id == request.ContentId, cancellationToken),
            _ => false
        };

        if (!contentExists)
            return Result<ReportResultDto>.Failure("Content not found", 404);

        // Check for duplicate report
        var existingReport = await _context.ContentReports
            .FirstOrDefaultAsync(r => r.ReporterId == _currentUser.UserId &&
                                       r.ContentType == contentType &&
                                       r.ContentId == request.ContentId &&
                                       r.Status == ReportStatus.Pending, cancellationToken);

        if (existingReport != null)
            return Result<ReportResultDto>.Success(new ReportResultDto(
                existingReport.Id,
                existingReport.Status.ToString(),
                "Report already submitted"));

        var report = new ContentReport
        {
            Id = Guid.NewGuid(),
            ReporterId = _currentUser.UserId.Value,
            ContentType = contentType,
            ContentId = request.ContentId,
            Reason = request.Reason,
            Description = request.Description,
            Status = ReportStatus.Pending,
            CreatedAt = _dateTime.UtcNow
        };

        _context.ContentReports.Add(report);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<ReportResultDto>.Success(new ReportResultDto(
            report.Id,
            report.Status.ToString(),
            "Report submitted successfully"));
    }
}

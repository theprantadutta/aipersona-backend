using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Admin.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Admin.Commands.SuspendPersona;

public record SuspendPersonaCommand(
    Guid PersonaId,
    string? Reason = null) : IRequest<Result<SuspendResultDto>>;

public class SuspendPersonaCommandHandler : IRequestHandler<SuspendPersonaCommand, Result<SuspendResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public SuspendPersonaCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<SuspendResultDto>> Handle(SuspendPersonaCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null || !_currentUser.IsAdmin)
            return Result<SuspendResultDto>.Failure("Admin access required", 403);

        var persona = await _context.Personas
            .FirstOrDefaultAsync(p => p.Id == request.PersonaId, cancellationToken);

        if (persona == null)
            return Result<SuspendResultDto>.Failure("Persona not found", 404);

        persona.Status = PersonaStatus.Suspended;
        persona.UpdatedAt = _dateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<SuspendResultDto>.Success(new SuspendResultDto(
            true, "Persona suspended", null));
    }
}

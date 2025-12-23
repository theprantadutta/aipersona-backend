using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Personas.Commands.DeletePersona;

public record DeletePersonaCommand(Guid PersonaId) : IRequest<Result>;

public class DeletePersonaCommandHandler : IRequestHandler<DeletePersonaCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public DeletePersonaCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result> Handle(DeletePersonaCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result.Failure("Unauthorized", 401);

        var persona = await _context.Personas
            .FirstOrDefaultAsync(p => p.Id == request.PersonaId, cancellationToken);

        if (persona == null)
            return Result.Failure("Persona not found", 404);

        if (persona.CreatorId != _currentUser.UserId)
            return Result.Failure("Not authorized to delete this persona", 403);

        // Soft delete
        persona.Status = PersonaStatus.Archived;
        persona.UpdatedAt = _dateTime.UtcNow;

        // Update chat sessions that reference this persona
        var sessions = await _context.ChatSessions
            .Where(s => s.PersonaId == persona.Id)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.DeletedPersonaName = persona.Name;
            session.DeletedPersonaImage = persona.ImagePath;
            session.PersonaDeletedAt = _dateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

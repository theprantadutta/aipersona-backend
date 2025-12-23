using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Personas.Commands.DeleteKnowledge;

public record DeleteKnowledgeCommand(Guid PersonaId, Guid KnowledgeId) : IRequest<Result>;

public class DeleteKnowledgeCommandHandler : IRequestHandler<DeleteKnowledgeCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public DeleteKnowledgeCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result> Handle(DeleteKnowledgeCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result.Failure("Unauthorized", 401);

        var persona = await _context.Personas
            .FirstOrDefaultAsync(p => p.Id == request.PersonaId, cancellationToken);

        if (persona == null)
            return Result.Failure("Persona not found", 404);

        if (persona.CreatorId != _currentUser.UserId)
            return Result.Failure("Not authorized", 403);

        var kb = await _context.KnowledgeBases
            .FirstOrDefaultAsync(k => k.Id == request.KnowledgeId && k.PersonaId == request.PersonaId, cancellationToken);

        if (kb == null)
            return Result.Failure("Knowledge base not found", 404);

        _context.KnowledgeBases.Remove(kb);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

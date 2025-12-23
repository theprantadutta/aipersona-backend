using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Chat.DTOs;
using AiPersona.Domain.Entities;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Chat.Commands.CreateSession;

public record CreateSessionCommand(Guid PersonaId) : IRequest<Result<ChatSessionDto>>;

public class CreateSessionCommandValidator : AbstractValidator<CreateSessionCommand>
{
    public CreateSessionCommandValidator()
    {
        RuleFor(x => x.PersonaId).NotEmpty();
    }
}

public class CreateSessionCommandHandler : IRequestHandler<CreateSessionCommand, Result<ChatSessionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public CreateSessionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<ChatSessionDto>> Handle(CreateSessionCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<ChatSessionDto>.Failure("Unauthorized", 401);

        var persona = await _context.Personas
            .FirstOrDefaultAsync(p => p.Id == request.PersonaId, cancellationToken);

        if (persona == null)
            return Result<ChatSessionDto>.Failure("Persona not found", 404);

        // Check access
        if (persona.CreatorId != _currentUser.UserId && !persona.IsPublic)
            return Result<ChatSessionDto>.Failure("Access denied", 403);

        var session = new ChatSession
        {
            UserId = _currentUser.UserId.Value,
            PersonaId = persona.Id,
            PersonaName = persona.Name,
            Status = ChatSessionStatus.Active,
            IsPinned = false,
            MessageCount = 0,
            CreatedAt = _dateTime.UtcNow,
            UpdatedAt = _dateTime.UtcNow
        };

        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<ChatSessionDto>.Created(new ChatSessionDto(
            session.Id, session.UserId, session.PersonaId, session.PersonaName,
            persona.ImagePath, null, session.Status.ToString(), session.IsPinned,
            session.MessageCount, null, session.CreatedAt, session.LastMessageAt,
            session.UpdatedAt, false, null, null, null, null, null));
    }
}

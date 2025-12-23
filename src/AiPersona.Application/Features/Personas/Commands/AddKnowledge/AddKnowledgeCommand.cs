using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Personas.DTOs;
using AiPersona.Domain.Entities;
using AiPersona.Domain.Enums;
using System.Text.Json;

namespace AiPersona.Application.Features.Personas.Commands.AddKnowledge;

public record AddKnowledgeCommand(
    Guid PersonaId,
    string SourceType,
    string? SourceName = null,
    string? Content = null,
    Dictionary<string, object>? MetaData = null) : IRequest<Result<KnowledgeBaseDto>>;

public class AddKnowledgeCommandValidator : AbstractValidator<AddKnowledgeCommand>
{
    public AddKnowledgeCommandValidator()
    {
        RuleFor(x => x.SourceType).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().When(x => x.SourceType != "file");
    }
}

public class AddKnowledgeCommandHandler : IRequestHandler<AddKnowledgeCommand, Result<KnowledgeBaseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public AddKnowledgeCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<KnowledgeBaseDto>> Handle(AddKnowledgeCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<KnowledgeBaseDto>.Failure("Unauthorized", 401);

        var persona = await _context.Personas
            .FirstOrDefaultAsync(p => p.Id == request.PersonaId, cancellationToken);

        if (persona == null)
            return Result<KnowledgeBaseDto>.Failure("Persona not found", 404);

        if (persona.CreatorId != _currentUser.UserId)
            return Result<KnowledgeBaseDto>.Failure("Not authorized", 403);

        if (!Enum.TryParse<KnowledgeSourceType>(request.SourceType, true, out var sourceType))
            return Result<KnowledgeBaseDto>.Failure("Invalid source type", 400);

        var kb = new KnowledgeBase
        {
            PersonaId = persona.Id,
            SourceType = sourceType,
            SourceName = request.SourceName,
            Content = request.Content!,
            MetaData = request.MetaData != null
                ? JsonSerializer.Serialize(request.MetaData)
                : null,
            Status = KnowledgeStatus.Active,
            CreatedAt = _dateTime.UtcNow,
            UpdatedAt = _dateTime.UtcNow
        };

        _context.KnowledgeBases.Add(kb);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<KnowledgeBaseDto>.Created(new KnowledgeBaseDto(
            kb.Id, kb.PersonaId, kb.SourceType.ToString(), kb.SourceName, kb.Content,
            kb.Status.ToString(), kb.MetaData, kb.CreatedAt));
    }
}

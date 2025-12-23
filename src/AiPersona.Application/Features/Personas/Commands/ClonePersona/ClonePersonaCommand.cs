using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Personas.DTOs;
using AiPersona.Domain.Entities;
using AiPersona.Domain.Enums;
using System.Text.Json;

namespace AiPersona.Application.Features.Personas.Commands.ClonePersona;

public record ClonePersonaCommand(Guid PersonaId, string? NewName = null, bool Customize = false)
    : IRequest<Result<PersonaDto>>;

public class ClonePersonaCommandHandler : IRequestHandler<ClonePersonaCommand, Result<PersonaDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public ClonePersonaCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<PersonaDto>> Handle(ClonePersonaCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<PersonaDto>.Failure("Unauthorized", 401);

        var original = await _context.Personas
            .Include(p => p.KnowledgeBases)
            .FirstOrDefaultAsync(p => p.Id == request.PersonaId, cancellationToken);

        if (original == null)
            return Result<PersonaDto>.Failure("Persona not found", 404);

        // Check access: must be owner or public persona
        if (original.CreatorId != _currentUser.UserId && !original.IsPublic)
            return Result<PersonaDto>.Failure("Access denied", 403);

        var clone = new Persona
        {
            CreatorId = _currentUser.UserId.Value,
            Name = request.NewName ?? $"{original.Name} (Clone)",
            Description = original.Description,
            Bio = original.Bio,
            ImagePath = original.ImagePath,
            PersonalityTraits = original.PersonalityTraits?.ToList(),
            LanguageStyle = original.LanguageStyle,
            Expertise = original.Expertise?.ToList(),
            Tags = original.Tags?.ToList(),
            VoiceId = original.VoiceId,
            VoiceSettings = original.VoiceSettings,
            IsPublic = false,
            IsMarketplace = false,
            Status = request.Customize ? PersonaStatus.Draft : PersonaStatus.Active,
            ClonedFromPersonaId = original.Id,
            CreatedAt = _dateTime.UtcNow,
            UpdatedAt = _dateTime.UtcNow
        };

        _context.Personas.Add(clone);

        // Clone knowledge bases
        if (original.KnowledgeBases != null)
        {
            foreach (var kb in original.KnowledgeBases.Where(k => k.Status == KnowledgeStatus.Active))
            {
                var clonedKb = new KnowledgeBase
                {
                    PersonaId = clone.Id,
                    SourceType = kb.SourceType,
                    SourceName = kb.SourceName,
                    Content = kb.Content,
                    MetaData = kb.MetaData,
                    Status = KnowledgeStatus.Active,
                    CreatedAt = _dateTime.UtcNow,
                    UpdatedAt = _dateTime.UtcNow
                };
                _context.KnowledgeBases.Add(clonedKb);
            }
        }

        // Increment clone count on original
        original.CloneCount++;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<PersonaDto>.Created(new PersonaDto(
            clone.Id, clone.CreatorId, clone.Name, clone.Description, clone.Bio, clone.ImagePath,
            clone.PersonalityTraits, clone.LanguageStyle, clone.Expertise, clone.Tags,
            clone.VoiceId, clone.VoiceSettings, clone.IsPublic, clone.IsMarketplace,
            clone.Status.ToString(), clone.LikeCount, clone.ViewCount, clone.CloneCount,
            clone.CreatedAt, clone.UpdatedAt, true, false));
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Personas.DTOs;
using AiPersona.Domain.Enums;
using System.Text.Json;

namespace AiPersona.Application.Features.Personas.Commands.UpdatePersona;

public record UpdatePersonaCommand(
    Guid PersonaId,
    string? Name = null,
    string? Description = null,
    string? Bio = null,
    string? ImagePath = null,
    List<string>? PersonalityTraits = null,
    string? LanguageStyle = null,
    List<string>? Expertise = null,
    List<string>? Tags = null,
    string? VoiceId = null,
    Dictionary<string, object>? VoiceSettings = null,
    bool? IsPublic = null,
    bool? IsMarketplace = null,
    string? Status = null) : IRequest<Result<PersonaDto>>;

public class UpdatePersonaCommandHandler : IRequestHandler<UpdatePersonaCommand, Result<PersonaDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public UpdatePersonaCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<PersonaDto>> Handle(UpdatePersonaCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<PersonaDto>.Failure("Unauthorized", 401);

        var persona = await _context.Personas
            .FirstOrDefaultAsync(p => p.Id == request.PersonaId, cancellationToken);

        if (persona == null)
            return Result<PersonaDto>.Failure("Persona not found", 404);

        if (persona.CreatorId != _currentUser.UserId)
            return Result<PersonaDto>.Failure("Not authorized to update this persona", 403);

        if (request.Name != null) persona.Name = request.Name;
        if (request.Description != null) persona.Description = request.Description;
        if (request.Bio != null) persona.Bio = request.Bio;
        if (request.ImagePath != null) persona.ImagePath = request.ImagePath;
        if (request.PersonalityTraits != null) persona.PersonalityTraits = request.PersonalityTraits;
        if (request.LanguageStyle != null) persona.LanguageStyle = request.LanguageStyle;
        if (request.Expertise != null) persona.Expertise = request.Expertise;
        if (request.Tags != null) persona.Tags = request.Tags;
        if (request.VoiceId != null) persona.VoiceId = request.VoiceId;
        if (request.VoiceSettings != null)
            persona.VoiceSettings = JsonSerializer.Serialize(request.VoiceSettings);
        if (request.IsPublic.HasValue) persona.IsPublic = request.IsPublic.Value;
        if (request.IsMarketplace.HasValue) persona.IsMarketplace = request.IsMarketplace.Value;
        if (request.Status != null && Enum.TryParse<PersonaStatus>(request.Status, true, out var status))
            persona.Status = status;

        persona.UpdatedAt = _dateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<PersonaDto>.Success(new PersonaDto(
            persona.Id, persona.CreatorId, persona.Name, persona.Description, persona.Bio, persona.ImagePath,
            persona.PersonalityTraits, persona.LanguageStyle, persona.Expertise, persona.Tags,
            persona.VoiceId, persona.VoiceSettings, persona.IsPublic, persona.IsMarketplace,
            persona.Status.ToString(), persona.LikeCount, persona.ViewCount, persona.CloneCount,
            persona.CreatedAt, persona.UpdatedAt, true, false));
    }
}

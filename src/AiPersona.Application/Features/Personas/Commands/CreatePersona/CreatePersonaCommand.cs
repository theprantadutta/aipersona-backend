using FluentValidation;
using MediatR;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Personas.DTOs;
using AiPersona.Domain.Entities;
using AiPersona.Domain.Enums;
using System.Text.Json;

namespace AiPersona.Application.Features.Personas.Commands.CreatePersona;

public record CreatePersonaCommand(
    string Name,
    string? Description = null,
    string? Bio = null,
    List<string>? PersonalityTraits = null,
    string? LanguageStyle = null,
    List<string>? Expertise = null,
    List<string>? Tags = null,
    string? VoiceId = null,
    Dictionary<string, object>? VoiceSettings = null,
    bool IsPublic = false,
    bool IsMarketplace = false) : IRequest<Result<PersonaDto>>;

public class CreatePersonaCommandValidator : AbstractValidator<CreatePersonaCommand>
{
    public CreatePersonaCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => x.Description != null);

        RuleFor(x => x.Bio)
            .MaximumLength(2000).When(x => x.Bio != null);
    }
}

public class CreatePersonaCommandHandler : IRequestHandler<CreatePersonaCommand, Result<PersonaDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;

    public CreatePersonaCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<PersonaDto>> Handle(CreatePersonaCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<PersonaDto>.Failure("Unauthorized", 401);

        var persona = new Persona
        {
            CreatorId = _currentUser.UserId.Value,
            Name = request.Name,
            Description = request.Description,
            Bio = request.Bio,
            PersonalityTraits = request.PersonalityTraits,
            LanguageStyle = request.LanguageStyle,
            Expertise = request.Expertise,
            Tags = request.Tags,
            VoiceId = request.VoiceId,
            VoiceSettings = request.VoiceSettings != null
                ? JsonSerializer.Serialize(request.VoiceSettings)
                : null,
            IsPublic = request.IsPublic,
            IsMarketplace = request.IsMarketplace,
            Status = PersonaStatus.Active,
            CreatedAt = _dateTime.UtcNow,
            UpdatedAt = _dateTime.UtcNow
        };

        _context.Personas.Add(persona);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<PersonaDto>.Created(MapToDto(persona, true, false));
    }

    private static PersonaDto MapToDto(Persona p, bool isOwner, bool isLiked) => new(
        p.Id, p.CreatorId, p.Name, p.Description, p.Bio, p.ImagePath,
        p.PersonalityTraits, p.LanguageStyle, p.Expertise, p.Tags,
        p.VoiceId, p.VoiceSettings, p.IsPublic, p.IsMarketplace,
        p.Status.ToString(), p.LikeCount, p.ViewCount, p.CloneCount,
        p.CreatedAt, p.UpdatedAt, isOwner, isLiked);
}

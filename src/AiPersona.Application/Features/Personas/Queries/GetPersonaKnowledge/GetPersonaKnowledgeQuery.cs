using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Personas.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Personas.Queries.GetPersonaKnowledge;

public record GetPersonaKnowledgeQuery(Guid PersonaId) : IRequest<Result<List<KnowledgeBaseDto>>>;

public class GetPersonaKnowledgeQueryHandler : IRequestHandler<GetPersonaKnowledgeQuery, Result<List<KnowledgeBaseDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetPersonaKnowledgeQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<List<KnowledgeBaseDto>>> Handle(GetPersonaKnowledgeQuery request, CancellationToken cancellationToken)
    {
        var persona = await _context.Personas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PersonaId, cancellationToken);

        if (persona == null)
            return Result<List<KnowledgeBaseDto>>.Failure("Persona not found", 404);

        // Check access
        var isOwner = _currentUser.UserId.HasValue && persona.CreatorId == _currentUser.UserId;
        if (!isOwner && !persona.IsPublic)
            return Result<List<KnowledgeBaseDto>>.Failure("Access denied", 403);

        var knowledgeBases = await _context.KnowledgeBases
            .Where(kb => kb.PersonaId == request.PersonaId && kb.Status == KnowledgeStatus.Active)
            .OrderByDescending(kb => kb.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var dtos = knowledgeBases.Select(kb => new KnowledgeBaseDto(
            kb.Id, kb.PersonaId, kb.SourceType.ToString(), kb.SourceName, kb.Content,
            kb.Status.ToString(), kb.MetaData, kb.CreatedAt)).ToList();

        return Result<List<KnowledgeBaseDto>>.Success(dtos);
    }
}

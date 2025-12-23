using System.Diagnostics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Ai.DTOs;
using AiPersona.Domain.Entities;

namespace AiPersona.Application.Features.Ai.Commands.GenerateResponse;

public record GenerateResponseCommand(
    Guid PersonaId,
    string Message,
    Guid? SessionId = null) : IRequest<Result<AiResponseDto>>;

public class GenerateResponseCommandHandler : IRequestHandler<GenerateResponseCommand, Result<AiResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IGeminiService _geminiService;

    public GenerateResponseCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IGeminiService geminiService)
    {
        _context = context;
        _currentUser = currentUser;
        _geminiService = geminiService;
    }

    public async Task<Result<AiResponseDto>> Handle(GenerateResponseCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<AiResponseDto>.Failure("Unauthorized", 401);

        var persona = await _context.Personas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PersonaId, cancellationToken);

        if (persona == null)
            return Result<AiResponseDto>.Failure("Persona not found", 404);

        if (!persona.IsPublic && persona.CreatorId != _currentUser.UserId)
            return Result<AiResponseDto>.Failure("Access denied", 403);

        var history = new List<ChatMessage>();

        if (request.SessionId.HasValue)
        {
            history = await _context.ChatMessages
                .Where(m => m.SessionId == request.SessionId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(20)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            history = history.OrderBy(m => m.CreatedAt).ToList();
        }

        var stopwatch = Stopwatch.StartNew();
        var result = await _geminiService.GenerateResponseAsync(
            persona,
            history,
            request.Message,
            cancellationToken);
        stopwatch.Stop();

        return Result<AiResponseDto>.Success(new AiResponseDto(
            result.Text,
            result.TokensUsed,
            "gemini",
            stopwatch.Elapsed.TotalMilliseconds));
    }
}

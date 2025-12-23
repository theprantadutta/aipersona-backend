using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Ai.DTOs;

namespace AiPersona.Application.Features.Ai.Commands.StreamResponse;

public record StreamResponseCommand(
    Guid PersonaId,
    string Message,
    Guid? SessionId = null,
    List<ConversationHistoryItem>? History = null) : IRequest<Result<StreamResponseDto>>;

public class StreamResponseCommandHandler : IRequestHandler<StreamResponseCommand, Result<StreamResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public StreamResponseCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<StreamResponseDto>> Handle(StreamResponseCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<StreamResponseDto>.Failure("Unauthorized", 401);

        var persona = await _context.Personas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PersonaId, cancellationToken);

        if (persona == null)
            return Result<StreamResponseDto>.Failure("Persona not found", 404);

        if (!persona.IsPublic && persona.CreatorId != _currentUser.UserId)
            return Result<StreamResponseDto>.Failure("Access denied", 403);

        // Streaming is handled by SignalR ChatHub
        // This command validates the request and returns session info
        var sessionId = request.SessionId?.ToString() ?? Guid.NewGuid().ToString();

        return Result<StreamResponseDto>.Success(new StreamResponseDto(
            sessionId,
            "ready",
            "Connect to SignalR hub for streaming response"));
    }
}

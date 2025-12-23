using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Chat.DTOs;

namespace AiPersona.Application.Features.Chat.Commands.ExportSession;

public record ExportSessionCommand(
    Guid SessionId,
    string Format = "json",
    bool IncludeTimestamps = true,
    bool IncludeMetadata = false) : IRequest<Result<ExportResultDto>>;

public class ExportSessionCommandHandler : IRequestHandler<ExportSessionCommand, Result<ExportResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ExportSessionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<ExportResultDto>> Handle(ExportSessionCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<ExportResultDto>.Failure("Unauthorized", 401);

        var session = await _context.ChatSessions
            .Include(s => s.Persona)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == _currentUser.UserId, cancellationToken);

        if (session == null)
            return Result<ExportResultDto>.Failure("Session not found", 404);

        var messages = await _context.ChatMessages
            .Where(m => m.SessionId == session.Id)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        var content = request.Format.ToLower() switch
        {
            "txt" => ExportToText(session, messages, request.IncludeTimestamps),
            "json" => ExportToJson(session, messages, request.IncludeTimestamps, request.IncludeMetadata),
            _ => ExportToJson(session, messages, request.IncludeTimestamps, request.IncludeMetadata)
        };

        var fileName = $"chat_{session.PersonaName}_{session.CreatedAt:yyyyMMdd}.{request.Format}";

        return Result<ExportResultDto>.Success(new ExportResultDto(request.Format, content, fileName));
    }

    private static string ExportToText(Domain.Entities.ChatSession session,
        List<Domain.Entities.ChatMessage> messages, bool includeTimestamps)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Chat with {session.PersonaName}");
        sb.AppendLine($"Started: {session.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine(new string('-', 50));
        sb.AppendLine();

        foreach (var msg in messages)
        {
            var sender = msg.SenderType.ToString();
            if (includeTimestamps)
            {
                sb.AppendLine($"[{msg.CreatedAt:HH:mm:ss}] {sender}:");
            }
            else
            {
                sb.AppendLine($"{sender}:");
            }
            sb.AppendLine(msg.Text);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string ExportToJson(Domain.Entities.ChatSession session,
        List<Domain.Entities.ChatMessage> messages, bool includeTimestamps, bool includeMetadata)
    {
        var export = new
        {
            session = new
            {
                id = session.Id,
                persona = session.PersonaName,
                createdAt = session.CreatedAt,
                messageCount = session.MessageCount,
                metadata = includeMetadata ? session.MetaData : null
            },
            messages = messages.Select(m => new
            {
                sender = m.SenderType.ToString(),
                text = m.Text,
                timestamp = includeTimestamps ? m.CreatedAt : (DateTime?)null,
                tokensUsed = m.TokensUsed
            }).ToList()
        };

        return JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
    }
}

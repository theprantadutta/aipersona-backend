using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Social.DTOs;

namespace AiPersona.Application.Features.Social.Commands.UnblockUser;

public record UnblockUserCommand(Guid UserId) : IRequest<Result<BlockResultDto>>;

public class UnblockUserCommandHandler : IRequestHandler<UnblockUserCommand, Result<BlockResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UnblockUserCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<BlockResultDto>> Handle(UnblockUserCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<BlockResultDto>.Failure("Unauthorized", 401);

        var existingBlock = await _context.UserBlocks
            .FirstOrDefaultAsync(b => b.BlockerId == _currentUser.UserId && b.BlockedId == request.UserId, cancellationToken);

        if (existingBlock == null)
            return Result<BlockResultDto>.Success(new BlockResultDto(false, "User is not blocked"));

        _context.UserBlocks.Remove(existingBlock);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<BlockResultDto>.Success(new BlockResultDto(false, "User unblocked successfully"));
    }
}

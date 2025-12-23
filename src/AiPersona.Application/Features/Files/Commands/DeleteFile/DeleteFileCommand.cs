using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Files.DTOs;

namespace AiPersona.Application.Features.Files.Commands.DeleteFile;

public record DeleteFileCommand(Guid FileId) : IRequest<Result<DeleteResultDto>>;

public class DeleteFileCommandHandler : IRequestHandler<DeleteFileCommand, Result<DeleteResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileService _fileService;

    public DeleteFileCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IFileService fileService)
    {
        _context = context;
        _currentUser = currentUser;
        _fileService = fileService;
    }

    public async Task<Result<DeleteResultDto>> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<DeleteResultDto>.Failure("Unauthorized", 401);

        var file = await _context.UploadedFiles
            .FirstOrDefaultAsync(f => f.Id == request.FileId && f.UserId == _currentUser.UserId, cancellationToken);

        if (file == null)
            return Result<DeleteResultDto>.Failure("File not found", 404);

        if (!string.IsNullOrEmpty(file.FilePath))
        {
            await _fileService.DeleteAsync(file.FilePath, cancellationToken);
        }

        _context.UploadedFiles.Remove(file);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<DeleteResultDto>.Success(new DeleteResultDto(true, "File deleted successfully"));
    }
}

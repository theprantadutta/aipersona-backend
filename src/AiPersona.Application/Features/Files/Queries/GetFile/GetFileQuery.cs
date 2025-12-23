using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Files.DTOs;

namespace AiPersona.Application.Features.Files.Queries.GetFile;

public record GetFileQuery(Guid FileId) : IRequest<Result<FileDto>>;

public class GetFileQueryHandler : IRequestHandler<GetFileQuery, Result<FileDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileService _fileService;

    public GetFileQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IFileService fileService)
    {
        _context = context;
        _currentUser = currentUser;
        _fileService = fileService;
    }

    public async Task<Result<FileDto>> Handle(GetFileQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<FileDto>.Failure("Unauthorized", 401);

        var file = await _context.UploadedFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == request.FileId && f.UserId == _currentUser.UserId, cancellationToken);

        if (file == null)
            return Result<FileDto>.Failure("File not found", 404);

        var url = _fileService.GetPublicUrl(file.FilePath);

        return Result<FileDto>.Success(new FileDto(
            file.Id,
            file.FilePath,
            file.OriginalName,
            file.Category.ToString(),
            file.MimeType,
            file.FileSize,
            url,
            file.CreatedAt));
    }
}

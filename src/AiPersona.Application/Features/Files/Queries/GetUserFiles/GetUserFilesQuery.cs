using MediatR;
using Microsoft.EntityFrameworkCore;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Files.DTOs;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Files.Queries.GetUserFiles;

public record GetUserFilesQuery(
    string? Category = null,
    int Page = 1,
    int PageSize = 50) : IRequest<Result<FileListDto>>;

public class GetUserFilesQueryHandler : IRequestHandler<GetUserFilesQuery, Result<FileListDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileService _fileService;

    public GetUserFilesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IFileService fileService)
    {
        _context = context;
        _currentUser = currentUser;
        _fileService = fileService;
    }

    public async Task<Result<FileListDto>> Handle(GetUserFilesQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<FileListDto>.Failure("Unauthorized", 401);

        var query = _context.UploadedFiles
            .Where(f => f.UserId == _currentUser.UserId)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(request.Category) && Enum.TryParse<FileCategory>(request.Category, true, out var category))
            query = query.Where(f => f.Category == category);

        var total = await query.CountAsync(cancellationToken);
        var totalSize = await query.SumAsync(f => (long)f.FileSize, cancellationToken);
        var skip = (request.Page - 1) * request.PageSize;

        var files = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = files.Select(f => new FileDto(
            f.Id,
            f.FilePath,
            f.OriginalName,
            f.Category.ToString(),
            f.MimeType,
            f.FileSize,
            _fileService.GetPublicUrl(f.FilePath),
            f.CreatedAt)).ToList();

        return Result<FileListDto>.Success(new FileListDto(dtos, total, totalSize, request.Page, request.PageSize));
    }
}

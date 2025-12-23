using MediatR;
using AiPersona.Application.Common;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Application.Features.Files.DTOs;
using AiPersona.Domain.Entities;
using AiPersona.Domain.Enums;

namespace AiPersona.Application.Features.Files.Commands.UploadFile;

public record UploadFileCommand(
    Stream FileStream,
    string FileName,
    string Category) : IRequest<Result<UploadResultDto>>;

public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, Result<UploadResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileService _fileService;
    private readonly IDateTimeService _dateTime;

    public UploadFileCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IFileService fileService,
        IDateTimeService dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _fileService = fileService;
        _dateTime = dateTime;
    }

    public async Task<Result<UploadResultDto>> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            return Result<UploadResultDto>.Failure("Unauthorized", 401);

        if (!Enum.TryParse<FileCategory>(request.Category, true, out var category))
            return Result<UploadResultDto>.Failure("Invalid file category", 400);

        var uploadResult = await _fileService.UploadAsync(
            request.FileStream,
            request.FileName,
            category,
            cancellationToken);

        var file = new UploadedFile
        {
            Id = Guid.NewGuid(),
            UserId = _currentUser.UserId.Value,
            FilePath = uploadResult.FilePath,
            OriginalName = uploadResult.OriginalName,
            FileSize = uploadResult.FileSize,
            MimeType = uploadResult.MimeType,
            Category = category,
            CreatedAt = _dateTime.UtcNow
        };

        _context.UploadedFiles.Add(file);
        await _context.SaveChangesAsync(cancellationToken);

        var url = _fileService.GetPublicUrl(file.FilePath);

        return Result<UploadResultDto>.Success(new UploadResultDto(
            file.Id,
            file.OriginalName,
            url,
            "File uploaded successfully"));
    }
}

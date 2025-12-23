namespace AiPersona.Application.Features.Files.DTOs;

public record FileDto(
    Guid Id,
    string FileName,
    string? OriginalName,
    string Category,
    string ContentType,
    long FileSize,
    string? Url,
    DateTime CreatedAt);

public record FileListDto(
    List<FileDto> Files,
    int Total,
    long TotalSizeBytes,
    int Page,
    int PageSize);

public record UploadResultDto(
    Guid FileId,
    string FileName,
    string Url,
    string Message);

public record DeleteResultDto(
    bool Success,
    string Message);

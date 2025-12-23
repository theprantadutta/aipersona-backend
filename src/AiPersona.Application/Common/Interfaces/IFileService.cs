using AiPersona.Domain.Enums;

namespace AiPersona.Application.Common.Interfaces;

public record UploadResult(string FilePath, string OriginalName, int FileSize, string MimeType);

public interface IFileService
{
    Task<UploadResult> UploadAsync(Stream fileStream, string fileName, FileCategory category, CancellationToken cancellationToken = default);
    Task<Stream?> DownloadAsync(string filePath, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string filePath, CancellationToken cancellationToken = default);
    string GetPublicUrl(string filePath);
}

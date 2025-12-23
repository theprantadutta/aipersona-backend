using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AiPersona.Infrastructure.Services;

public interface IFileRunnerService
{
    Task<FileRunnerUploadResult> UploadFileAsync(
        byte[] fileContent,
        string filename,
        string contentType,
        string category = "misc",
        CancellationToken cancellationToken = default);

    string GetFileUrl(string fileId);
}

public record FileRunnerUploadResult(
    string FileId,
    string OriginalName,
    long Size,
    string MimeType,
    string DownloadUrl,
    string FolderPath);

public class FileRunnerService : IFileRunnerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FileRunnerService> _logger;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    private static readonly Dictionary<string, string> FolderMapping = new()
    {
        ["avatar"] = "avatars",
        ["persona_image"] = "persona_images",
        ["chat_attachment"] = "chat_attachments",
        ["knowledge_base"] = "knowledge_base"
    };

    public FileRunnerService(HttpClient httpClient, IConfiguration configuration, ILogger<FileRunnerService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _baseUrl = !string.IsNullOrEmpty(configuration["FileRunner:BaseUrl"])
            ? configuration["FileRunner:BaseUrl"]!
            : Environment.GetEnvironmentVariable("FILERUNNER_BASE_URL")
              ?? throw new InvalidOperationException("FileRunner base URL is not configured");

        _apiKey = !string.IsNullOrEmpty(configuration["FileRunner:ApiKey"])
            ? configuration["FileRunner:ApiKey"]!
            : Environment.GetEnvironmentVariable("FILERUNNER_API_KEY")
            ?? throw new InvalidOperationException("FileRunner API key is not configured");
    }

    public async Task<FileRunnerUploadResult> UploadFileAsync(
        byte[] fileContent,
        string filename,
        string contentType,
        string category = "misc",
        CancellationToken cancellationToken = default)
    {
        var folderPath = FolderMapping.GetValueOrDefault(category, "misc");

        using var content = new MultipartFormDataContent();
        var fileContentBytes = new ByteArrayContent(fileContent);
        fileContentBytes.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContentBytes, "file", filename);
        content.Add(new StringContent(folderPath), "folder_path");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/upload")
        {
            Content = content
        };
        request.Headers.Add("X-API-Key", _apiKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("FileRunner upload failed: {StatusCode} - {Error}", response.StatusCode, errorBody);
            throw new Exception($"FileRunner upload failed: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        var uploadResult = new FileRunnerUploadResult(
            FileId: result.GetProperty("file_id").GetString()!,
            OriginalName: result.GetProperty("original_name").GetString()!,
            Size: result.GetProperty("size").GetInt64(),
            MimeType: result.GetProperty("mime_type").GetString()!,
            DownloadUrl: result.GetProperty("download_url").GetString()!,
            FolderPath: folderPath
        );

        _logger.LogInformation("File uploaded to FileRunner: {FileId}", uploadResult.FileId);
        return uploadResult;
    }

    public string GetFileUrl(string fileId)
    {
        return $"{_baseUrl}/api/files/{fileId}";
    }
}

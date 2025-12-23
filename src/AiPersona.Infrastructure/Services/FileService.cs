using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Domain.Enums;

namespace AiPersona.Infrastructure.Services;

public class FileService : IFileService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FileService> _logger;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public FileService(HttpClient httpClient, IConfiguration configuration, ILogger<FileService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _baseUrl = configuration["FileRunner:BaseUrl"]
            ?? Environment.GetEnvironmentVariable("FILERUNNER_URL")
            ?? throw new InvalidOperationException("FileRunner base URL is not configured");

        _apiKey = configuration["FileRunner:ApiKey"]
            ?? Environment.GetEnvironmentVariable("FILERUNNER_API_KEY")
            ?? throw new InvalidOperationException("FileRunner API key is not configured");
    }

    public async Task<UploadResult> UploadAsync(Stream fileStream, string fileName, FileCategory category, CancellationToken cancellationToken = default)
    {
        var folder = category switch
        {
            FileCategory.Avatar => "avatars",
            FileCategory.PersonaImage => "personas",
            FileCategory.ChatAttachment => "attachments",
            FileCategory.KnowledgeBase => "knowledge",
            _ => "misc"
        };

        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = $"{folder}/{uniqueFileName}";

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(fileName));
        content.Add(streamContent, "file", fileName);
        content.Add(new StringContent(filePath), "path");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/upload")
        {
            Content = content
        };
        request.Headers.Add("X-Api-Key", _apiKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var fileSize = (int)fileStream.Length;
        var mimeType = GetMimeType(fileName);

        _logger.LogInformation("File uploaded successfully: {Path}", filePath);

        return new UploadResult(filePath, fileName, fileSize, mimeType);
    }

    public async Task<Stream?> DownloadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/download/{filePath}");
        request.Headers.Add("X-Api-Key", _apiKey);

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to download file: {Path}, Status: {Status}", filePath, response.StatusCode);
            return null;
        }

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/delete/{filePath}");
        request.Headers.Add("X-Api-Key", _apiKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("File deleted successfully: {Path}", filePath);
            return true;
        }

        _logger.LogWarning("Failed to delete file: {Path}, Status: {Status}", filePath, response.StatusCode);
        return false;
    }

    public string GetPublicUrl(string filePath)
    {
        return $"{_baseUrl}/files/{filePath}";
    }

    private static string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".mp4" => "video/mp4",
            _ => "application/octet-stream"
        };
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiPersona.Application.Features.Files.Commands.UploadFile;
using AiPersona.Application.Features.Files.Commands.DeleteFile;
using AiPersona.Application.Features.Files.Queries.GetFile;
using AiPersona.Application.Features.Files.Queries.GetUserFiles;

namespace AiPersona.Api.Controllers.V1;

public class FilesController : BaseApiController
{
    /// <summary>
    /// Upload a file
    /// </summary>
    [HttpPost("upload")]
    [Authorize]
    public async Task<ActionResult> UploadFile(
        IFormFile file,
        [FromQuery] string category = "general")
    {
        var result = await Mediator.Send(new UploadFileCommand(
            file.OpenReadStream(),
            file.FileName,
            category));
        return HandleResult(result);
    }

    /// <summary>
    /// Get file by ID
    /// </summary>
    [HttpGet("{fileId:guid}")]
    [Authorize]
    public async Task<ActionResult> GetFile(Guid fileId)
    {
        var result = await Mediator.Send(new GetFileQuery(fileId));
        return HandleResult(result);
    }

    /// <summary>
    /// Get user's files
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult> GetUserFiles(
        [FromQuery] string? category = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetUserFilesQuery(category, page, pageSize));
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a file
    /// </summary>
    [HttpDelete("{fileId:guid}")]
    [Authorize]
    public async Task<ActionResult> DeleteFile(Guid fileId)
    {
        var result = await Mediator.Send(new DeleteFileCommand(fileId));
        return HandleResult(result);
    }
}

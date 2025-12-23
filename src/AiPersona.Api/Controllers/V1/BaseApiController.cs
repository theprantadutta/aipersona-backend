using MediatR;
using Microsoft.AspNetCore.Mvc;
using AiPersona.Application.Common;

namespace AiPersona.Api.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected ActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return result.StatusCode == 201
                ? StatusCode(201, result.Value)
                : Ok(result.Value);
        }

        return result.StatusCode switch
        {
            401 => Unauthorized(new { error = result.Error }),
            403 => Forbid(),
            404 => NotFound(new { error = result.Error }),
            409 => Conflict(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
    }

    protected ActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
        {
            return result.StatusCode == 204 ? NoContent() : Ok();
        }

        return result.StatusCode switch
        {
            401 => Unauthorized(new { error = result.Error }),
            403 => Forbid(),
            404 => NotFound(new { error = result.Error }),
            409 => Conflict(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
    }
}

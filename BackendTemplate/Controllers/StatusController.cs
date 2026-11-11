using Microsoft.AspNetCore.Mvc;
using BackendTemplate.Resp;

namespace BackendTemplate.Controllers;

[ApiController]
[Route("api/status")]
public sealed class StatusController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ResponseDto>(StatusCodes.Status200OK)]
    public ServerResponse Get() => ServerResponse.Ok(new StatusResponse("BackendTemplate", "ok"));
}

public sealed record StatusResponse(string Service, string Status);

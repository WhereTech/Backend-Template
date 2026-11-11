using System.ComponentModel.DataAnnotations;
using BackendTemplate.Model;
using BackendTemplate.Resp;
using BackendTemplate.Server;
using BackendTemplate.Services;
using Microsoft.AspNetCore.Mvc;

namespace BackendTemplate.Controllers;

[ApiController]
[Route("login")]
[ServiceFilter<UseTransactionFilterService>]
public class LoginController(AuthService authService) : ControllerBase
{
    public record SendSmsRequest(
        [Required, MaxLength(20)] string mobile,
        SmsOperation operation);

    [HttpPost("send-sms")]
    public async Task<ServerResponse> SendSms([FromBody] SendSmsRequest request)
    {
        await authService.SendSms(request.mobile, request.operation);
        return ServerResponse.Ok();
    }

    public record RegisterRequest(
        [Required, MaxLength(50)] string name,
        [Required, MaxLength(20)] string mobile,
        [Required, StringLength(6, MinimumLength = 6)] string code);

    [HttpPost("register")]
    public async Task<ServerResponse> Register([FromBody] RegisterRequest request)
    {
        var user = await authService.Register(request.name, request.mobile, request.code);
        return ServerResponse.Ok(new { user, authService.Session!.token });
    }

    public record LoginRequest(
        [Required, MaxLength(20)] string mobile,
        [Required, StringLength(6, MinimumLength = 6)] string code);

    [HttpPost]
    public async Task<ServerResponse> Login([FromBody] LoginRequest request)
    {
        await authService.Login(request.mobile, request.code);
        return ServerResponse.Ok(new { user = authService.CurrentUser, authService.Session!.token });
    }

    [HttpGet("current")]
    public ServerResponse Current()
    {
        authService.RequireLogin();
        return ServerResponse.Ok(authService.CurrentUser);
    }

    [HttpPost("logout")]
    public async Task<ServerResponse> Logout()
    {
        await authService.Logout();
        return ServerResponse.Ok();
    }
}

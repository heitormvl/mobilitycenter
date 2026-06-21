using Microsoft.AspNetCore.Mvc;
using MobilityCenter.Business.Interfaces;
using MobilityCenter.Shared.DTOs.Usuario;

namespace MobilityCenter.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var response = await _authService.LoginAsync(dto);
        return Ok(response);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CriarUsuarioDto dto)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var response = await _authService.RegisterAsync(dto, baseUrl);
        return Ok(response);
    }

    [HttpGet("confirmar-email")]
    public async Task<IActionResult> ConfirmarEmail([FromQuery] string userId, [FromQuery] string token)
    {
        var response = await _authService.ConfirmarEmailAsync(userId, token);

        var frontendUrl = HttpContext.RequestServices
            .GetService<IConfiguration>()
            ?.GetValue<string>("App:FrontendUrl");

        if (!string.IsNullOrEmpty(frontendUrl))
            return Redirect($"{frontendUrl}/login?emailConfirmado=true");

        return Ok(response);
    }

    [HttpPost("google")]
    public async Task<IActionResult> Google([FromBody] GoogleLoginDto dto)
    {
        var response = await _authService.LoginWithGoogleAsync(dto.IdToken);
        return Ok(response);
    }
}

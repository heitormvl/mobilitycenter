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
        var response = await _authService.RegisterAsync(dto);
        return StatusCode(201, response);
    }

    [HttpPost("google")]
    public async Task<IActionResult> Google([FromBody] GoogleLoginDto dto)
    {
        var response = await _authService.LoginWithGoogleAsync(dto.IdToken);
        return Ok(response);
    }

    [HttpGet("confirmar-email")]
    public async Task<IActionResult> ConfirmarEmail([FromQuery] string userId, [FromQuery] string token)
    {
        await _authService.ConfirmEmailAsync(userId, token);
        return Ok(new { message = "E-mail confirmado com sucesso!" });
    }

    [HttpPost("reenviar-confirmacao")]
    public async Task<IActionResult> ReenviarConfirmacao([FromBody] ReenviarConfirmacaoDto dto)
    {
        await _authService.ReenviarConfirmacaoAsync(dto.Email);
        return Ok(new { message = "E-mail de confirmação reenviado." });
    }
}

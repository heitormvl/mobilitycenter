using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var response = await _authService.LoginAsync(dto);
        return Ok(response);
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
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
    [EnableRateLimiting("auth-relaxed")]
    public async Task<IActionResult> ReenviarConfirmacao([FromBody] ReenviarConfirmacaoDto dto)
    {
        await _authService.ReenviarConfirmacaoAsync(dto.Email);
        return Ok(new { message = "E-mail de confirmação reenviado." });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        var response = await _authService.RefreshAsync(dto.Token);
        return Ok(response);
    }

    [HttpPost("esqueci-senha")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> EsquecerSenha([FromBody] EsquecerSenhaDto dto)
    {
        await _authService.EsquecerSenhaAsync(dto.Email);
        return Ok(new { message = "Se o e-mail estiver cadastrado, você receberá um link de redefinição." });
    }

    [HttpPost("redefinir-senha")]
    public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaDto dto)
    {
        await _authService.RedefinirSenhaAsync(dto.Email, dto.Token, dto.NovaSenha);
        return Ok(new { message = "Senha redefinida com sucesso!" });
    }
}

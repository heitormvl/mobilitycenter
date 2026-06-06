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
        return Created("/api/usuarios/me", response);
    }
}

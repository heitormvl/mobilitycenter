using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MobilityCenter.Business.Interfaces;
using MobilityCenter.Shared.DTOs.Usuario;
using MobilityCenter.Shared.Exceptions;
using MobilityCenter.Shared.Models;

namespace MobilityCenter.Business.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<Usuario> _userManager;
    private readonly IConfiguration _configuration;

    public AuthService(UserManager<Usuario> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var usuario = await _userManager.FindByEmailAsync(dto.Email)
            ?? throw new AppException("Credenciais inválidas.", 401);

        var senhaCorreta = await _userManager.CheckPasswordAsync(usuario, dto.Password);
        if (!senhaCorreta)
            throw new AppException("Credenciais inválidas.", 401);

        return new AuthResponseDto
        {
            Token = GerarToken(usuario),
            Usuario = MapearUsuario(usuario)
        };
    }

    public async Task<AuthResponseDto> RegisterAsync(CriarUsuarioDto dto)
    {
        var existente = await _userManager.FindByEmailAsync(dto.Email);
        if (existente != null)
            throw new ConflictException("E-mail já cadastrado.");

        var usuario = new Usuario
        {
            DisplayName = dto.DisplayName,
            Email = dto.Email,
            UserName = dto.Email,
            Type = dto.Type,
            CreatedAt = DateTime.UtcNow
        };

        var resultado = await _userManager.CreateAsync(usuario, dto.Password);
        if (!resultado.Succeeded)
        {
            var erros = string.Join(", ", resultado.Errors.Select(e => e.Description));
            throw new ValidationException(erros);
        }

        return new AuthResponseDto
        {
            Token = GerarToken(usuario),
            Usuario = MapearUsuario(usuario)
        };
    }

    private string GerarToken(Usuario usuario)
    {
        var secretKey = _configuration["Jwt:SecretKey"]!;
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email!),
            new Claim("displayName", usuario.DisplayName),
            new Claim("tipo", ((int)usuario.Type).ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credenciais
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UsuarioDto MapearUsuario(Usuario u) => new()
    {
        Id = u.Id,
        DisplayName = u.DisplayName,
        Email = u.Email!,
        Type = u.Type,
        CreatedAt = u.CreatedAt
    };
}

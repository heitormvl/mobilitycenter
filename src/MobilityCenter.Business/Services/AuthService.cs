using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MobilityCenter.Business.Interfaces;
using MobilityCenter.Shared.DTOs.Usuario;
using MobilityCenter.Shared.Exceptions;
using MobilityCenter.Shared.Enums;
using MobilityCenter.Shared.Models;

namespace MobilityCenter.Business.Services;

public class AuthService : IAuthService
{
    private const string AdminEmail = "heitormvl12@gmail.com";

    private readonly UserManager<Usuario> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IFotoStorageService _fotoStorage;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IEmailService _emailService;

    public AuthService(
        UserManager<Usuario> userManager,
        IConfiguration configuration,
        IFotoStorageService fotoStorage,
        IHttpClientFactory httpFactory,
        IEmailService emailService)
    {
        _userManager = userManager;
        _configuration = configuration;
        _fotoStorage = fotoStorage;
        _httpFactory = httpFactory;
        _emailService = emailService;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var usuario = await _userManager.FindByEmailAsync(dto.Email)
            ?? throw new AppException("Credenciais inválidas.", 401);

        var senhaCorreta = await _userManager.CheckPasswordAsync(usuario, dto.Password);
        if (!senhaCorreta)
            throw new AppException("Credenciais inválidas.", 401);

        if (!await _userManager.IsEmailConfirmedAsync(usuario))
            throw new AppException("E-mail não confirmado. Verifique sua caixa de entrada.", 403);

        return new AuthResponseDto
        {
            Token = GerarToken(usuario),
            Usuario = MapearUsuario(usuario)
        };
    }

    public async Task<RegisterResponseDto> RegisterAsync(CriarUsuarioDto dto, string apiBaseUrl)
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

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(usuario);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var confirmUrl = $"{apiBaseUrl}/api/auth/confirmar-email?userId={usuario.Id}&token={encodedToken}";

        await _emailService.EnviarConfirmacaoAsync(usuario.Email!, usuario.DisplayName, confirmUrl);

        return new RegisterResponseDto
        {
            Message = "Cadastro realizado! Verifique seu e-mail para ativar a conta."
        };
    }

    public async Task<AuthResponseDto> ConfirmarEmailAsync(string userId, string token)
    {
        var usuario = await _userManager.FindByIdAsync(userId)
            ?? throw new AppException("Usuário não encontrado.", 404);

        if (await _userManager.IsEmailConfirmedAsync(usuario))
            return new AuthResponseDto
            {
                Token = GerarToken(usuario),
                Usuario = MapearUsuario(usuario)
            };

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        var resultado = await _userManager.ConfirmEmailAsync(usuario, decodedToken);

        if (!resultado.Succeeded)
            throw new AppException("Link de confirmação inválido ou expirado.", 400);

        return new AuthResponseDto
        {
            Token = GerarToken(usuario),
            Usuario = MapearUsuario(usuario)
        };
    }

    public async Task<AuthResponseDto> LoginWithGoogleAsync(string idToken)
    {
        var clientId = _configuration["Google:ClientId"]!;

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] });
        }
        catch (InvalidJwtException)
        {
            throw new AppException("Token Google inválido.", 401);
        }

        var usuario = await _userManager.FindByEmailAsync(payload.Email);

        if (usuario is null)
        {
            usuario = new Usuario
            {
                DisplayName = payload.Name,
                Email = payload.Email,
                UserName = payload.Email,
                EmailConfirmed = true,
                Type = payload.Email == AdminEmail ? TipoUsuario.Admin : TipoUsuario.Usuario,
                CreatedAt = DateTime.UtcNow
            };

            var resultado = await _userManager.CreateAsync(usuario);
            if (!resultado.Succeeded)
            {
                var erros = string.Join(", ", resultado.Errors.Select(e => e.Description));
                throw new ValidationException(erros);
            }
        }
        else if (payload.Email == AdminEmail && usuario.Type != TipoUsuario.Admin)
        {
            usuario.Type = TipoUsuario.Admin;
            await _userManager.UpdateAsync(usuario);
        }

        var novaFoto = await BaixarEArmazenarFotoGoogleAsync(usuario.Id, payload.Picture);
        if (!string.IsNullOrEmpty(novaFoto) && usuario.FotoPerfilUrl != novaFoto)
        {
            usuario.FotoPerfilUrl = novaFoto;
            await _userManager.UpdateAsync(usuario);
        }

        return new AuthResponseDto
        {
            Token = GerarToken(usuario),
            Usuario = MapearUsuario(usuario)
        };
    }

    private async Task<string?> BaixarEArmazenarFotoGoogleAsync(Guid usuarioId, string? googleUrl)
    {
        if (string.IsNullOrEmpty(googleUrl)) return null;
        try
        {
            var http = _httpFactory.CreateClient();
            using var response = await http.GetAsync(googleUrl);
            if (!response.IsSuccessStatusCode) return null;

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            await using var stream = await response.Content.ReadAsStreamAsync();
            return await _fotoStorage.UploadFotoPerfilAsync(usuarioId, stream, contentType);
        }
        catch
        {
            return null;
        }
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
        CreatedAt = u.CreatedAt,
        FotoPerfilUrl = u.FotoPerfilUrl
    };
}

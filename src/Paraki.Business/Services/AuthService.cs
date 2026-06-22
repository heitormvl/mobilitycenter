using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Paraki.Business.Interfaces;
using Paraki.Repositories.Context;
using Paraki.Shared.DTOs.Usuario;
using Paraki.Shared.Exceptions;
using Paraki.Shared.Enums;
using Paraki.Shared.Models;

namespace Paraki.Business.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<Usuario> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IFotoStorageService _fotoStorage;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;
    private readonly ParakiDbContext _db;

    public AuthService(
        UserManager<Usuario> userManager,
        IConfiguration configuration,
        IFotoStorageService fotoStorage,
        IHttpClientFactory httpFactory,
        IEmailService emailService,
        ILogger<AuthService> logger,
        ParakiDbContext db)
    {
        _userManager = userManager;
        _configuration = configuration;
        _fotoStorage = fotoStorage;
        _httpFactory = httpFactory;
        _emailService = emailService;
        _logger = logger;
        _db = db;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var usuario = await _userManager.FindByEmailAsync(dto.Email)
            ?? throw new AppException("Credenciais inválidas.", 401);

        var senhaCorreta = await _userManager.CheckPasswordAsync(usuario, dto.Password);
        if (!senhaCorreta)
            throw new AppException("Credenciais inválidas.", 401);

        if (!usuario.EmailConfirmed)
            throw new AppException("E-mail não confirmado. Verifique sua caixa de entrada.", 403);

        var refreshTokenValor = GerarRefreshTokenValor();
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            Token = refreshTokenValor,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
        });
        await _db.SaveChangesAsync();

        return new AuthResponseDto
        {
            Token = GerarToken(usuario),
            RefreshToken = refreshTokenValor,
            Usuario = MapearUsuario(usuario)
        };
    }

    public async Task<RegisterResponseDto> RegisterAsync(CriarUsuarioDto dto)
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

        await EnviarEmailConfirmacaoAsync(usuario);

        return new RegisterResponseDto
        {
            Email = dto.Email,
            Message = "Conta criada! Verifique seu e-mail para confirmar o cadastro."
        };
    }

    public async Task ConfirmEmailAsync(string userId, string token)
    {
        var usuario = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("Usuário não encontrado.");

        if (usuario.EmailConfirmed)
            return;

        var resultado = await _userManager.ConfirmEmailAsync(usuario, token);
        if (!resultado.Succeeded)
            throw new AppException("Link de confirmação inválido ou expirado.", 400);
    }

    public async Task ReenviarConfirmacaoAsync(string email)
    {
        var usuario = await _userManager.FindByEmailAsync(email)
            ?? throw new NotFoundException("Usuário não encontrado.");

        if (usuario.EmailConfirmed)
            return;

        await EnviarEmailConfirmacaoAsync(usuario);
    }

    public async Task EsquecerSenhaAsync(string email)
    {
        var usuario = await _userManager.FindByEmailAsync(email);
        if (usuario is null) return; // silencioso para evitar enumeração de usuários

        var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
        var tokenEncoded = Uri.EscapeDataString(token);
        var emailEncoded = Uri.EscapeDataString(email);
        var frontendUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:5200";
        var link = $"{frontendUrl}/redefinir-senha?email={emailEncoded}&token={tokenEncoded}";

        _logger.LogInformation("Link de redefinição de senha para {Email}: {Link}", email, link);

        try
        {
            var html = GerarHtmlRedefinicaoSenha(usuario.DisplayName, link);
            await _emailService.SendAsync(email, "Redefinir sua senha — Paraki", html);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enviar e-mail de redefinição para {Email}.", email);
        }
    }

    public async Task RedefinirSenhaAsync(string email, string token, string novaSenha)
    {
        var usuario = await _userManager.FindByEmailAsync(email)
            ?? throw new NotFoundException("Usuário não encontrado.");

        var resultado = await _userManager.ResetPasswordAsync(usuario, token, novaSenha);
        if (!resultado.Succeeded)
            throw new AppException("Link inválido ou expirado. Solicite um novo link.", 400);
    }

    private async Task EnviarEmailConfirmacaoAsync(Usuario usuario)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(usuario);
        var tokenEncoded = Uri.EscapeDataString(token);
        var frontendUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:5200";
        var link = $"{frontendUrl}/confirmar-email?userId={usuario.Id}&token={tokenEncoded}";

        _logger.LogInformation("Link de confirmação para {Email}: {Link}", usuario.Email, link);

        try
        {
            var html = GerarHtmlConfirmacao(usuario.DisplayName, link);
            await _emailService.SendAsync(usuario.Email!, "Confirme seu e-mail — Paraki", html);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enviar e-mail de confirmação para {Email}. O link está disponível nos logs acima.", usuario.Email);
        }
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
                Type = TipoUsuario.Usuario,
                CreatedAt = DateTime.UtcNow
            };

            var resultado = await _userManager.CreateAsync(usuario);
            if (!resultado.Succeeded)
            {
                var erros = string.Join(", ", resultado.Errors.Select(e => e.Description));
                throw new ValidationException(erros);
            }
        }

        // Baixa a foto do Google e armazena no nosso storage para evitar restrições de CORS/acesso
        var novaFoto = await BaixarEArmazenarFotoGoogleAsync(usuario.Id, payload.Picture);
        if (!string.IsNullOrEmpty(novaFoto) && usuario.FotoPerfilUrl != novaFoto)
        {
            usuario.FotoPerfilUrl = novaFoto;
            await _userManager.UpdateAsync(usuario);
        }

        var refreshTokenValor = GerarRefreshTokenValor();
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            Token = refreshTokenValor,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
        });
        await _db.SaveChangesAsync();

        return new AuthResponseDto
        {
            Token = GerarToken(usuario),
            RefreshToken = refreshTokenValor,
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

    public async Task<AuthResponseDto> RefreshAsync(string token)
    {
        var rt = await _db.RefreshTokens
            .Include(r => r.Usuario)
            .FirstOrDefaultAsync(r => r.Token == token && r.UsedAt == null && r.ExpiresAt > DateTime.UtcNow);

        if (rt is null)
        {
            // Detecta replay: token existente mas já usado = possível roubo de token
            var replayed = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token);
            if (replayed is not null)
            {
                var ativos = await _db.RefreshTokens
                    .Where(r => r.UsuarioId == replayed.UsuarioId && r.UsedAt == null)
                    .ToListAsync();
                foreach (var t in ativos)
                    t.UsedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            throw new AppException("Token de atualização inválido ou expirado.", 401);
        }

        var novoTokenValor = GerarRefreshTokenValor();

        rt.UsedAt = DateTime.UtcNow;
        rt.ReplacedByToken = novoTokenValor;

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = rt.UsuarioId,
            Token = novoTokenValor,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
        });

        await _db.SaveChangesAsync();

        return new AuthResponseDto
        {
            Token = GerarToken(rt.Usuario),
            RefreshToken = novoTokenValor,
            Usuario = MapearUsuario(rt.Usuario)
        };
    }

    private static string GerarRefreshTokenValor() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

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

    private static string GerarHtmlRedefinicaoSenha(string nome, string link) => $"""
        <!DOCTYPE html>
        <html lang="pt-BR">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width,initial-scale=1">
          <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap" rel="stylesheet">
        </head>
        <body style="margin:0;padding:0;background:#F2F2F7;font-family:'Inter',-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#F2F2F7;padding:40px 16px;">
            <tr><td align="center">
              <table width="100%" style="max-width:480px;background:#ffffff;border-radius:20px;overflow:hidden;box-shadow:0 4px 12px rgba(0,0,0,0.08),0 1px 4px rgba(0,0,0,0.05);">
                <tr>
                  <td style="background:linear-gradient(160deg,#7D1128 0%,#5E0D1E 100%);padding:36px 28px;text-align:center;">
                    <div style="width:60px;height:60px;background:rgba(255,255,255,0.15);border:1.5px solid rgba(255,255,255,0.25);border-radius:16px;display:inline-block;line-height:60px;margin-bottom:16px;font-size:28px;">
                      &#x1F512;
                    </div>
                    <p style="margin:0;font-size:22px;font-weight:800;color:#ffffff;letter-spacing:-0.03em;line-height:1;">Paraki</p>
                    <p style="margin:6px 0 0;font-size:13px;color:rgba(255,255,255,0.65);font-weight:400;">Micromobilidade colaborativa</p>
                  </td>
                </tr>
                <tr>
                  <td style="padding:36px 32px 28px;">
                    <p style="margin:0 0 6px;font-size:21px;font-weight:800;color:#111827;letter-spacing:-0.02em;">Redefinir senha</p>
                    <p style="margin:0 0 8px;font-size:15px;color:#6B7280;line-height:1.65;">Olá, {System.Net.WebUtility.HtmlEncode(nome)}!</p>
                    <p style="margin:0 0 28px;font-size:15px;color:#6B7280;line-height:1.65;">
                      Recebemos uma solicitação para redefinir a senha da sua conta. Clique no botão abaixo para criar uma nova senha.
                    </p>
                    <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:28px;">
                      <tr><td align="center">
                        <a href="{link}"
                           style="display:inline-block;background:#7D1128;color:#ffffff;font-size:15px;font-weight:700;text-decoration:none;padding:15px 36px;border-radius:12px;letter-spacing:0.01em;box-shadow:0 4px 12px rgba(125,17,40,0.35);">
                          Redefinir senha
                        </a>
                      </td></tr>
                    </table>
                    <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:20px;">
                      <tr><td style="height:1px;background:#E5E7EB;"></td></tr>
                    </table>
                    <p style="margin:0;font-size:12px;color:#9CA3AF;line-height:1.7;">
                      Se você não solicitou a redefinição de senha, ignore este e-mail com segurança.<br>
                      Este link expira em <strong style="color:#6B7280;">1 hora</strong>.
                    </p>
                  </td>
                </tr>
                <tr>
                  <td style="padding:16px 32px;background:#F8F8FA;border-top:1px solid #E5E7EB;">
                    <p style="margin:0;font-size:11px;color:#9CA3AF;text-align:center;line-height:1.6;">
                      Paraki &mdash; Maverick Software &bull; mavericksoftware.com.br
                    </p>
                  </td>
                </tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;

    private static string GerarHtmlConfirmacao(string nome, string link) => $"""
        <!DOCTYPE html>
        <html lang="pt-BR">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width,initial-scale=1">
          <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap" rel="stylesheet">
        </head>
        <body style="margin:0;padding:0;background:#F2F2F7;font-family:'Inter',-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#F2F2F7;padding:40px 16px;">
            <tr><td align="center">
              <table width="100%" style="max-width:480px;background:#ffffff;border-radius:20px;overflow:hidden;box-shadow:0 4px 12px rgba(0,0,0,0.08),0 1px 4px rgba(0,0,0,0.05);">

                <!-- Header -->
                <tr>
                  <td style="background:linear-gradient(160deg,#7D1128 0%,#5E0D1E 100%);padding:36px 28px;text-align:center;">
                    <div style="width:60px;height:60px;background:rgba(255,255,255,0.15);border:1.5px solid rgba(255,255,255,0.25);border-radius:16px;display:inline-block;line-height:60px;margin-bottom:16px;font-size:28px;">
                      &#x1F6B2;
                    </div>
                    <p style="margin:0;font-size:22px;font-weight:800;color:#ffffff;letter-spacing:-0.03em;line-height:1;">Paraki</p>
                    <p style="margin:6px 0 0;font-size:13px;color:rgba(255,255,255,0.65);font-weight:400;">Micromobilidade colaborativa</p>
                  </td>
                </tr>

                <!-- Body -->
                <tr>
                  <td style="padding:36px 32px 28px;">
                    <p style="margin:0 0 6px;font-size:21px;font-weight:800;color:#111827;letter-spacing:-0.02em;">Olá, {System.Net.WebUtility.HtmlEncode(nome)}!</p>
                    <p style="margin:0 0 28px;font-size:15px;color:#6B7280;line-height:1.65;">
                      Obrigado por criar sua conta. Para ativar o acesso, confirme seu e-mail clicando no botão abaixo.
                    </p>

                    <!-- CTA -->
                    <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:28px;">
                      <tr><td align="center">
                        <a href="{link}"
                           style="display:inline-block;background:#7D1128;color:#ffffff;font-size:15px;font-weight:700;text-decoration:none;padding:15px 36px;border-radius:12px;letter-spacing:0.01em;box-shadow:0 4px 12px rgba(125,17,40,0.35);">
                          Confirmar e-mail
                        </a>
                      </td></tr>
                    </table>

                    <!-- Divider -->
                    <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:20px;">
                      <tr><td style="height:1px;background:#E5E7EB;"></td></tr>
                    </table>

                    <p style="margin:0;font-size:12px;color:#9CA3AF;line-height:1.7;">
                      Se você não criou uma conta no Paraki, ignore este e-mail com segurança.<br>
                      Este link expira em <strong style="color:#6B7280;">24 horas</strong>.
                    </p>
                  </td>
                </tr>

                <!-- Footer -->
                <tr>
                  <td style="padding:16px 32px;background:#F8F8FA;border-top:1px solid #E5E7EB;">
                    <p style="margin:0;font-size:11px;color:#9CA3AF;text-align:center;line-height:1.6;">
                      Paraki &mdash; Maverick Software &bull; mavericksoftware.com.br
                    </p>
                  </td>
                </tr>

              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;
}

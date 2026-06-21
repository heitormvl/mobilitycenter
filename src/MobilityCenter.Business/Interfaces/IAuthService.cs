using MobilityCenter.Shared.DTOs.Usuario;

namespace MobilityCenter.Business.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<RegisterResponseDto> RegisterAsync(CriarUsuarioDto dto, string apiBaseUrl);
    Task<AuthResponseDto> LoginWithGoogleAsync(string idToken);
    Task<AuthResponseDto> ConfirmarEmailAsync(string userId, string token);
}

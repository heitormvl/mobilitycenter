using MobilityCenter.Shared.DTOs.Usuario;

namespace MobilityCenter.Business.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<RegisterResponseDto> RegisterAsync(CriarUsuarioDto dto);
    Task<AuthResponseDto> LoginWithGoogleAsync(string idToken);
    Task ConfirmEmailAsync(string userId, string token);
    Task ReenviarConfirmacaoAsync(string email);
}

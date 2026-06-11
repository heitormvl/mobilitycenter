using MobilityCenter.Shared.DTOs.Usuario;

namespace MobilityCenter.Business.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> RegisterAsync(CriarUsuarioDto dto);
    Task<AuthResponseDto> LoginWithGoogleAsync(string idToken);
}

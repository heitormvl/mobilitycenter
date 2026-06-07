using MobilityCenter.Shared.DTOs.Bicicletario;
using MobilityCenter.Shared.DTOs.Avaliacao;
using MobilityCenter.Shared.DTOs.Usuario;

namespace MobilityCenter.Business.Interfaces;

public interface IUsuarioService
{
    Task<UsuarioPerfilDto> ObterPerfilAsync(Guid usuarioId);
    Task<IEnumerable<AvaliacaoDto>> ObterAvaliacoesAsync(Guid usuarioId);
    Task<IEnumerable<BicicletarioResumoDto>> ObterBicicletariosAsync(Guid usuarioId);
    Task<string> AtualizarFotoPerfilAsync(Guid usuarioId, Stream imageStream, string contentType);
}

using Paraki.Shared.DTOs.Avaliacao;

namespace Paraki.Business.Interfaces;

public interface IAvaliacaoService
{
    Task<IEnumerable<AvaliacaoDto>> ListarPorBicicletarioAsync(Guid bicicletarioId);
    Task<AvaliacaoDto> CriarAsync(Guid bicicletarioId, CriarAvaliacaoDto dto, Guid usuarioId);
}

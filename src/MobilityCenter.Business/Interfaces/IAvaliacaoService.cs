using MobilityCenter.Shared.DTOs.Avaliacao;

namespace MobilityCenter.Business.Interfaces;

public interface IAvaliacaoService
{
    Task<IEnumerable<AvaliacaoDto>> ListarPorBicicletarioAsync(Guid bicicletarioId);
    Task<AvaliacaoDto> CriarAsync(Guid bicicletarioId, CriarAvaliacaoDto dto, Guid usuarioId);
}

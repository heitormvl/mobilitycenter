using MobilityCenter.Shared.DTOs.Bicicletario;
using MobilityCenter.Shared.DTOs.SugestaoEdicao;

namespace MobilityCenter.Business.Interfaces;

public interface ISugestaoEdicaoService
{
    Task<List<SugestaoEdicaoDto>> ListarPorBicicletarioAsync(Guid bicicletarioId, Guid revisorId);
    Task<BicicletarioDetalheDto> AprovarAsync(Guid sugestaoId, Guid revisorId);
    Task<SugestaoEdicaoDto> RejeitarAsync(Guid sugestaoId, Guid revisorId, string? motivo);
}

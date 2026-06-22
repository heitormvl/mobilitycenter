using Paraki.Shared.DTOs.Bicicletario;
using Paraki.Shared.DTOs.SugestaoEdicao;

namespace Paraki.Business.Interfaces;

public interface ISugestaoEdicaoService
{
    Task<List<SugestaoEdicaoDto>> ListarPorBicicletarioAsync(Guid bicicletarioId, Guid revisorId);
    Task<BicicletarioDetalheDto> AprovarAsync(Guid sugestaoId, Guid revisorId);
    Task<SugestaoEdicaoDto> RejeitarAsync(Guid sugestaoId, Guid revisorId, string? motivo);
}

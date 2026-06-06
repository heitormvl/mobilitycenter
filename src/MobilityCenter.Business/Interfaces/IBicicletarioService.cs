using MobilityCenter.Business.Filters;
using MobilityCenter.Shared.DTOs.Bicicletario;

namespace MobilityCenter.Business.Interfaces;

public interface IBicicletarioService
{
    Task<IEnumerable<BicicletarioResumoDto>> ListarAsync(BicicletarioFiltros filtros);
    Task<BicicletarioDetalheDto> ObterPorIdAsync(Guid id);
    Task<BicicletarioDetalheDto> CriarAsync(CriarBicicletarioDto dto, Guid usuarioId);
    Task<BicicletarioDetalheDto> AtualizarAsync(Guid id, AtualizarBicicletarioDto dto, Guid usuarioId);
    Task DeletarAsync(Guid id, Guid usuarioId);
}

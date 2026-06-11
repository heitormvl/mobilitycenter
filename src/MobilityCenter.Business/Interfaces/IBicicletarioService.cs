using MobilityCenter.Business.Filters;
using MobilityCenter.Shared.DTOs.Bicicletario;
using MobilityCenter.Shared.Enums;

namespace MobilityCenter.Business.Interfaces;

public interface IBicicletarioService
{
    Task<IEnumerable<BicicletarioResumoDto>> ListarAsync(BicicletarioFiltros filtros);
    Task<BicicletarioDetalheDto> ObterPorIdAsync(Guid id);
    Task<BicicletarioDetalheDto> CriarAsync(CriarBicicletarioDto dto, Guid usuarioId);
    Task<ResultadoAtualizacaoDto> AtualizarAsync(Guid id, AtualizarBicicletarioDto dto, Guid usuarioId);
    Task DeletarAsync(Guid id, Guid usuarioId, TipoUsuario tipoUsuario);
    Task DeletarPermanenteAsync(Guid id, Guid usuarioId, TipoUsuario tipoUsuario);
}

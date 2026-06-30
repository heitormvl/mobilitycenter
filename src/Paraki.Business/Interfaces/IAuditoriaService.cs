using Paraki.Shared.Enums;
using Paraki.Shared.Models;

namespace Paraki.Business.Interfaces;

public interface IAuditoriaService
{
    SnapshotBicicletario CriarSnapshot(Bicicletario b);

    Task RegistrarAsync(
        TipoAcaoAuditoria tipoAcao,
        Guid usuarioId,
        string nomeUsuario,
        Guid bicicletarioId,
        SnapshotBicicletario? antes,
        SnapshotBicicletario? depois,
        string? observacao = null,
        Guid? sugestaoId = null);
}

using Microsoft.AspNetCore.Http;
using Paraki.Shared.DTOs.Bicicletario;
using Paraki.Shared.DTOs.SugestaoEdicao;

namespace Paraki.Business.Interfaces;

public interface ISugestaoEdicaoService
{
    Task<List<SugestaoEdicaoDto>> ListarPorBicicletarioAsync(Guid bicicletarioId, Guid revisorId);
    Task<List<SugestaoEdicaoDto>> ListarPendentesAsync(Guid adminId);
    Task<int> ContarPendentesAsync(Guid adminId);
    Task<BicicletarioDetalheDto> AprovarAsync(Guid sugestaoId, Guid revisorId);
    Task<SugestaoEdicaoDto> RejeitarAsync(Guid sugestaoId, Guid revisorId, string? motivo);
    Task<SugestaoEdicaoDto> AdicionarFotoAsync(Guid sugestaoId, Guid autorId, IFormFile foto);
}

using MobilityCenter.Shared.DTOs.SugestaoEdicao;

namespace MobilityCenter.Shared.DTOs.Bicicletario;

public class ResultadoAtualizacaoDto
{
    public bool EditadoDireto { get; set; }
    public BicicletarioDetalheDto? Bicicletario { get; set; }
    public SugestaoEdicaoDto? Sugestao { get; set; }
}

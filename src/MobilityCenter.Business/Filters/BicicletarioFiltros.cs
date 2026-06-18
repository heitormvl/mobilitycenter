using MobilityCenter.Shared.Enums;

namespace MobilityCenter.Business.Filters;

public class BicicletarioFiltros
{
    public double? Lat { get; set; }
    public double? Lon { get; set; }
    public double? RaioKm { get; set; }
    public TipoVeiculo? TipoVeiculo { get; set; }
    public bool? AcessoLivre { get; set; }
    public bool? TemTomada { get; set; }
    public string? OrderBy { get; set; }
    public string? Q { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool IncluirOcultas { get; set; }
}

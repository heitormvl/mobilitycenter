namespace Paraki.Shared.DTOs.Admin;

public class UsuarioAdminDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public int Tipo { get; set; }
    public int TierEfetivo { get; set; }
    public int? TierOverride { get; set; }
    public int PontosAprovados { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class UsuariosPageDto
{
    public List<UsuarioAdminDto> Usuarios { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

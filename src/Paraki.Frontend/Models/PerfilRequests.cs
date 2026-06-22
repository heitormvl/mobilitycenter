namespace Paraki.Frontend.Models;

public class AtualizarPerfilRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class AlterarSenhaRequest
{
    public string SenhaAtual { get; set; } = string.Empty;
    public string NovaSenha { get; set; } = string.Empty;
}

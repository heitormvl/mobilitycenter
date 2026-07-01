namespace Paraki.RazorLib.Interfaces;

/// <summary>
/// Abstração do login com Google. No WASM usa o SDK GSI (JS) dentro do navegador;
/// no MAUI usa o fluxo OAuth nativo (Chrome Custom Tabs / SFSafariViewController).
/// </summary>
public interface IGoogleAuthService
{
    /// <summary>
    /// Inicia o fluxo de login com Google e retorna o <c>id_token</c> (JWT) do Google,
    /// pronto para ser enviado a <c>api/auth/google</c>. Retorna <c>null</c> se o
    /// usuário cancelar ou o fluxo falhar.
    /// </summary>
    Task<string?> SignInAsync();
}

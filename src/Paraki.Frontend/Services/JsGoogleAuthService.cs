using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using Paraki.RazorLib.Interfaces;

namespace Paraki.Frontend.Services;

/// <summary>
/// Implementação WASM de <see cref="IGoogleAuthService"/>. Encapsula o fluxo do
/// SDK GSI do Google (<c>wwwroot/js/googleAuth.js</c>), que devolve o credential
/// (id_token) por callback — aqui convertido em <see cref="Task"/> via
/// <see cref="TaskCompletionSource{TResult}"/>.
/// </summary>
public class JsGoogleAuthService(IJSRuntime js, IConfiguration config) : IGoogleAuthService, IDisposable
{
    private DotNetObjectReference<JsGoogleAuthService>? _ref;
    private TaskCompletionSource<string?>? _tcs;

    public async Task<string?> SignInAsync()
    {
        _tcs = new TaskCompletionSource<string?>();
        _ref ??= DotNetObjectReference.Create(this);

        var clientId = config["Google:ClientId"] ?? "";
        await js.InvokeVoidAsync("googleAuth.signIn", clientId, _ref);

        return await _tcs.Task;
    }

    [JSInvokable]
    public void HandleGoogleCredential(string idToken) => _tcs?.TrySetResult(idToken);

    [JSInvokable]
    public void HandleGoogleError(string reason) => _tcs?.TrySetResult(null);

    public void Dispose() => _ref?.Dispose();
}

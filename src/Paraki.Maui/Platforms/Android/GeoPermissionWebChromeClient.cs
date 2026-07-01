#if ANDROID
using Android.Net;
using Android.Webkit;

namespace Paraki.Maui;

/// <summary>
/// Wraps the BlazorWebView's default WebChromeClient para conceder permissão
/// de geolocalização ao WebView quando o JavaScript chama navigator.geolocation.
/// Sem esse override o Android bloqueia silenciosamente todas as chamadas geo.
/// </summary>
internal class GeoPermissionWebChromeClient : WebChromeClient
{
    private readonly WebChromeClient _inner;

    public GeoPermissionWebChromeClient(WebChromeClient inner) => _inner = inner;

    // Concede geolocalização ao origin do WebView (a permissão de OS já foi pedida via Permissions API)
    public override void OnGeolocationPermissionsShowPrompt(string? origin, GeolocationPermissions.ICallback? callback)
        => callback?.Invoke(origin, true, false);

    // Encaminha o restante para o cliente original do MAUI (mantém file picker, JS dialogs, etc.)
    public override bool OnShowFileChooser(Android.Webkit.WebView? wv, IValueCallback? cb, FileChooserParams? p)
        => _inner.OnShowFileChooser(wv, cb, p);

    public override bool OnJsAlert(Android.Webkit.WebView? v, string? url, string? msg, JsResult? r)
        => _inner.OnJsAlert(v, url, msg, r);

    public override bool OnJsConfirm(Android.Webkit.WebView? v, string? url, string? msg, JsResult? r)
        => _inner.OnJsConfirm(v, url, msg, r);

    public override bool OnJsPrompt(Android.Webkit.WebView? v, string? url, string? msg, string? def, JsPromptResult? r)
        => _inner.OnJsPrompt(v, url, msg, def, r);

    public override bool OnConsoleMessage(ConsoleMessage? msg) => _inner.OnConsoleMessage(msg);

    public override void OnPermissionRequest(PermissionRequest? req) => _inner.OnPermissionRequest(req);
}
#endif

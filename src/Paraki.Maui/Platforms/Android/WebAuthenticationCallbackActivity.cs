using Android.App;
using Android.Content;
using Android.Content.PM;

namespace Paraki.Maui;

/// <summary>
/// Recebe o redirect do fluxo OAuth do Google (<c>br.com.paraki:/oauthredirect</c>)
/// e devolve o controle ao <see cref="WebAuthenticator"/>. O scheme deve bater com
/// <c>Google:RedirectScheme</c> no appsettings.json.
/// </summary>
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "br.com.paraki")]
public class WebAuthenticationCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
}

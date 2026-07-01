#if IOS
using Foundation;
using Google.SignIn;

namespace Paraki.Maui.Services;

/// <summary>
/// Ponte entre <see cref="MauiGoogleAuthService"/> e o SDK GoogleSignIn do iOS.
/// Requer, no Info.plist, o URL scheme reverso do iOS client id
/// (ex.: <c>com.googleusercontent.apps.SEU-IOS-CLIENT-ID</c>) e o encaminhamento
/// de <c>OpenUrl</c> a partir do <see cref="AppDelegate"/>.
///
/// NOTA: iOS não pode ser compilado/testado neste ambiente (requer Mac). A
/// superfície da API do binding deve ser verificada ao buildar no macOS.
/// </summary>
internal static class GoogleSignInHelper
{
    public static Task<string?> SignInAsync(string iosClientId, string webClientId)
    {
        var tcs = new TaskCompletionSource<string?>();

        var vc = Microsoft.Maui.ApplicationModel.Platform.GetCurrentUIViewController();
        if (vc is null)
        {
            tcs.SetResult(null);
            return tcs.Task;
        }

        // clientID = iOS client; serverClientID = Web client (audience do id_token p/ backend)
        SignIn.SharedInstance.Configuration = new Configuration(iosClientId, webClientId);
        SignIn.SharedInstance.SignIn(vc, (result, error) =>
        {
            var idToken = result?.User?.IdToken?.TokenString;
            tcs.TrySetResult(error is null ? idToken : null);
        });

        return tcs.Task;
    }

    public static bool HandleUrl(NSUrl url) => SignIn.SharedInstance.HandleUrl(url);
}
#endif

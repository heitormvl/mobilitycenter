#if ANDROID
using Android.App;
using Android.Content;
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Common.Apis;

namespace Paraki.Maui.Services;

// A API GoogleSignIn (Play Services Auth) foi marcada como obsoleta pelo Google em
// favor do Credential Manager, mas continua funcional e é a via nativa suportada
// por este binding. Uso deliberado — suprimimos o aviso de obsolescência.
#pragma warning disable CS0618

/// <summary>
/// Ponte entre <see cref="MauiGoogleAuthService"/> e o SDK do Google Play Services
/// (Android.Gms.Auth.Api.SignIn). Dispara o intent de login e recebe o resultado
/// via <see cref="MainActivity.OnActivityResult"/>.
/// </summary>
internal static class GoogleSignInHelper
{
    public const int RcSignIn = 9001;
    private static TaskCompletionSource<string?>? _tcs;

    public static Task<string?> SignInAsync(string webClientId)
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity is null)
            return Task.FromResult<string?>(null);

        _tcs = new TaskCompletionSource<string?>();

        var options = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
            .RequestIdToken(webClientId)
            .RequestEmail()
            .Build();

        var client = GoogleSignIn.GetClient(activity, options);
        // Força o seletor de conta a cada login (evita reusar sessão anterior silenciosamente)
        client.SignOut();
        activity.StartActivityForResult(client.SignInIntent, RcSignIn);

        return _tcs.Task;
    }

    public static void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        if (requestCode != RcSignIn || _tcs is null)
            return;

        try
        {
            var completed = GoogleSignIn.GetSignedInAccountFromIntent(data);
            var account = (GoogleSignInAccount)completed.GetResult(
                Java.Lang.Class.FromType(typeof(ApiException)))!;
            _tcs.TrySetResult(account.IdToken);
        }
        catch
        {
            // ApiException (cancelado / falha) — trata como login não concluído
            _tcs.TrySetResult(null);
        }
        finally
        {
            _tcs = null;
        }
    }
}
#pragma warning restore CS0618
#endif

using Microsoft.Extensions.Configuration;
using Paraki.RazorLib.Interfaces;

namespace Paraki.Maui.Services;

/// <summary>
/// Login com Google no MAUI usando o SDK nativo do Google Sign-In
/// (Google Play Services no Android, GoogleSignIn no iOS). Retorna o
/// <c>id_token</c> do Google, que o backend valida em <c>api/auth/google</c>.
///
/// Configuração necessária (Google Cloud Console):
///  - <c>Google:ClientId</c>: o OAuth client do tipo <b>Web</b> (o mesmo que o
///    backend valida). É passado como audience do id_token — RequestIdToken no
///    Android e serverClientID no iOS.
///  - Android: criar um OAuth client do tipo <b>Android</b> (package
///    <c>br.com.paraki</c> + SHA-1 da keystore de assinatura). Não precisa de
///    google-services.json com este fluxo.
///  - iOS: criar um OAuth client do tipo <b>iOS</b>, preencher
///    <c>Google:iOSClientId</c> e registrar o URL scheme reverso no Info.plist.
/// </summary>
public class MauiGoogleAuthService(IConfiguration config) : IGoogleAuthService
{
    public Task<string?> SignInAsync()
    {
        // O id_token precisa ter como audience o Web client id (validado pelo backend).
        var webClientId = config["Google:ClientId"];
        if (string.IsNullOrEmpty(webClientId))
            return Task.FromResult<string?>(null);

#if ANDROID
        return GoogleSignInHelper.SignInAsync(webClientId);
#elif IOS
        var iosClientId = config["Google:iOSClientId"];
        if (string.IsNullOrEmpty(iosClientId))
            return Task.FromResult<string?>(null);
        return GoogleSignInHelper.SignInAsync(iosClientId, webClientId);
#else
        return Task.FromResult<string?>(null);
#endif
    }
}

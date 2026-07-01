using Foundation;
using Paraki.Maui.Services;
using UIKit;

namespace Paraki.Maui;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	// Encaminha o callback OAuth do GoogleSignIn de volta ao SDK
	public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
		=> GoogleSignInHelper.HandleUrl(url);
}

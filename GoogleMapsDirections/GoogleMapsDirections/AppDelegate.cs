using Google.Maps;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace GoogleMapsDirections
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        UIWindow _window;
        MyViewController _viewController;

        const string ApiKey = "INSERT API KEY HERE";

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            _window = new UIWindow(UIScreen.MainScreen.Bounds);

            _viewController = new MyViewController();
            _window.RootViewController = _viewController;

            MapServices.ProvideAPIKey(ApiKey);

            _window.MakeKeyAndVisible();

            return true;
        }
    }
}


using Android.App;
using Android.Content;
using Android.Content.PM;
using AwsSaaC03Practice.Services;

namespace AwsSaaC03Practice;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation |
                           ConfigChanges.UiMode | ConfigChanges.ScreenLayout |
                           ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
    LaunchMode = LaunchMode.SingleTask)]
// Catch the myapp://callback URI from the system browser after OAuth
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "myapp",
    DataHost = "callback")]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        if (intent?.Data is not null && intent.Data.Scheme == "myapp")
        {
            AuthService.HandleAndroidCallback(new Uri(intent.DataString!));
        }
    }
}

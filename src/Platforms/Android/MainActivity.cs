using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Firebase;
using Firebase.Crashlytics;
using AwsSaaC03Practice.Services;

namespace AwsSaaC03Practice;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation |
                           ConfigChanges.UiMode | ConfigChanges.ScreenLayout |
                           ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
    LaunchMode = LaunchMode.SingleTask)]
// Catch the selimcelemsaaapp://callback URI from Custom Tabs after OAuth
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "selimcelemsaaapp",
    DataHost = "callback")]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        FirebaseApp.InitializeApp(this);
        FirebaseCrashlytics.Instance.SetCrashlyticsCollectionEnabled(true);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        if (intent?.Data is not null && intent.Data.Scheme == "selimcelemsaaapp")
        {
            AuthService.HandleAndroidCallback(new Uri(intent.DataString!));
        }
    }
}

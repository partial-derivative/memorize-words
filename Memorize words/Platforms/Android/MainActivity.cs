using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.Core.View;

namespace Memorize_words;
[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density
)]
[Preserve(AllMembers = true)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var window = Window;

        // 关键：允许内容延伸到系统栏
        window.SetDecorFitsSystemWindows(false);

        // ⭐ 关键补丁：消费 WindowInsets（核心）
        ViewCompat.SetOnApplyWindowInsetsListener(window.DecorView, new InsetsListener());
    }
}

public class InsetsListener : Java.Lang.Object, AndroidX.Core.View.IOnApplyWindowInsetsListener
{
    public WindowInsetsCompat OnApplyWindowInsets(Android.Views.View v, WindowInsetsCompat insets)
    {
        // 直接消费 system bars inset
        return insets.ConsumeSystemWindowInsets();
    }
}
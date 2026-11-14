using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Window;

namespace Vrouves;

[Activity(Label = "@string/app_name", MainLauncher = true, Exported = true, Theme = "@style/VrouvesTheme",
    Icon = "@mipmap/icon", RoundIcon = "@mipmap/icon",
    ScreenOrientation = ScreenOrientation.SensorLandscape, LaunchMode = LaunchMode.SingleTask,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout |
        ConfigChanges.SmallestScreenSize | ConfigChanges.KeyboardHidden | ConfigChanges.Keyboard |
        ConfigChanges.Navigation | ConfigChanges.UiMode | ConfigChanges.Density)]
public class MainActivity : Activity
{
    App app = null!;
    GameView view = null!;
    BackCallback? back;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        using (var s = Assets!.Open("data/stages.json"))
            Data.LoadStages(s);
        Store.Load(this);
        Sfx.Init(this);
        var gfx = new Gfx(Assets!);
        gfx.PreloadSprites();
        app = new App(this, gfx);
        // for testing, as the PC version's --stage N and --ending SCORE:
        //   adb shell am start -n <activity> --ei stage 8      (or --ei ending 7000)
        var it = Intent;
        if (it != null && it.HasExtra("stage"))
            app.StartAt(Math.Clamp(it.GetIntExtra("stage", 1), 1, Data.Stages.Count));
        else if (it != null && it.HasExtra("ending"))
            app.ShowEnding(it.GetIntExtra("ending", 0));
        else
            app.Start();
        view = new GameView(this, app);
        SetContentView(view);
        view.RequestFocus();
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            back = new BackCallback(OnBack);
            OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(0, back);
        }
        HideSystemBars();
    }

    void OnBack()
    {
        if (!app.Screen.Back())
            Finish();
    }

#pragma warning disable CS0672, CA1422
    /// <summary>Back before Android 13</summary>
    public override void OnBackPressed() => OnBack();
#pragma warning restore CS0672, CA1422

    protected override void OnResume()
    {
        base.OnResume();
        HideSystemBars();
        Sfx.OnAppResume();
        view.Resume();
    }

    protected override void OnPause()
    {
        app.Screen.AppPaused();
        view.Pause();
        Sfx.OnAppPause();
        base.OnPause();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus)
            HideSystemBars();
    }

    protected override void OnDestroy()
    {
        if (back != null && OperatingSystem.IsAndroidVersionAtLeast(33))
            OnBackInvokedDispatcher.UnregisterOnBackInvokedCallback(back);
        base.OnDestroy();
    }

    /// <summary>the whole screen for the game; a swipe from the edge shows the bars for a moment</summary>
    void HideSystemBars()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
#pragma warning disable CA1422
            if (!OperatingSystem.IsAndroidVersionAtLeast(35))   // from 35 on, always edge to edge
                Window!.SetDecorFitsSystemWindows(false);
#pragma warning restore CA1422
            var c = Window!.InsetsController;
            if (c != null)
            {
                c.Hide(WindowInsets.Type.SystemBars());
                c.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
            }
        }
        else
        {
#pragma warning disable CS0618, CA1422
            Window!.DecorView.SystemUiFlags = SystemUiFlags.ImmersiveSticky | SystemUiFlags.Fullscreen |
                SystemUiFlags.HideNavigation | SystemUiFlags.LayoutFullscreen | SystemUiFlags.LayoutHideNavigation |
                SystemUiFlags.LayoutStable;
#pragma warning restore CS0618, CA1422
        }
        if (OperatingSystem.IsAndroidVersionAtLeast(28))
        {
            // draw beside the camera cutout too; GameView keeps the game itself clear of it
            var a = Window!.Attributes!;
            if (a.LayoutInDisplayCutoutMode != LayoutInDisplayCutoutMode.ShortEdges)
            {
                a.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
                Window.Attributes = a;
            }
        }
    }

    sealed class BackCallback(Action action) : Java.Lang.Object, IOnBackInvokedCallback
    {
        public void OnBackInvoked() => action();
    }
}

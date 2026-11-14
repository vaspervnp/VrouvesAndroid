using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;

namespace Vrouves;

/// <summary>The one view of the app: every frame it moves the current screen on and draws it,
/// scaled to fill the display (inside any camera cutout), and hands it the touches and keys
/// in its own coordinates.</summary>
public sealed class GameView : View
{
    readonly App app;
    readonly System.Diagnostics.Stopwatch clock = new();
    double last;
    bool running;
    float scale = 1f, offX, offY;
    float safeL, safeT, safeR, safeB;
    int pointer = -1;
    double perfT, perfDraw;
    int perfFrames;

    public GameView(Context ctx, App app) : base(ctx)
    {
        this.app = app;
        Focusable = true;
        FocusableInTouchMode = true;
        KeepScreenOn = true;
    }

    public void Resume()
    {
        running = true;
        clock.Restart();
        last = 0;
        PostInvalidateOnAnimation();
    }

    public void Pause() => running = false;

    public override WindowInsets OnApplyWindowInsets(WindowInsets insets)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(28) && insets.DisplayCutout is { } cut)
        {
            safeL = cut.SafeInsetLeft;
            safeT = cut.SafeInsetTop;
            safeR = cut.SafeInsetRight;
            safeB = cut.SafeInsetBottom;
        }
        else
            safeL = safeT = safeR = safeB = 0;
        return base.OnApplyWindowInsets(insets);
    }

    void Layout(Screen s)
    {
        float aw = Math.Max(1, Width - safeL - safeR), ah = Math.Max(1, Height - safeT - safeB);
        float aspect = aw / ah;
        float h = s.DesignH, w = h * aspect;
        if (w < s.MinW)
        {
            w = s.MinW;
            h = w / aspect;
        }
        s.W = w;
        s.H = h;
        scale = aw / w;
        offX = safeL;
        offY = safeT;
        // the strips beside a camera cutout, in the screen's units: its background goes there too
        s.ExtL = safeL / scale;
        s.ExtT = safeT / scale;
        s.ExtR = (Width - safeL - aw) / scale;
        s.ExtB = (Height - safeT - ah) / scale;
    }

    protected override void OnDraw(Canvas canvas)
    {
        double now = clock.Elapsed.TotalSeconds;
        float dt = (float)Math.Clamp(now - last, 0, 0.1);
        last = now;
        var s = app.Screen;
        Layout(s);
        if (running)
            s.Update(dt);
        canvas.DrawColor(Color.Black);
        canvas.Save();
        canvas.Translate(offX, offY);
        canvas.Scale(scale, scale);
        canvas.ClipRect(-s.ExtL, -s.ExtT, s.W + s.ExtR, s.H + s.ExtB);
        app.Gfx.C = canvas;
#if DEBUG
        // adb logcat -s VrouvesPerf: frames a second and the time the drawing calls take
        double t0 = clock.Elapsed.TotalSeconds;
#endif
        s.Draw(app.Gfx);
        canvas.Restore();
#if DEBUG
        perfDraw += clock.Elapsed.TotalSeconds - t0;
        perfFrames++;
        if (now - perfT > 3)
        {
            Android.Util.Log.Info("VrouvesPerf", $"{perfFrames / (now - perfT):F1} fps, draw {perfDraw / perfFrames * 1000:F2} ms");
            perfT = now;
            perfFrames = 0;
            perfDraw = 0;
        }
#endif
        if (running)
            PostInvalidateOnAnimation();
    }

    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (e == null)
            return false;
        var s = app.Screen;
        // one finger at a time: the first one down
        switch (e.ActionMasked)
        {
            case MotionEventActions.Down:
                pointer = e.GetPointerId(0);
                s.Down((e.GetX(0) - offX) / scale, (e.GetY(0) - offY) / scale);
                break;
            case MotionEventActions.Move:
            {
                int i = e.FindPointerIndex(pointer);
                if (i >= 0)
                    s.Move((e.GetX(i) - offX) / scale, (e.GetY(i) - offY) / scale);
                break;
            }
            case MotionEventActions.PointerUp:
            case MotionEventActions.Up:
            {
                int i = e.ActionIndex;
                if (e.GetPointerId(i) == pointer)
                {
                    pointer = -1;
                    s.Up((e.GetX(i) - offX) / scale, (e.GetY(i) - offY) / scale);
                }
                break;
            }
            case MotionEventActions.Cancel:
                pointer = -1;
                s.Cancel();
                break;
        }
        return true;
    }

    public override bool OnKeyDown(Keycode keyCode, KeyEvent? e)
    {
        if (keyCode == Keycode.Back || e == null)
            return base.OnKeyDown(keyCode, e);
        if (keyCode == Keycode.Escape)
        {
            if (!app.Screen.Back())
                app.Activity.Finish();
            return true;
        }
        return app.Screen.Key(keyCode, e) || base.OnKeyDown(keyCode, e);
    }
}

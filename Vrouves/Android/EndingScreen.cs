using Android.Views;

namespace Vrouves;

/// <summary>The ending: the Bride's wedding feast in front of the church (rendered in Blender
/// for the CPC version, VrouvesCPC/tools/wedding_blender.py), with confetti.</summary>
public sealed class EndingScreen : Screen
{
    readonly App app;
    readonly int score;
    float time;

    public EndingScreen(App app, int score)
    {
        this.app = app;
        this.score = score;
        Sfx.Play("wave");
    }

    public override void Update(float dt) => time += dt;

    void Done()
    {
        if (time >= 2f)
            app.GameEnded(score);
    }

    public override void Down(float x, float y) => Done();

    public override bool Back()
    {
        Done();
        return true;
    }

    public override bool Key(Keycode k, KeyEvent e)
    {
        if (k is Keycode.Space or Keycode.Enter or Keycode.NumpadEnter or Keycode.DpadCenter or Keycode.ButtonA)
        {
            Done();
            return true;
        }
        return false;
    }

    public override void Draw(Gfx g)
    {
        g.Rect(-ExtL, -ExtT, W + ExtL + ExtR, H + ExtT + ExtB, 0xFF59A6FF);
        // the picture as big as leaves room for the title above and three lines and the prompt below
        var pic = app.EndingBitmap();
        float sc = Math.Min(W / pic.Width, (H - 330) / pic.Height);
        float pw = pic.Width * sc, ph = pic.Height * sc;
        g.Image(pic, (W - pw) / 2, 112, pw, ph);
        string title = Data.StageText(Data.Stages.Count, 1);
        g.Text(0, 90, title, 58, Col.GOLD, Align.Center, W, g.Title, 14, Col.BLACK);
        float y = 112 + ph + 44;
        foreach (var line in new[] { Data.T("ending"), Data.T("ending2"), Data.T("final_score", score) })
        {
            float sz = 30;
            while (sz > 18 && g.Width(line, sz) > W - 40)
                sz--;
            g.Text(0, y, line, sz, Col.WHITE, Align.Center, W, null, 8, Col.BLACK);
            y += 42;
        }
        if (time > 2f)
            g.Text(0, H - 22, Data.T("press"), 26, Col.Alpha(Col.WHITE, 0.6f + 0.4f * MathF.Sin(time * 5f)), Align.Center, W, null, 5);
        // confetti
        var rng = new Random(7);
        for (int i = 0; i < 80; i++)
        {
            float x = Mod((float)rng.NextDouble() * W + MathF.Sin(time + i) * 30f, W);
            float yy = Mod((float)rng.NextDouble() * H + time * (60f + (float)rng.NextDouble() * 80f), H);
            var c = Android.Graphics.Color.HSVToColor([(float)rng.NextDouble() * 360f, 0.8f, 1f]);
            g.Rect(x, yy, 8, 12, (uint)c.ToArgb());
        }
    }

    static float Mod(float a, float m) => ((a % m) + m) % m;
}

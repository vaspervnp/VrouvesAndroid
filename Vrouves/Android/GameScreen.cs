using Android.Graphics;
using Android.Views;

namespace Vrouves;

/// <summary>A stage on screen: the map with the defenders, the invaders and the shots, a bar
/// on top (stage, wave, countdown, score, lives, vrouves, sacks) and a panel on the right
/// with the defender cards and the Next wave, Speed and Pause buttons. Touch a tile and hold
/// to see the range, lift to build; touch a defender, then again, to sell it.</summary>
public sealed class GameScreen : Screen
{
    const float CELL = Stage.CELL;
    const float K = Stage.K;
    const float PF_W = Stage.PF_W;
    const float PF_H = Stage.PF_H;
    const float TOP = 64f;
    const float SPR = 0.5f;                     // sprite pictures are 128 px per cell
    const float PANEL_MIN = 340f, PANEL_MAX = 800f;

    public override float DesignH => TOP + PF_H;
    public override float MinW => PF_W + PANEL_MIN;

    readonly App app;
    readonly Bitmap bg;
    readonly Stage st = new();

    /// <summary>let the stage play by itself, with the CPC's build plan (for testing and the
    /// store screenshots)</summary>
    public void PlayBot(int[][] plan)
    {
        st.Bot = true;
        st.Plan.AddRange(plan);
    }

    // layout, from W and H every frame
    float ox, oy, pw;
    float FX => ox;                             // the playfield's top left corner
    float FY => oy + TOP;
    float PX => ox + PF_W;                      // the panel's left edge

    Point cursor = new(7, 3);
    bool cursorOn;
    bool holding;                               // a finger on the map: lift it to build
    Point sellCell = new(-1, -1);               // a defender touched once: a second touch sells it
    float sellT;
    bool paused, quitAsk;
    int speed = 1;
    float endWait;
    bool hasLeft;
    float tickAcc, alpha, time;
    string pressed = "";
    readonly Dictionary<string, (float x, float y, float w, float h)> buttons = [];
    readonly List<(float y, int kind, object o)> items = [];

    public GameScreen(App app, Bitmap bg)
    {
        this.app = app;
        this.bg = bg;
    }

    public void Setup(int n, int startScore, int lives)
    {
        st.Setup(n, startScore, lives);
        Sfx.Music(true);
    }

    void Layout()
    {
        pw = Math.Min(W - PF_W, PANEL_MAX);
        ox = (W - PF_W - pw) / 2;
        oy = (H - DesignH) / 2;
    }

    // ====================================================================== main loop

    public override void Update(float dt)
    {
        time += dt;
        st.Shake = Math.Max(0, st.Shake - dt * 3f);
        if (st.GameOver != 0)
            endWait += dt;
        else if (!paused)
        {
            tickAcc += dt * speed;
            int n = 0;
            while (tickAcc >= Data.TICK && n < 10)
            {
                tickAcc -= Data.TICK;
                st.GameTick();
                n++;
                if (st.GameOver != 0)
                {
                    endWait = 0;
                    break;
                }
            }
        }
        alpha = !paused && st.GameOver == 0 ? Math.Clamp(tickAcc / Data.TICK, 0, 1) : 1f;
        foreach (var e in st.Enemies)
            e.Flash = Math.Max(0, e.Flash - dt * 6f);
        foreach (var tw in st.Towers)
            if (tw != null)
                tw.Kick = Math.Max(0, tw.Kick - dt * 5f);
    }

    void LeaveStage()
    {
        if (hasLeft)
            return;
        hasLeft = true;
        app.StageFinished(st.GameOver == 1 ? "won" : st.GameOver == 2 ? "lost" : "quit", st.Score);
    }

    public override void AppPaused()
    {
        if (st.GameOver == 0)
            paused = true;
        holding = false;
    }

    public override void Leave() => Sfx.Music(false);

    // ====================================================================== input

    bool InField(float x, float y) => x >= FX && x < FX + PF_W && y >= FY && y < FY + PF_H;

    Point CellAt(float x, float y) =>
        new(Math.Clamp((int)((x - FX) / CELL), 0, Data.COLS - 1), Math.Clamp((int)((y - FY) / CELL), 0, Data.ROWS - 1));

    public override void Down(float x, float y)
    {
        if (st.GameOver != 0)
        {
            if (endWait > 1.2f)
                LeaveStage();
            return;
        }
        foreach (var (name, r) in buttons)
            if (In(x, y, r) && (!paused || name is "continue" or "quit" or "pause" or "speed"))
            {
                pressed = name;
                PressButton(name);
                return;
            }
        if (paused)
            return;
        if (InField(x, y))
        {
            holding = true;
            cursorOn = true;
            cursor = CellAt(x, y);
        }
    }

    public override void Move(float x, float y)
    {
        if (!holding)
            return;
        if (InField(x, y))
        {
            var c = CellAt(x, y);
            if (c.X != cursor.X || c.Y != cursor.Y)
                cursor = c;
            cursorOn = true;
        }
        else
            cursorOn = false;               // slid off the map: nothing is built
    }

    public override void Up(float x, float y)
    {
        pressed = "";
        if (!holding)
            return;
        holding = false;
        if (paused || st.GameOver != 0 || !InField(x, y))
            return;
        var p = CellAt(x, y);
        cursor = p;
        cursorOn = true;
        Act(p.X, p.Y);
    }

    public override void Cancel()
    {
        holding = false;
        pressed = "";
    }

    /// <summary>a touch lifted on a tile: build there, or sell the defender there on the
    /// second touch (one touch says what it sells for)</summary>
    void Act(int c, int r)
    {
        var tw = st.TowerAt[r, c];
        if (tw != null)
        {
            if (sellCell.X == c && sellCell.Y == r && time - sellT < 3f)
            {
                sellCell = new Point(-1, -1);
                st.TrySell(c, r);
            }
            else
            {
                sellCell = new Point(c, r);
                sellT = time;
                st.Msg = Data.T("sell", Data.TTYPES[tw.Type].Sell);
                st.MsgTimer = 75;
                Sfx.Play("move");
            }
        }
        else
        {
            sellCell = new Point(-1, -1);
            st.TryBuild(c, r);
        }
    }

    public override bool Back()
    {
        if (st.GameOver != 0)
        {
            if (endWait > 1.2f)
                LeaveStage();
            return true;
        }
        if (quitAsk)
            LeaveStage();
        else
        {
            paused = true;
            quitAsk = true;
            Sfx.Play("move");
        }
        return true;
    }

    public override bool Key(Keycode k, KeyEvent e)
    {
        if (st.GameOver != 0)
        {
            if (endWait > 1.2f && k is Keycode.Space or Keycode.Enter or Keycode.NumpadEnter or Keycode.DpadCenter or Keycode.ButtonA)
                LeaveStage();
            return true;
        }
        switch (k)
        {
            case Keycode.L:
                Data.ToggleLang();
                return true;
            case Keycode.M:
                Sfx.ToggleMusic();
                return true;
            case Keycode.H:
            case Keycode.ButtonStart:
                paused = !paused;
                quitAsk = false;
                return true;
        }
        if (paused)
            return false;
        switch (k)
        {
            case Keycode.DpadLeft or Keycode.O:
                MoveCursor(-1, 0);
                break;
            case Keycode.DpadRight or Keycode.P:
                MoveCursor(1, 0);
                break;
            case Keycode.DpadUp or Keycode.Q:
                MoveCursor(0, -1);
                break;
            case Keycode.DpadDown or Keycode.A:
                MoveCursor(0, 1);
                break;
            case Keycode.Space or Keycode.Enter or Keycode.NumpadEnter or Keycode.DpadCenter or Keycode.ButtonA:
                st.TryBuild(cursor.X, cursor.Y);
                break;
            case Keycode.S or Keycode.Del or Keycode.ForwardDel or Keycode.ButtonX:
                st.TrySell(cursor.X, cursor.Y);
                break;
            case Keycode.N or Keycode.ButtonY:
                st.CallWaveNow();
                break;
            case Keycode.F:
                speed = 1 + speed % 3;
                break;
            case Keycode.ButtonL1:
                Cycle(-1);
                break;
            case Keycode.ButtonR1:
                Cycle(1);
                break;
            case >= Keycode.Num1 and <= Keycode.Num7:
                Select(Data.TOWER_ORDER[k - Keycode.Num1]);
                break;
            default:
                return false;
        }
        return true;
    }

    void MoveCursor(int dc, int dr)
    {
        cursor = new Point(Math.Clamp(cursor.X + dc, 0, Data.COLS - 1), Math.Clamp(cursor.Y + dr, 0, Data.ROWS - 1));
        cursorOn = true;
        Sfx.Play("move");
    }

    void Cycle(int d)
    {
        var have = Data.TOWER_ORDER.Where(st.Has).ToList();
        int i = have.IndexOf(st.Selected);
        Select(have[(i + d + have.Count) % have.Count]);
    }

    void Select(int type)
    {
        if (st.Has(type))
        {
            st.Selected = type;
            Sfx.Play("move");
        }
        else
        {
            Sfx.Play("error");
            st.Msg = Data.T("locked");
            st.MsgTimer = 50;
        }
    }

    void PressButton(string b)
    {
        if (b.StartsWith("def"))
            Select(int.Parse(b[3..]));
        else if (b == "next")
            st.CallWaveNow();
        else if (b == "speed")
        {
            speed = 1 + speed % 3;
            Sfx.Play("move");
        }
        else if (b is "pause" or "continue")
        {
            paused = !paused;
            quitAsk = false;
            Sfx.Play("move");
        }
        else if (b == "quit")
            LeaveStage();
    }

    // ====================================================================== drawing

    (float x, float y) CellPx(int c, int r) => (FX + c * CELL, FY + r * CELL);

    (float x, float y) UnitPx(float x, float y) => (FX + x * K, FY + y * K);

    static float Lerp(float a, float b, float t) => a + (b - a) * t;

    void Shadow(Gfx g, float x, float y, float w, float a) => g.Ellipse(x, y, w, w * 0.45f, Col.Alpha(Col.BLACK, a));

    public override void Draw(Gfx g)
    {
        Layout();
        buttons.Clear();
        g.Rect(-ExtL, -ExtT, W + ExtL + ExtR, H + ExtT + ExtB, 0xFF1A1729);
        float sx = 0, sy = 0;
        if (st.Shake > 0)
        {
            sx = MathF.Sin(time * 60f) * st.Shake * 6f;
            sy = MathF.Cos(time * 47f) * st.Shake * 6f;
        }
        g.Image(bg, FX + sx, FY + sy, PF_W, PF_H);
        DrawCursor(g);
        DrawWorld(g);
        DrawTopBar(g);
        DrawPanel(g);
        DrawBanner(g);
    }

    void DrawCursor(Gfx g)
    {
        if (!cursorOn || st.GameOver != 0)
            return;
        int c = cursor.X, r = cursor.Y;
        var (px, py) = CellPx(c, r);
        var tw = st.TowerAt[r, c];
        string why = st.BuildCheck(c, r, st.Selected);
        bool ok = tw != null || why == "";
        uint col = ok ? Col.OK : Col.BAD;
        float blink = 0.55f + 0.25f * MathF.Sin(time * 8f);
        g.Rect(px + 2, py + 2, CELL - 4, CELL - 4, Col.Alpha(col, 0.18f * blink));
        g.RectLine(px + 3.5f, py + 3.5f, CELL - 7, CELL - 7, Col.Alpha(col, blink + 0.2f), 3f);
        int rr;
        if (tw != null)
            rr = Data.TTYPES[tw.Type].Range;
        else if (why is "" or "cant_afford")
            rr = Data.TTYPES[st.Selected].Range;
        else
            return;
        float cx = px + CELL / 2, cy = py + CELL / 2;
        g.Circle(cx, cy, rr * K, Col.Alpha(Col.WHITE, holding ? 0.16f : 0.1f));
        g.Arc(cx, cy, rr * K, 0, MathF.Tau, Col.Alpha(Col.WHITE, 0.55f), 2.5f);
        if (holding && tw == null)
        {
            // the defender as it would stand there, to see it under the finger's edge
            g.Sprite(Data.TTYPES[st.Selected].Spr, cx, cy + 6, SPR, false, ok ? 0x99FFFFFF : 0x80FF8080);
        }
    }

    const int SACK = 0, TOWER = 1, ENEMY = 2, PROJ = 3, FX_ = 4;

    void DrawWorld(Gfx g)
    {
        items.Clear();
        for (int k = 0; k < 5; k++)
            items.Add((st.Def.Sacks[k][1] * CELL + CELL * 0.5f, SACK, k));
        foreach (var tw in st.Towers)
            if (tw != null)
                items.Add((tw.R * CELL + CELL * 0.5f, TOWER, tw));
        foreach (var e in st.Enemies)
            items.Add((EnemyPos(e).y * K + (e.Type == Data.DRONE ? 40f : 0f), ENEMY, e));
        foreach (var p in st.Projs)
            if (p.Kind != "laser")
                items.Add((Lerp(p.Py, p.Y, alpha) * K + 30f, PROJ, p));
        foreach (var f in st.Fxs)
            if (f.Kind != "text")
                items.Add((f.Y * K + 20f, FX_, f));
        items.Sort((a, b) => a.y.CompareTo(b.y));
        // shadows first, all on the ground
        foreach (var it in items)
        {
            if (it.kind == ENEMY)
            {
                var e = (Enemy)it.o;
                var ep = EnemyPos(e);
                var (x, y) = UnitPx(ep.x, ep.y);
                Shadow(g, x, y + 4, e.Type == Data.DRONE ? 16f : e.Type == Data.TANK ? 26f : 18f, 0.22f);
            }
            else if (it.kind == TOWER)
            {
                var tw = (Tower)it.o;
                var (x, y) = CellPx(tw.C, tw.R);
                Shadow(g, x + CELL / 2, y + CELL / 2 + 6, 20f, 0.25f);
            }
        }
        foreach (var it in items)
        {
            switch (it.kind)
            {
                case SACK:
                {
                    int k = (int)it.o;
                    var (x, y) = CellPx(st.Def.Sacks[k][0], st.Def.Sacks[k][1]);
                    float wob = st.SacksFull[k] ? MathF.Sin(time * 2f + k) * 0.03f : 0f;
                    g.Sprite(st.SacksFull[k] ? "sack_full" : "sack_empty", x + CELL / 2, y + CELL / 2 + 8, SPR * (1f + wob));
                    break;
                }
                case TOWER:
                    DrawTower(g, (Tower)it.o);
                    break;
                case ENEMY:
                    DrawEnemy(g, (Enemy)it.o);
                    break;
                case PROJ:
                {
                    var p = (Proj)it.o;
                    var (x, y) = UnitPx(Lerp(p.Px, p.X, alpha), Lerp(p.Py, p.Y, alpha));
                    if (p.Kind == "bullet")
                    {
                        g.Circle(x, y, 4f, 0xFF261F1A);
                        g.Circle(x - 1, y - 1, 1.6f, 0xFFFFE680);
                    }
                    else if (p.Kind == "heart")
                        g.Sprite(p.Crit ? "heart_crit" : "heart", x, y + 26, SPR);
                    else if (p.Kind == "dakos")
                    {
                        float d = MathF.Sqrt((p.X - p.SrcX) * (p.X - p.SrcX) + (p.Y - p.SrcY) * (p.Y - p.SrcY));
                        float hop = MathF.Sin(Math.Clamp(d / 40f, 0, 1) * MathF.PI) * 30f;
                        g.Sprite(g.Tex("dakos"), x, y - hop, 0.55f, 0.55f, 64, 74, time * 9f);
                    }
                    break;
                }
                case FX_:
                    DrawEffect(g, (Fx)it.o);
                    break;
            }
        }
        // lasers on top
        foreach (var p in st.Projs)
            if (p.Kind == "laser")
            {
                var (ax, ay) = UnitPx(Lerp(p.Px, p.X, alpha), Lerp(p.Py, p.Y, alpha));
                float dx = p.Tx - p.X, dy = p.Ty - p.Y, len = MathF.Max(0.001f, MathF.Sqrt(dx * dx + dy * dy));
                float bx = ax - dx / len * 26f, by = ay - dy / len * 26f;
                g.Line(bx, by, ax, ay, 0x99FF3366, 7f, true);
                g.Line(bx, by, ax, ay, 0xFFFFD9F2, 3f, true);
            }
        foreach (var f in st.Fxs)
            if (f.Kind == "text")
            {
                float t = 1f - f.Life / f.Total;
                g.Text(FX + f.X, FY + f.Y - t * 34f, f.Text, 28, Col.Alpha(f.Col, f.Life * 2.5f), Align.Center, -1, g.Title, 6,
                    Col.Alpha(Col.BLACK, Math.Clamp(f.Life * 2.5f, 0, 1) * 0.85f));
            }
    }

    void DrawTower(Gfx g, Tower tw)
    {
        var tt = Data.TTYPES[tw.Type];
        var (x, y) = CellPx(tw.C, tw.R);
        x += CELL / 2;
        y += CELL / 2 + 6;
        string name = tt.Spr;
        if (tw.Type == Data.CAULDRON)
        {
            if (tw.Dis > 0)
                name = "cauldron_dented";
            else if ((int)(time * 4f) % 2 == 1)
                name = "cauldron_b";
        }
        if (tw.Type == Data.PSSSST)
            y += MathF.Sin(time * 3f + tw.C) * 4f;
        else
            y -= tw.Kick * 5f;
        // a little squash when it strikes, a gentle breath otherwise
        float sq = 1f + 0.02f * MathF.Sin(time * 2.5f + tw.C * 1.7f) + tw.Kick * 0.06f;
        g.Sprite(g.Tex(name), x, y, SPR * tw.Face * (2f - sq), SPR * sq, 128, 200);
        if (tw.Syn > 0)
        {
            bool good = tw.Syn > 16;
            float bx = x + 20, by = y - 64;
            g.Circle(bx, by, 12, 0x99000000);
            g.Circle(bx, by, 10, good ? Col.OK : Col.BAD);
            g.Text(bx, by + 7, good ? "+" : "-", 20, Col.BLACK, Align.Center);
        }
        if (tw.Dis > 0)
            g.Arc(x - 26, y - 50, 9, -MathF.PI / 2, MathF.Tau * tw.Dis / 125f, 0xCCFFFFFF, 3f);
    }

    /// <summary>where to draw an enemy's centre, in steps: the logic moves it whole steps 25
    /// times a second, so between ticks it is drawn on its way through the next step by the
    /// fraction of a step its speed has built up, which keeps its motion even</summary>
    (float x, float y) EnemyPos(Enemy e)
    {
        var segs = e.Path.Segs;
        if (e.Seg >= segs.Length)
            return (e.X + 6, e.Y + 6);
        var s = segs[e.Seg];
        float frac = Math.Clamp((e.Acc + Stage.Speed(e) * alpha) / 16f, 0, 1);
        return (e.X + 6 + s[0] * frac, e.Y + 6 + s[1] * frac);
    }

    void DrawEnemy(Gfx g, Enemy e)
    {
        var et = Data.ETYPES[e.Type];
        var ep = EnemyPos(e);
        var (x, y) = UnitPx(ep.x, ep.y);
        var segs = e.Path.Segs;
        var s = segs[Math.Min(e.Seg, segs.Length - 1)];
        string dir = s[0] > 0 ? "right" : s[0] < 0 ? "left" : s[1] < 0 ? "up" : "down";
        int frame = (e.Anim >> 2) & 1;
        if (e.Type == Data.DRONE)
        {
            frame = (e.Anim >> 1) & 1;
            y += MathF.Sin(time * 5f + e.Gen) * 4f;
        }
        else
            y -= MathF.Abs(MathF.Sin(e.Anim * MathF.PI / 8f)) * 3f;
        float r = 1f, gr = 1f, b = 1f;
        if (e.Flash > 0)
        {
            gr = 1f - e.Flash * 0.6f;
            b = gr;
        }
        if (e.Slow > 0)
        {
            r *= 0.75f;
            b *= 0.75f;
        }
        g.Sprite($"{et.Name}_{dir}_{frame}", x, y, SPR, false, Col.Rgba(r, gr, b));
        if (e.Hp < e.MaxHp)
        {
            const float w = 34f;
            float tx = x - w / 2, ty = y + (e.Type != Data.TANK ? -76 : -84);
            g.Rect(tx, ty, w, 6, 0xB3000000);
            float f = Math.Clamp((float)e.Hp / e.MaxHp, 0, 1);
            g.Rect(tx + 1, ty + 1, (w - 2) * f, 4, Col.Lerp(Col.OK, Col.BAD, 1f - f));
        }
        if (e.Slow > 0 && e.Type != Data.DRONE)
            g.Circle(x + 14, y - 60, 4, 0xCC99FF4D);
    }

    void DrawEffect(Gfx g, Fx f)
    {
        var (x, y) = UnitPx(f.X, f.Y);
        float t = 1f - f.Life / f.Total;
        float a = Math.Clamp(f.Life / f.Total * 2f, 0, 1);
        switch (f.Kind)
        {
            case "spark":
            case "zap":
            {
                uint col = f.Kind == "spark" ? Col.Rgba(1, 0.9f, 0.2f, a) : Col.Rgba(0.5f, 1, 1, a);
                for (int i = 0; i < 6; i++)
                {
                    float ang = i * MathF.Tau / 6 + t, cs = MathF.Cos(ang), sn = MathF.Sin(ang);
                    g.Line(x + cs * 4, y + sn * 4, x + cs * (8 + t * 14), y + sn * (8 + t * 14), col, 3f, true);
                }
                break;
            }
            case "bash":
            case "boom":
            {
                float sc = (0.6f + t * 0.6f) * (f.Kind == "bash" ? 1f : 1.2f) * SPR;
                g.Sprite(g.Tex("fx_" + f.Kind), x, y + 30, sc, sc, 128, 200, t * 2f, Col.Alpha(Col.WHITE, a));
                if (f.Kind == "boom" || t < 0.5f)
                    g.Text(x, y - 10, f.Kind == "boom" ? "BOOM!" : "BONK!", f.Kind == "boom" ? 24 : 20,
                        Col.Rgba(1, 1, 0.6f, a), Align.Center, -1, g.Title, 5, Col.Alpha(Col.BLACK, a * 0.85f));
                break;
            }
            case "notes":
            {
                float sc = (0.7f + t * 0.4f) * SPR;
                g.Sprite(g.Tex((int)(t * 4) % 2 == 0 ? "fx_notes" : "fx_notes_b"), x, y + 30 - t * 40, sc, sc, 128, 200, 0, Col.Alpha(Col.WHITE, a));
                break;
            }
            case "puff":
            case "poof":
            case "poof_b":
            {
                string nm = f.Kind == "puff" ? "fx_puff" : f.Kind == "poof" ? "fx_poof" : "fx_poof_b";
                float sc = (0.5f + t * 0.7f) * SPR;
                g.Sprite(g.Tex(nm), x, y + 30, sc, sc, 128, 200, 0, Col.Alpha(Col.WHITE, a));
                break;
            }
        }
    }

    // ------------------------------------------------------------------ the bars

    void DrawTopBar(Gfx g)
    {
        g.Rect(-ExtL, oy, W + ExtL + ExtR, TOP, Col.BAR);
        g.Rect(-ExtL, oy + TOP - 3, W + ExtL + ExtR, 3, Col.GOLD);
        float by = oy + 44;
        // left to right: stage, wave, countdown, speed
        float x = ox + 16;
        x += g.Text(x, by, Data.StageText(st.StageNo, 0), 26, Col.GOLD, Align.Left, -1, g.Title) + 28;
        x += g.Text(x, by, $"{Data.T("wave")} {st.WaveNum}/{Data.NUM_WAVES}", 28, Col.WHITE) + 22;
        string cd = st.WaveActive ? Data.T("go") : Data.T("next_in", st.CdSec);
        x += g.Text(x, by, cd, 28, st.WaveActive ? Col.OK : 0xFF99D9FF) + 18;
        if (speed > 1)
            g.Text(x, by, $"x{speed}", 28, Col.GOLD);
        // right to left: the sacks bar, vrouves, lives, score
        float rx = ox + PF_W + pw - 16 - 200;
        uint hpcol = st.Hp < 30 ? Col.BAD : Col.WHITE;
        g.RoundRect(rx, oy + 14, 200, 34, 6, 0x80000000);
        g.RoundRect(rx + 3, oy + 17, 194 * st.Hp / 100f, 28, 5, Col.Lerp(Col.OK, Col.BAD, 1f - st.Hp / 100f));
        g.Text(rx, oy + 41, $"{Data.T("hp")} {st.Hp}", 22, hpcol, Align.Center, 200, null, 4);
        rx -= 24;
        rx -= g.Text(rx, by, $"{st.Funds}", 32, Col.GOLD, Align.Right);
        g.Sprite("sack_full", rx - 26, oy + 60, 0.3f);
        rx -= 78;
        rx -= g.Text(rx, by, $"{Data.T("lives")} {st.Lives}", 28, 0xFFFF99CC, Align.Right) + 28;
        g.Text(rx, by, $"{Data.T("score")} {st.Score:D5}", 28, Col.WHITE, Align.Right);
        if (st.Msg != "")
        {
            float w = g.Width(st.Msg, 28) + 48;
            float mx = FX + PF_W / 2 - w / 2, my = FY + 12;
            g.RoundRect(mx, my, w, 50, 10, 0xE61A40BF);
            g.RoundRectLine(mx, my, w, 50, 10, Col.GOLD, 2.5f);
            g.Text(mx, my + 35, st.Msg, 28, Col.WHITE, Align.Center, w);
        }
    }

    void DrawPanel(Gfx g)
    {
        float x0 = PX, y0 = FY;
        g.Rect(x0, y0, pw + ExtR + (W - x0 - pw), PF_H, Col.BAR);
        g.Rect(x0, y0, 3, PF_H, Col.GOLD);
        // the defender cards, two to a row
        const float gap = 10f, ch = 118f;
        float cw = (pw - 3 - gap * 3) / 2;
        for (int i = 0; i < Data.TOWER_ORDER.Length; i++)
        {
            int type = Data.TOWER_ORDER[i];
            var tt = Data.TTYPES[type];
            float cx = x0 + 3 + gap + (i % 2) * (cw + gap), cy = y0 + gap + (i / 2) * (ch + gap);
            string key = "def" + type;
            buttons[key] = (cx, cy, cw, ch);
            bool unlocked = st.Has(type), afford = st.Funds >= tt.Cost, sel = type == st.Selected;
            g.RoundRect(cx, cy, cw, ch, 12, sel ? 0xFF267340 : pressed == key ? 0xFF524C73 : 0xFF403859);
            g.RoundRectLine(cx, cy, cw, ch, 12, sel ? Col.GOLD : 0x40FFFFFF, sel ? 4f : 2f);
            float sprX = cx + Math.Min(cw * 0.3f, 70f);
            if (unlocked)
            {
                g.Sprite(tt.Spr, sprX, cy + ch + 2, 0.56f, false, afford ? Col.WHITE : 0x73FFFFFF);
                g.Text(cx + cw - 12, cy + 42, $"{tt.Cost}", 32, afford ? Col.GOLD : Col.BAD, Align.Right, -1, g.Title);
                string nm = Data.T("name" + type);
                float fs = 24, room = cw - (sprX - cx) - 40;
                while (fs > 14 && g.Width(nm, fs) > room)
                    fs--;
                g.Text(cx + cw - 12, cy + ch - 18, nm, fs, Col.WHITE, Align.Right);
            }
            else
            {
                g.Sprite(tt.Spr, cx + cw / 2, cy + ch + 2, 0.56f, false, 0x80000000);
                g.Text(cx, cy + ch / 2 + 16, "?", 44, 0x80FFFFFF, Align.Center, cw, g.Title);
            }
        }
        // the buttons at the bottom: Next wave, then Speed and Pause side by side
        float bw = pw - 3 - gap * 2, bx = x0 + 3 + gap;
        float byPause = y0 + PF_H - gap - 72, byNext = byPause - gap - 84;
        bool dim = st.WaveActive || st.GameOver != 0;
        Button(g, "next", bx, byNext, bw, 84, Data.T("next_btn"), dim ? 0xFF2E2A3D : 0xFFB33326, dim ? 0x59FFFFFF : Col.WHITE, 32);
        float hw = (bw - gap) / 2;
        Button(g, "speed", bx, byPause, hw, 72, Data.T("speed_btn", speed), speed > 1 ? 0xFF8C6A14 : 0xFF38334D, Col.WHITE, 24);
        Button(g, "pause", bx + hw + gap, byPause, hw, 72, Data.T("pause_btn"), 0xFF38334D, Col.WHITE, 24);
        // the info: what is under the cursor, or the chosen defender
        float iy = y0 + gap + 4 * (ch + gap) + 30, ibottom = byNext - gap;
        string info, info2 = "";
        uint infoCol = Col.WHITE;
        var tw = cursorOn ? st.TowerAt[cursor.Y, cursor.X] : null;
        if (tw != null)
        {
            info = $"{Data.T("name" + tw.Type)} - {Data.T("sell", Data.TTYPES[tw.Type].Sell)}";
            if (tw.Syn != 0)
            {
                info2 = SynLabel(tw.Syn);
                infoCol = tw.Syn > 16 ? Col.OK : Col.BAD;
            }
        }
        else
        {
            info = $"{Data.T("name" + st.Selected)} {Data.TTYPES[st.Selected].Cost}: {Data.T("desc" + st.Selected)}";
            string why = cursorOn ? st.BuildCheck(cursor.X, cursor.Y, st.Selected) : "-";
            if (why is "" or "cant_afford")
            {
                int f = st.SynFactorAt(cursor.X, cursor.Y, st.Selected);
                if (f != 0 && f < 16)
                {
                    info2 = SynLabel(f);
                    infoCol = Col.BAD;
                }
                else if (f > 16)
                {
                    bool full = st.SynCount() >= Data.SYN_MAX;
                    info2 = SynLabel(f) + (full ? " " + Data.T("bonus_full") : "");
                    infoCol = full ? 0xFFB3B3B3 : Col.OK;
                }
            }
        }
        float fsz = 24;
        int lines = info2 != "" ? (int)((ibottom - iy - 8) / Gfx.LineHeight(fsz)) : (int)((ibottom - iy + 20) / Gfx.LineHeight(fsz));
        float ny = g.Multiline(bx, iy, info, bw, fsz, Col.WHITE, Align.Left, null, Math.Max(1, lines));
        if (info2 != "")
            g.Text(bx, Math.Min(ny + 4, ibottom), info2, fsz, infoCol);
    }

    void Button(Gfx g, string id, float x, float y, float w, float h, string label, uint bgc, uint fg, float size)
    {
        buttons[id] = (x, y, w, h);
        bool on = pressed == id;
        g.RoundRect(x + 4, y + 5, w, h, 14, 0x59000000);
        g.RoundRect(x, y, w, h, 14, on ? Col.Lerp(bgc, Col.WHITE, 0.25f) : bgc);
        g.RoundRectLine(x, y, w, h, 14, on ? Col.GOLD : 0x4DFFFFFF, 2.5f);
        while (size > 14 && g.Width(label, size, g.Title) > w - 16)
            size--;
        g.Text(x, y + h / 2 + size * 0.36f, label, size, fg, Align.Center, w, g.Title);
    }

    static string SynLabel(int f)
    {
        string pct = f switch { 19 => "+20%", 24 => "+50%", 13 => "-20%", 8 => "-50%", _ => "" };
        return $"{Data.T("neighbour")} {pct}";
    }

    // ------------------------------------------------------------------ banners

    void DrawBanner(Gfx g)
    {
        if (paused && st.GameOver == 0)
        {
            g.Rect(FX, FY, PF_W, PF_H, 0x73000000);
            float by = Box(g, quitAsk ? [Data.T("quit_ask"), Data.T("quit_ask2")] : [Data.T("paused"), Data.T("paused2")]);
            for (int i = 0; i < 2; i++)
            {
                string id = i == 0 ? "continue" : "quit";
                Button(g, id, FX + PF_W / 2 - 330 + i * 340, by + 30, 320, 84, Data.T(i == 0 ? "continue" : "to_title"),
                    i == 1 ? 0xFFD93326 : 0xFF267340, Col.WHITE, 30);
            }
            return;
        }
        if (st.Banner.Count > 0)
            Box(g, st.Banner);
    }

    /// <summary>the big blue box in the middle of the map: a title and lines below it;
    /// returns its bottom edge</summary>
    float Box(Gfx g, List<string> lines)
    {
        float w = 0;
        for (int i = 0; i < lines.Count; i++)
            w = Math.Max(w, g.Width(lines[i], i == 0 ? 46 : 28, i == 0 ? g.Title : null));
        w = Math.Min(w + 80, PF_W - 40);
        float h = 44 + 58 + (lines.Count - 1) * 42;
        float bx = FX + PF_W / 2 - w / 2, by = FY + PF_H / 2 - h / 2 - 60;
        g.RoundRect(bx + 8, by + 8, w, h, 18, 0x59000000);
        g.RoundRect(bx, by, w, h, 18, 0xF2143399);
        g.RoundRectLine(bx, by, w, h, 18, Col.GOLD, 4f);
        float y = by + 64;
        for (int i = 0; i < lines.Count; i++)
        {
            if (i == 0)
            {
                float sz = 46;
                while (sz > 26 && g.Width(lines[0], sz, g.Title) > w - 40)
                    sz--;
                g.Text(bx, y, lines[i], sz, Col.GOLD, Align.Center, w, g.Title, 8);
                y += 52;
            }
            else
            {
                bool blink = st.GameOver != 0 && i == lines.Count - 1 && endWait > 1.2f;
                uint col = blink ? Col.Alpha(Col.WHITE, 0.6f + 0.4f * MathF.Sin(time * 6f)) : Col.WHITE;
                float sz = 28;
                while (sz > 16 && g.Width(lines[i], sz) > w - 40)
                    sz--;
                g.Text(bx + 20, y, lines[i], sz, col, Align.Center, w - 40);
                y += 42;
            }
        }
        return by + h;
    }
}

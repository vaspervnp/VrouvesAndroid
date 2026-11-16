using Android.Views;

namespace Vrouves;

/// <summary>The title screen: the menu, the pages (story, defenders, invaders, how to play,
/// high scores) and the name entry for a new high score. As VrouvesPC/scripts/title.gd.</summary>
public sealed class TitleScreen : Screen
{
    const uint C_RED = 0xFFFF4D40;
    static readonly string[] PAGES = ["story", "defenders", "invaders", "howto"];
    static readonly string[] KEYS = ["ABCDEFGHIJKLM", "NOPQRSTUVWXYZ", "0123456789"];

    readonly App app;
    string page = "menu";
    readonly int entryScore;
    string entryName = "";
    int entryRank = -1;
    int sel;
    string pressed = "";
    readonly List<(string id, (float x, float y, float w, float h) r)> items = [];
    float time;

    public TitleScreen(App app, string pageOnStart, int entryScore)
    {
        this.app = app;
        this.entryScore = entryScore;
        if (pageOnStart != "")
            page = pageOnStart;
        Sfx.Music(true);
    }

    public override void Update(float dt) => time += dt;

    static bool CanContinue => Data.SavedStage > 1 && Data.SavedStage <= Data.Stages.Count;

    (string id, string label)[] MenuItems() =>
    [
        .. (CanContinue ? new[] { ("continue", Data.T("continue_at", Data.SavedStage)), ("start", Data.T("new_game")) }
            : new[] { ("start", Data.T("start")) }),
        ("howto", Data.T("howto")),
        ("hiscores", Data.T("hiscores_t")),
        ("dif", Data.T("dif" + Data.Difficulty)),
        ("lang", Data.T("language")),
        ("music", Data.MusicOn ? Data.T("music") : Data.T("music_off")),
    ];

    void Activate(string id)
    {
        Sfx.Play("move");
        switch (id)
        {
            case "start":
                Sfx.Play("wave");
                app.NewGame();
                break;
            case "continue":
                Sfx.Play("wave");
                app.ContinueGame();
                break;
            case "howto":
                page = "story";
                break;
            case "hiscores":
                page = "hiscores";
                break;
            case "dif":
                Data.Difficulty = (Data.Difficulty + 1) % 4;
                Data.SaveSettings();
                break;
            case "lang":
                Data.ToggleLang();
                break;
            case "music":
                Sfx.ToggleMusic();
                break;
            case "ok":
                EntryDone();
                break;
            case "del":
                if (entryName.Length > 0)
                    entryName = entryName[..^1];
                break;
            case "back":
                page = "menu";
                break;
            case "next":
            {
                int i = Array.IndexOf(PAGES, page);
                page = i >= 0 ? PAGES[(i + 1) % PAGES.Length] : "menu";
                break;
            }
            default:
                if (id.StartsWith("key") && entryName.Length < 3)
                    entryName += id[3..];
                break;
        }
    }

    public override void Down(float x, float y)
    {
        foreach (var (id, r) in items)
            if (In(x, y, r))
            {
                pressed = id;
                return;
            }
        pressed = "";
    }

    public override void Up(float x, float y)
    {
        string p = pressed;
        pressed = "";
        foreach (var (id, r) in items)
            if (In(x, y, r))
            {
                if (id == p)
                    Activate(id);
                return;
            }
        if (p == "" && page != "menu" && page != "enter")
            Activate(Array.IndexOf(PAGES, page) >= 0 ? "next" : "back");
    }

    public override void Cancel() => pressed = "";

    public override bool Back()
    {
        if (page == "menu")
            return false;
        if (page == "enter")
            EntryDone();
        else
            Activate("back");
        return true;
    }

    public override bool Key(Keycode k, KeyEvent e)
    {
        if (page == "enter")
            return EntryKey(k, e);
        switch (k)
        {
            case Keycode.L:
                Data.ToggleLang();
                return true;
            case Keycode.M:
                Sfx.ToggleMusic();
                return true;
            case Keycode.D:
                Activate("dif");
                return true;
        }
        if (page == "menu")
        {
            int n = MenuItems().Length;
            switch (k)
            {
                case Keycode.DpadUp or Keycode.Q:
                    sel = (sel + n - 1) % n;
                    Sfx.Play("move");
                    return true;
                case Keycode.DpadDown or Keycode.A:
                    sel = (sel + 1) % n;
                    Sfx.Play("move");
                    return true;
                case Keycode.Enter or Keycode.NumpadEnter or Keycode.DpadCenter or Keycode.ButtonA:
                    Activate(MenuItems()[sel].id);
                    return true;
                case Keycode.Space or Keycode.ButtonStart:
                    Activate("start");
                    return true;
                case Keycode.DpadRight or Keycode.P:
                    page = "story";
                    return true;
            }
            return false;
        }
        switch (k)
        {
            case Keycode.Del or Keycode.ButtonB:
                Activate("back");
                return true;
            case Keycode.DpadRight or Keycode.P or Keycode.Enter or Keycode.NumpadEnter or Keycode.DpadCenter or Keycode.ButtonA:
                Activate(Array.IndexOf(PAGES, page) >= 0 ? "next" : "back");
                return true;
            case Keycode.DpadLeft or Keycode.O:
            {
                int i = Array.IndexOf(PAGES, page);
                page = i >= 0 ? PAGES[(i + PAGES.Length - 1) % PAGES.Length] : "menu";
                return true;
            }
            case Keycode.Space or Keycode.ButtonStart:
                Activate("start");
                return true;
        }
        return false;
    }

    bool EntryKey(Keycode k, KeyEvent e)
    {
        if (k is Keycode.Enter or Keycode.NumpadEnter)
        {
            EntryDone();
            return true;
        }
        if (k == Keycode.Del)
        {
            if (entryName.Length > 0)
                entryName = entryName[..^1];
            return true;
        }
        int u = e.UnicodeChar;
        if (u > 32 && entryName.Length < 3)
        {
            string ch = char.ConvertFromUtf32(u).ToUpperInvariant();
            if (char.IsLetterOrDigit(ch[0]))
            {
                entryName += ch;
                Sfx.Play("move");
                return true;
            }
        }
        return false;
    }

    void EntryDone()
    {
        string nm = entryName.Trim();
        if (nm == "")
            nm = "???";
        entryRank = Data.AddHiscore(nm.ToUpperInvariant(), entryScore);
        page = "hiscores";
        Sfx.Play("build");
    }

    // ====================================================================== drawing

    void Panel(Gfx g, (float x, float y, float w, float h) r, uint col = 0xD11A1F4D)
    {
        g.RoundRect(r.x + 8, r.y + 8, r.w, r.h, 20, 0x4D000000);
        g.RoundRect(r.x, r.y, r.w, r.h, 20, col);
        g.RoundRectLine(r.x, r.y, r.w, r.h, 20, Col.GOLD, 3f);
    }

    void Item(Gfx g, string id, (float x, float y, float w, float h) r, string label, uint bg, float size, bool on, bool title = false)
    {
        items.Add((id, r));
        bool down = pressed == id;
        g.RoundRect(r.x + 6, r.y + 6, r.w, r.h, 14, 0x4D000000);
        g.RoundRect(r.x, r.y, r.w, r.h, 14, down ? Col.Lerp(bg, Col.WHITE, 0.25f) : bg);
        g.RoundRectLine(r.x, r.y, r.w, r.h, 14, on || down ? Col.GOLD : 0x4DFFFFFF, on || down ? 3f : 1.5f);
        while (size > 14 && g.Width(label, size, title ? g.Title : null) > r.w - 20)
            size--;
        g.Text(r.x, r.y + r.h / 2 + size * 0.36f, label, size, Col.WHITE, Align.Center, r.w, title ? g.Title : null, title ? 4 : 0);
    }

    public override void Draw(Gfx g)
    {
        items.Clear();
        // the picture fills the screen; on a wide one its lower part shows, with the village
        var bg = app.TitleBitmap();
        float fw = W + ExtL + ExtR, fh = H + ExtT + ExtB;
        float sc = Math.Max(fw / bg.Width, fh / bg.Height);
        float bw = bg.Width * sc, bh = bg.Height * sc;
        g.Image(bg, -ExtL + (fw - bw) / 2, -ExtT + fh - bh + (bh - fh) * 0.3f, bw, bh);
        g.Rect(-ExtL, -ExtT, fw, fh, page == "menu" ? 0x26000000u : 0x73000000u);
        // the title, bouncing a little
        float bob = MathF.Sin(time * 2f) * 4f;
        g.Text(0, 100 + bob, Data.TEXT["title"][0], 80, Col.GOLD, Align.Center, W, g.Title, 16);
        g.Text(0, 170 - bob, Data.TEXT["title"][1], 56, C_RED, Align.Center, W, g.Title, 14);
        switch (page)
        {
            case "menu":
                DrawMenu(g);
                break;
            case "story":
                DrawStory(g);
                break;
            case "defenders":
                DrawDefenders(g);
                break;
            case "invaders":
                DrawInvaders(g);
                break;
            case "howto":
                DrawHowto(g);
                break;
            case "hiscores":
                DrawHiscores(g);
                break;
            case "enter":
                DrawEntry(g);
                break;
        }
        g.Text(0, H - 16, Data.T("credit"), 18, 0xBFFFFFFF, Align.Center, W, null, 4);
    }

    void DrawMenu(Gfx g)
    {
        g.Text(0, 222, Data.T("subtitle"), 30, Col.WHITE, Align.Center, W, null, 6);
        var mi = MenuItems();
        float y = 270;
        for (int i = 0; i < mi.Length; i++)
        {
            bool big = i == 0;
            var r = (W / 2 - 260, y, 520f, big ? 92f : 70f);
            bool on = i == sel;
            uint bgc = big ? 0xF2D93326 : on ? 0xF21F59CC : 0xD91A1F4D;
            Item(g, mi[i].id, r, mi[i].label, bgc, big ? 44 : 30, on, big);
            y += r.Item4 + 14;
        }
    }

    (float x, float y, float w, float h) PageFrame(Gfx g, string title)
    {
        float pw = Math.Min(W - 120, 1400);
        var r = (x: W / 2 - pw / 2, y: 206f, w: pw, h: H - 250);
        Panel(g, r);
        g.Text(r.x, r.y + 56, title, 40, Col.GOLD, Align.Center, r.w, g.Title, 6);
        int i = Array.IndexOf(PAGES, page);
        if (i >= 0)
        {
            // next page, as touching anywhere else does
            var nb = (r.x + r.w - 190, r.y + 14, 170f, 60f);
            Item(g, "next", nb, $"{i + 1}/{PAGES.Length}  >", 0xFF267340, 26, false);
        }
        Item(g, "back", (r.x + 20, r.y + 14, 170f, 60f), Data.T("back"), 0xFFD93326, 26, false);
        return r;
    }

    void DrawStory(Gfx g)
    {
        var r = PageFrame(g, Data.T("story_t"));
        g.Multiline(r.x + 60, r.y + 140, Data.T("story"), r.w - 420, 30, Col.WHITE);
        g.Sprite("psssst", r.x + r.w - 170, r.y + 350 + MathF.Sin(time * 3f) * 8f, 1.3f);
        g.Sprite("sack_full", r.x + r.w - 170, r.y + r.h - 70, 1.3f);
    }

    void DrawDefenders(Gfx g)
    {
        var r = PageFrame(g, Data.T("defenders_t"));
        int n = Data.TOWER_ORDER.Length;
        // the rows share the room above the neighbour rules
        float top = r.y + 92, rowH = Math.Min(84f, (r.h - 92 - 96) / n);
        float k = rowH / 84f;
        for (int i = 0; i < n; i++)
        {
            int type = Data.TOWER_ORDER[i];
            var tt = Data.TTYPES[type];
            float y = top + i * rowH;
            g.Sprite(tt.Spr, r.x + 90, y + 74 * k, 0.5f * k);
            g.Text(r.x + 160, y + 34 * k, $"{i + 1}. {Data.T("name" + type)} - {tt.Cost}", 30 * k, Col.GOLD);
            g.Text(r.x + 160, y + 66 * k, Data.T("desc" + type), 25 * k, Col.WHITE);
        }
        g.Multiline(r.x + 60, top + n * rowH + 22, Data.T("synergy_help"), r.w - 120, 20, 0xFFB3FF99, Align.Center);
    }

    void DrawInvaders(Gfx g)
    {
        var r = PageFrame(g, Data.T("invaders_t"));
        string[] names = ["drone", "guard", "tank"];
        for (int i = 0; i < 3; i++)
        {
            float y = r.y + 120 + i * 180;
            g.Sprite($"{names[i]}_down_{(int)(time * 3) % 2}", r.x + 130, y + 120, 1f);
            g.Text(r.x + 260, y + 60, Data.T("ename" + (i + 1)), 36, C_RED, Align.Left, -1, g.Title, 4);
            g.Multiline(r.x + 260, y + 104, Data.T("edesc" + (i + 1)), r.w - 330, 27, Col.WHITE);
        }
    }

    void DrawHowto(Gfx g)
    {
        var r = PageFrame(g, Data.T("howto"));
        float y = g.Multiline(r.x + 60, r.y + 140, Data.T("controls"), r.w - 120, 28, Col.WHITE);
        g.Multiline(r.x + 60, y + 20, Data.T("synergy_help"), r.w - 120, 25, 0xFFB3FF99);
        string[] who = ["cretan", "bride", "shepherd", "yiayia", "priest"];
        for (int i = 0; i < who.Length; i++)
            g.Sprite(who[i], r.x + r.w / 2 + (i - 2) * 160, r.y + r.h - 24, 0.6f, i % 2 == 1);
    }

    void DrawHiscores(Gfx g)
    {
        var r = PageFrame(g, Data.T("hiscores_t"));
        float rowH = Math.Min(64, (r.h - 130) / 8);
        for (int i = 0; i < Data.Hiscores.Count; i++)
        {
            float y = r.y + 140 + i * rowH;
            uint col = i == entryRank ? Col.GOLD : Col.WHITE;
            if (i == entryRank)
                g.RoundRect(r.x + r.w / 2 - 340, y - 44, 680, rowH - 4, 10, Col.Alpha(Col.WHITE, 0.12f + 0.08f * MathF.Sin(time * 6f)));
            g.Text(r.x + r.w / 2 - 280, y, $"{i + 1}.", 38, col);
            g.Text(r.x + r.w / 2 - 170, y, Data.Hiscores[i].Name, 38, col, Align.Left, -1, g.Title);
            g.Text(r.x + r.w / 2 + 280, y, $"{Data.Hiscores[i].Score:D5}", 38, col, Align.Right);
        }
    }

    void DrawEntry(Gfx g)
    {
        float pw = Math.Min(W - 120, 1100);
        var r = (x: W / 2 - pw / 2, y: 200f, w: pw, h: H - 240);
        Panel(g, r);
        g.Text(r.x, r.y + 64, Data.T("newhs"), 50, Col.GOLD, Align.Center, r.w, g.Title, 8);
        g.Text(r.x, r.y + 114, Data.T("final_score", entryScore), 30, Col.WHITE, Align.Center, r.w);
        g.Text(r.x, r.y + 160, Data.T("entername"), 26, 0xFFCCE6FF, Align.Center, r.w);
        for (int i = 0; i < 3; i++)
        {
            float bx = W / 2 - 150 + i * 105, by = r.y + 180;
            g.Rect(bx, by, 90, 104, 0x66000000);
            bool on = i == entryName.Length && (int)(time * 3) % 2 == 0;
            g.RectLine(bx, by, 90, 104, on ? Col.GOLD : 0x80FFFFFF, 3f);
            if (i < entryName.Length)
                g.Text(bx, by + 82, entryName[i].ToString(), 72, Col.WHITE, Align.Center, 90, g.Title);
        }
        // the letters, as big as fit
        float ky = r.y + 304, kw = Math.Min(84, (r.w - 40) / 13), kh = 76;
        foreach (string row in KEYS)
        {
            float x = W / 2 - row.Length * kw / 2;
            foreach (char ch in row)
            {
                Item(g, "key" + ch, (x + 3, ky, kw - 6, kh), ch.ToString(), 0xF21F59CC, 36, false, true);
                x += kw;
            }
            ky += kh + 8;
        }
        Item(g, "del", (W / 2 - 310, ky + 8, 290f, 76f), Data.T("del"), 0xF2666680, 32, false, true);
        Item(g, "ok", (W / 2 + 20, ky + 8, 290f, 76f), Data.T("ok"), 0xF2D93326, 32, false, true);
    }
}

using Android.App;
using Android.Content;
using Android.Graphics;

namespace Vrouves;

/// <summary>The flow of a game: title -> stages 1..10 -> ending -> high score -> title.
/// Lives last the whole game; a stage lost with a life left is played again with the score
/// it started with. As VrouvesPC/scripts/main.gd.</summary>
public sealed class App
{
    public readonly Activity Activity;
    public readonly Gfx Gfx;
    public Screen Screen = null!;

    int stageNo = 1, score, stageScore, lives;

    Bitmap? stageBmp;
    int stageBmpNo;
    Task<Bitmap>? prefetch;
    int prefetchNo;
    Bitmap? titleBmp, endingBmp;

    public App(Activity activity, Gfx gfx)
    {
        Activity = activity;
        Gfx = gfx;
    }

    public void Start() => ShowTitle();

    /// <summary>a game from a later stage, with three lives (for testing); with bot, the
    /// stage plays by itself</summary>
    public void StartAt(int n, bool bot = false)
    {
        NewGame();
        stageNo = n;
        lives = 3;
        StartStage();
        if (bot && Screen is GameScreen g)
        {
            using (var s = Activity.Assets!.Open("data/plans.json"))
                Data.LoadPlans(s);
            g.PlayBot(Data.Plans[n - 1]);
        }
    }

    public void ShowEnding(int s) => Swap(new EndingScreen(this, s));

    void Swap(Screen s)
    {
        Screen?.Leave();
        Screen = s;
    }

    public void ShowTitle(string page = "", int entryScore = -1)
    {
        Swap(new TitleScreen(this, page, entryScore));
        PrefetchStage(1);
    }

    public void NewGame()
    {
        stageNo = 1;
        score = 0;
        stageScore = 0;
        lives = Data.DIFFICULTY_LIVES[Data.Difficulty];
        StartStage();
    }

    void StartStage()
    {
        var g = new GameScreen(this, StageBitmap(stageNo));
        g.Setup(stageNo, stageScore, lives);
        Swap(g);
        PrefetchStage(stageNo + 1);
    }

    public void StageFinished(string result, int s)
    {
        switch (result)
        {
            case "won":
                score = s;
                stageScore = s;
                if (stageNo >= Data.Stages.Count)
                    Swap(new EndingScreen(this, score));
                else
                {
                    stageNo++;
                    StartStage();
                }
                break;
            case "lost":
                if (lives > 0)
                {
                    lives--;
                    StartStage();
                }
                else
                    GameEnded(s);
                break;
            default:
                GameEnded(s);
                break;
        }
    }

    public void GameEnded(int s)
    {
        Sfx.Music(false);
        ShowTitle(Data.HiscoreRank(s) >= 0 ? "enter" : "hiscores", s);
    }

    // ------------------------------------------------------------------ pictures

    public Bitmap TitleBitmap() => titleBmp ??= Gfx.LoadBitmap("title.jpg", true);

    public Bitmap EndingBitmap() => endingBmp ??= Gfx.LoadBitmap("ending.jpg", true);

    /// <summary>a stage's map picture; only the one being played and the next are kept</summary>
    Bitmap StageBitmap(int n)
    {
        if (stageBmp != null && stageBmpNo == n)
            return stageBmp;
        Bitmap b;
        if (prefetch != null && prefetchNo == n)
            b = prefetch.Result;
        else
            b = Gfx.LoadBitmap($"stages/stage{n}.jpg", true);
        prefetch = null;
        // the screen that showed the old one is replaced before the next frame is drawn
        var old = stageBmp;
        stageBmp = b;
        stageBmpNo = n;
        if (old != null && old != b)
            old.Recycle();
        return b;
    }

    /// <summary>start loading a stage's picture in the background, so starting it does not stall</summary>
    void PrefetchStage(int n)
    {
        if (n < 1 || n > Data.Stages.Count || (stageBmp != null && stageBmpNo == n) || (prefetch != null && prefetchNo == n))
            return;
        prefetchNo = n;
        prefetch = Task.Run(() => Gfx.LoadBitmap($"stages/stage{n}.jpg", true));
    }
}

/// <summary>The settings and the high scores, in the app's SharedPreferences.</summary>
public static class Store
{
    static ISharedPreferences? prefs;

    public static void Load(Context ctx)
    {
        prefs = ctx.GetSharedPreferences("vrouves", FileCreationMode.Private)!;
        int lang = prefs.GetInt("lang", -1);
        // English until the player picks Ελληνικά on the title screen
        Data.Lang = lang >= 0 ? lang : 0;
        Data.MusicOn = prefs.GetBoolean("music", true);
        Data.Difficulty = Math.Clamp(prefs.GetInt("difficulty", 0), 0, 3);
        Data.ResetHiscores();
        var hs = prefs.GetString("hiscores", "") ?? "";
        var rows = hs.Split(';', StringSplitOptions.RemoveEmptyEntries);
        if (rows.Length == 8)
        {
            var list = new List<(string, int)>();
            foreach (var row in rows)
            {
                var p = row.Split(':');
                if (p.Length == 2 && int.TryParse(p[1], out int v))
                    list.Add((p[0], v));
            }
            if (list.Count == 8)
                Data.Hiscores = list;
        }
        Data.SaveSettingsHook = SaveSettings;
        Data.SaveHiscoresHook = SaveHiscores;
    }

    static void SaveSettings() =>
        prefs?.Edit()!.PutInt("lang", Data.Lang)!.PutBoolean("music", Data.MusicOn)!.PutInt("difficulty", Data.Difficulty)!.Apply();

    static void SaveHiscores() =>
        prefs?.Edit()!.PutString("hiscores", string.Join(";", Data.Hiscores.Select(h => $"{h.Name}:{h.Score}")))!.Apply();
}

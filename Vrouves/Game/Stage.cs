namespace Vrouves;

/// <summary>Colours as 0xAARRGGBB.</summary>
public static class Col
{
    public const uint BAR = 0xEB211C33;
    public const uint GOLD = 0xFFFFD933;
    public const uint OK = 0xFF73FF66;
    public const uint BAD = 0xFFFF594D;
    public const uint WHITE = 0xFFFFFFFF;
    public const uint BLACK = 0xFF000000;
    public const uint CRIT = 0xFFFF66B3;

    public static uint Rgba(float r, float g, float b, float a = 1f) =>
        ((uint)(Math.Clamp(a, 0, 1) * 255 + 0.5f) << 24) | ((uint)(Math.Clamp(r, 0, 1) * 255 + 0.5f) << 16) |
        ((uint)(Math.Clamp(g, 0, 1) * 255 + 0.5f) << 8) | (uint)(Math.Clamp(b, 0, 1) * 255 + 0.5f);

    public static uint Alpha(uint c, float a) => (c & 0xFFFFFF) | ((uint)(Math.Clamp(a, 0, 1) * 255 + 0.5f) << 24);

    public static uint Lerp(uint a, uint b, float t)
    {
        t = Math.Clamp(t, 0, 1);
        uint ch(int sh) => (uint)(((a >> sh) & 255) + (((int)((b >> sh) & 255) - (int)((a >> sh) & 255)) * t)) & 255;
        return (ch(24) << 24) | (ch(16) << 16) | (ch(8) << 8) | ch(0);
    }
}

public sealed class PathDef
{
    public int X0, Y0, Seg0, Rem0;
    public int[][] Segs = [];        // dx, dy, steps
}

public sealed class Enemy
{
    public int Type, X, Y, Seg, Rem, Acc, Prog, Slow, Anim, Hp, MaxHp, Arm, Gen;
    public float Px, Py, Flash;
    public PathDef Path = null!;
    public bool Alive = true;
}

public sealed class Tower
{
    public int Type, C, R, Cx, Cy, Cd = 8, Dis, Syn, Face = 1;
    public float Kick;
    public bool OnBlocked;
}

public sealed class Proj
{
    public string Kind = "";
    public float X, Y, Px, Py, Tx, Ty, Speed, SrcX, SrcY;
    public Enemy Target = null!;
    public int Gen, Dmg;
    public bool Crit, Lost;
}

public sealed class Fx
{
    public string Kind = "";
    public float X, Y, Life, Total;
    public string Text = "";
    public uint Col = 0xFFFFFFFF;
}

/// <summary>One stage of LET'S GO FOR VROUVES: the logic runs at 25 ticks a second exactly
/// as on the CPC (VrouvesPC/docs/cpc_mechanics.md); positions are in path steps, 12 to a map
/// cell. A port of VrouvesPC/scripts/game.gd without its drawing.</summary>
public class Stage
{
    /// <summary>layout of the playfield, in the game screen's pixels: the floating texts are
    /// placed in these, relative to the playfield's top left corner</summary>
    public const float CELL = 64f;
    public const float PF_W = CELL * Data.COLS;
    public const float PF_H = CELL * Data.ROWS;
    public const float K = CELL / 12f;           // pixels per step

    public int StageNo = 1;
    public StageDef Def = null!;
    public int[,] CellT = new int[Data.ROWS, Data.COLS];   // 0 buildable, 1 road, 2 blocked (Psssst only), 3 base
    public Tower?[,] TowerAt = new Tower?[Data.ROWS, Data.COLS];
    public readonly Dictionary<string, PathDef> Paths = [];

    public int Funds = Data.START_FUNDS;
    public int Hp = Data.START_HP;
    public int Score;
    public int Lives;
    public int WaveNum;
    public bool WaveActive;
    public int CdSec = Data.FIRST_COUNTDOWN;
    public int CdSub = 25;
    readonly List<int[]> groups = [];
    int spTimer, spLeft, genCtr;

    public readonly List<Enemy> Enemies = [];
    public readonly Tower?[] Towers = new Tower?[32];
    public readonly List<Proj> Projs = [];
    public readonly List<Fx> Fxs = [];
    public readonly bool[] SacksFull = [true, true, true, true, true];

    public int Selected = Data.CRETAN;
    public int GameOver;                         // 1 won, 2 lost
    public string Msg = "";
    public int MsgTimer;
    public List<string> Banner = [];             // lines of the big banner
    public int BanTimer;
    public float Shake;
    readonly Random rng = new();

    public bool Bot;                             // plays by itself (the headless test)
    int botStep;
    public readonly List<int[]> Plan = [];       // the CPC's build plan for this stage, [type, col, row]
    static readonly Dictionary<int, int[]> PLAN_SUBSTITUTE = new()
    {
        [2] = [1], [3] = [1, 4], [5] = [4, 1], [6] = [1], [7] = [4, 1],
    };
    static readonly int[] BOT_PLAN =
    [
        Data.CRETAN, Data.CRETAN, Data.PSSSST, Data.CRETAN, Data.BRIDE, Data.YIAYIA,
        Data.BAKER, Data.CAULDRON, Data.CRETAN, Data.BRIDE, Data.LYRA, Data.PSSSST, Data.YIAYIA,
    ];

    public void Setup(int n, int startScore, int livesLeft)
    {
        StageNo = n;
        Def = Data.Stages[n - 1];
        Score = startScore;
        Lives = livesLeft;
        for (int r = 0; r < Data.ROWS; r++)
            for (int c = 0; c < Data.COLS; c++)
            {
                char ch = Def.Grid[r][c];
                CellT[r, c] = ch is '.' or 'F' ? 0 : ch == '#' ? 1 : ch is 'K' or 'P' ? 3 : 2;
                TowerAt[r, c] = null;
            }
        Paths["ground"] = MakePath(Def.Ground);
        if (Def.Ground2.Length > 0)
            Paths["ground2"] = MakePath(Def.Ground2);
        Paths["air"] = MakePath(Def.Air);
        Selected = Def.Defenders[0];
        Msg = Data.T("call");
        MsgTimer = -1;
        var newdef = new Dictionary<int, int> { [2] = Data.YIAYIA, [3] = Data.BRIDE, [4] = Data.BAKER, [5] = Data.CAULDRON, [8] = Data.LYRA };
        if (StageNo == 1)
        {
            Banner = [Data.StageText(1, 0), $"{Data.T("name1")} - {Data.T("desc1")}", $"{Data.T("name4")} - {Data.T("desc4")}"];
            BanTimer = 250;
        }
        else if (newdef.TryGetValue(StageNo, out int d))
        {
            Banner = [Data.StageText(StageNo, 0), Data.T("newdef"), $"{Data.T("name" + d)} - {Data.T("desc" + d)}"];
            Selected = d;
            BanTimer = 250;
        }
        else if (!Def.Defenders.Contains(Data.BRIDE) && StageNo > 3)
        {
            // the wedding: the Bride is at the altar
            Banner = [Data.StageText(StageNo, 0), Data.T("altar"), Data.T("altar2")];
            BanTimer = 250;
        }
        else
        {
            Banner = [Data.StageText(StageNo, 0)];
            BanTimer = 125;
        }
    }

    public bool Has(int type) => Def.Defenders.Contains(type);

    // ====================================================================== paths

    /// <summary>waypoints (col,row) -> start position in steps and segments (dx, dy, n)</summary>
    static PathDef MakePath(int[][] wps)
    {
        var segs = new List<int[]>();
        for (int i = 0; i < wps.Length - 1; i++)
        {
            int[] a = wps[i], b = wps[i + 1];
            int dc = Math.Sign(b[0] - a[0]);
            int dr = Math.Sign(b[1] - a[1]);
            int n = 12 * Math.Max(Math.Abs(b[0] - a[0]), Math.Abs(b[1] - a[1]));
            segs.Add([dc, dr, n]);
        }
        var p = new PathDef { Segs = segs.ToArray() };
        // start low enough to be on the map, as the CPC does
        int x = wps[0][0] * 12, y = wps[0][1] * 12, seg = 0, rem = segs[0][2];
        while (y < 2)
        {
            x += segs[seg][0];
            y += segs[seg][1];
            rem--;
            if (rem == 0)
            {
                seg++;
                rem = segs[seg][2];
            }
        }
        p.X0 = x;
        p.Y0 = y;
        p.Seg0 = seg;
        p.Rem0 = rem;
        return p;
    }

    // ====================================================================== tick

    public void GameTick()
    {
        if (Bot)
            BotTick();
        WaveUpdate();
        if (GameOver != 0)
            return;
        UpdateEnemies();
        UpdateTowers();
        UpdateProjs();
        UpdateFx();
        if (BanTimer > 0 && --BanTimer == 0)
            Banner = [];
        if (MsgTimer > 0 && --MsgTimer == 0)
            Msg = "";
    }

    // ====================================================================== waves

    void WaveUpdate()
    {
        if (Hp <= 0)
        {
            EndStage(2);
            return;
        }
        if (!WaveActive)
        {
            if (CdSub > 0)
                CdSub--;
            else if (CdSec == 0)
                StartWave();
            else
            {
                CdSec--;
                CdSub = 24;
            }
            return;
        }
        if (spLeft == 0 && Enemies.Count == 0)
        {
            Funds += 15 + 3 * WaveNum;
            Audio.Play("sell");
            if (WaveNum >= Data.NUM_WAVES)
            {
                Score += Funds;
                EndStage(1);
                return;
            }
            WaveActive = false;
            CdSec = Data.NEXT_COUNTDOWN;
            CdSub = 25;
            Msg = Data.T("clear");
            MsgTimer = 150;
            FloatText(PF_W / 2, 60, $"+{15 + 3 * WaveNum}", Col.GOLD);
            return;
        }
        SpawnTick();
    }

    void StartWave()
    {
        if (WaveNum >= Data.NUM_WAVES)
            return;
        Banner = [];
        BanTimer = 0;
        WaveNum++;
        WaveActive = true;
        groups.Clear();
        foreach (var g in Def.Waves[WaveNum - 1])
            groups.Add((int[])g.Clone());
        spLeft = groups.Sum(g => g[1]);
        spTimer = 10;
        if (WaveNum == Data.NUM_WAVES)
        {
            Msg = Data.T("final");
            MsgTimer = 100;
        }
        else
            Msg = "";
        Audio.Play("wave");
    }

    public void CallWaveNow()
    {
        if (WaveActive || GameOver != 0)
            return;
        Funds += CdSec >> 1;
        Score += CdSec;
        if (CdSec > 1)
            FloatText(PF_W - 170, 40, $"+{CdSec >> 1}", Col.GOLD);
        StartWave();
    }

    void SpawnTick()
    {
        if (groups.Count == 0)
            return;
        if (--spTimer > 0)
            return;
        if (Enemies.Count >= Data.MAX_ENEMIES)
        {
            spTimer = 1;
            return;
        }
        var g = groups[0];
        SpawnEnemy(g[0]);
        spLeft--;
        g[1]--;
        spTimer = g[2];
        if (g[1] <= 0)
        {
            groups.RemoveAt(0);
            if (groups.Count > 0)
                spTimer = groups[0][2];
        }
    }

    void SpawnEnemy(int type)
    {
        var et = Data.ETYPES[type];
        var e = new Enemy { Type = type, Path = type == Data.DRONE ? Paths["air"] : Paths["ground"] };
        genCtr++;
        // with a second road, every other walker takes it
        if (type != Data.DRONE && Paths.TryGetValue("ground2", out var g2) && genCtr % 2 == 0)
            e.Path = g2;
        e.X = e.Path.X0;
        e.Y = e.Path.Y0;
        e.Px = e.X;
        e.Py = e.Y;
        e.Seg = e.Path.Seg0;
        e.Rem = e.Path.Rem0;
        e.Hp = Math.Min(255, et.Hp + et.Hpw * (WaveNum + Def.Toughness));
        e.MaxHp = e.Hp;
        e.Arm = et.Arm;
        e.Anim = rng.Next(256);
        e.Gen = genCtr;
        Enemies.Add(e);
    }

    // ====================================================================== enemies

    public static int Speed(Enemy e)
    {
        int c = Data.ETYPES[e.Type].Spd;
        return e.Slow > 0 ? c >> 1 : c;
    }

    void UpdateEnemies()
    {
        foreach (var e in Enemies.ToArray())
        {
            if (!e.Alive)
                continue;
            e.Px = e.X;
            e.Py = e.Y;
            int c = Speed(e);
            if (e.Slow > 0)
                e.Slow--;
            int t = e.Acc + c;
            e.Acc = t & 15;
            for (int i = 0; i < t >> 4; i++)
                if (EnemyStep(e))
                {
                    Arrive(e);
                    break;
                }
            if (!e.Alive)
                continue;
            e.Anim++;
            if (e.Type == Data.TANK && (e.Anim & 7) == 0)
                TankCrush(e);
        }
    }

    /// <summary>one path step; true when the enemy has reached the base</summary>
    static bool EnemyStep(Enemy e)
    {
        var segs = e.Path.Segs;
        var s = segs[e.Seg];
        e.X += s[0];
        e.Y += s[1];
        e.Prog++;
        if (--e.Rem <= 0)
        {
            e.Seg++;
            if (e.Seg >= segs.Length)
                return true;
            e.Rem = segs[e.Seg][2];
        }
        return false;
    }

    void Arrive(Enemy e)
    {
        RemoveEnemy(e);
        int dmg = Data.ETYPES[e.Type].Dmg;
        Hp = Math.Max(0, Hp - dmg);
        for (int k = 0; k < 5; k++)
            if (Hp <= 80 - 20 * k && SacksFull[k])
            {
                SacksFull[k] = false;
                var sc = Def.Sacks[k];
                AddFx("poof_b", sc[0] * 12 + 6, sc[1] * 12 + 6, 0.5f);
            }
        Shake = 1f;
        Audio.Play("hurt");
        FloatText((e.X + 6) * K, (e.Y + 6) * K - 30, $"-{dmg}", Col.BAD);
    }

    void RemoveEnemy(Enemy e)
    {
        e.Alive = false;
        Enemies.Remove(e);
    }

    void DamageEnemy(Enemy e, int d, bool ignoreArmour = false)
    {
        if (!e.Alive)
            return;
        if (!ignoreArmour)
            d = Math.Max(1, d - e.Arm);
        e.Hp -= d;
        e.Flash = 1f;
        if (e.Hp <= 0)
            KillEnemy(e);
    }

    void KillEnemy(Enemy e)
    {
        var et = Data.ETYPES[e.Type];
        Funds += et.Reward;
        Score = Math.Min(65535, Score + et.Dmg);
        RemoveEnemy(e);
        bool tank = e.Type == Data.TANK;
        AddFx(tank ? "boom" : "poof", e.X + 6, e.Y + 6, tank ? 0.5f : 0.4f);
        FloatText((e.X + 6) * K, (e.Y + 2) * K - 20, $"+{et.Reward}", Col.GOLD);
        Audio.Play("die");
    }

    static readonly (int, int)[] NEIGHBOURS = [(-1, 0), (1, 0), (0, -1), (0, 1)];

    void TankCrush(Enemy e)
    {
        int c = (e.X + 6) / 12, r = (e.Y + 6) / 12;
        foreach (var (dx, dy) in NEIGHBOURS)
        {
            int cc = c + dx, rr = r + dy;
            if (cc < 0 || rr < 0 || cc >= Data.COLS || rr >= Data.ROWS)
                continue;
            var tw = TowerAt[rr, cc];
            if (tw != null && tw.Type == Data.CAULDRON && tw.Dis == 0)
            {
                tw.Dis = 125;
                Audio.Play("crush");
                AddFx("bash", tw.Cx, tw.Cy - 6, 0.3f);
            }
        }
    }

    // ====================================================================== towers

    void UpdateTowers()
    {
        int budget = 4;
        foreach (var tw in Towers)
        {
            if (tw == null)
                continue;
            if (tw.Dis > 0)
            {
                tw.Dis--;
                continue;
            }
            if (tw.Cd > 0)
            {
                tw.Cd--;
                continue;
            }
            if (budget == 0)
                continue;
            budget--;
            var tg = FindTarget(tw);
            if (tg == null)
            {
                tw.Cd = 4;
                continue;
            }
            tw.Cd = Data.TTYPES[tw.Type].Cd;
            Fire(tw, tg);
        }
    }

    static bool InRange(Tower tw, Enemy e, int units)
    {
        int dx = e.X + 6 - tw.Cx, dy = e.Y + 6 - tw.Cy;
        return dx * dx + dy * dy <= units * units;
    }

    Enemy? FindTarget(Tower tw)
    {
        var tt = Data.TTYPES[tw.Type];
        Enemy? best = null;
        foreach (var e in Enemies)
        {
            if (e.Type == Data.DRONE && !tt.Air)
                continue;
            if (InRange(tw, e, tt.Range) && (best == null || e.Prog > best.Prog))
                best = e;
        }
        return best;
    }

    static int TowerDamage(Tower tw)
    {
        int b = Data.TTYPES[tw.Type].Dmg;
        return tw.Syn == 0 ? b : (b * tw.Syn + 8) / 16;
    }

    void Fire(Tower tw, Enemy tg)
    {
        var tt = Data.TTYPES[tw.Type];
        tw.Face = tg.X + 6 < tw.Cx ? -1 : 1;
        tw.Kick = 1f;
        Audio.Play(tt.Sfx);
        switch (tw.Type)
        {
            case Data.YIAYIA:
                foreach (var e in Enemies)
                    if (e.Type != Data.DRONE && InRange(tw, e, tt.Range))
                        e.Slow = 40;
                AddFx("bash", tg.X + 6, tg.Y + 2, 0.3f);
                DamageEnemy(tg, TowerDamage(tw));
                break;
            case Data.CAULDRON:
            {
                int clouds = 0;
                foreach (var e in Enemies.ToArray())
                    if (e.Type != Data.DRONE && InRange(tw, e, tt.Range))
                    {
                        e.Slow = 30;
                        if (clouds < 2)
                        {
                            AddFx("puff", e.X + 6, e.Y + 4, 0.5f);
                            clouds++;
                        }
                        DamageEnemy(e, 8, true);
                    }
                break;
            }
            case Data.LYRA:
            {
                // his tune reaches everyone around him, drones too; armour counts; drones slow down
                int notes = 0, d = TowerDamage(tw);
                foreach (var e in Enemies.ToArray())
                    if (InRange(tw, e, tt.Range))
                    {
                        if (e.Type == Data.DRONE)
                            e.Slow = 30;
                        if (notes < 2)
                        {
                            AddFx("notes", e.X + 6, e.Y - 2, 0.4f);
                            notes++;
                        }
                        DamageEnemy(e, d);
                    }
                break;
            }
            default:
            {
                var p = new Proj { Kind = tt.Proj, X = tw.Cx, Y = tw.Type == Data.PSSSST ? tw.Cy - 14 : tw.Cy - 6 };
                p.Px = p.X;
                p.Py = p.Y;
                p.SrcX = p.X;
                p.SrcY = p.Y;
                p.Target = tg;
                p.Gen = tg.Gen;
                p.Tx = tg.X + 6;
                p.Ty = tg.Y + 6;
                p.Dmg = TowerDamage(tw);
                p.Speed = p.Kind switch { "bullet" => 10f, "heart" => 11f, "laser" => 17f, _ => 5.5f };
                if (tw.Type == Data.BRIDE && rng.Next(256) < 64)
                {
                    p.Dmg *= 3;
                    p.Crit = true;
                }
                if (Projs.Count < 20)
                    Projs.Add(p);
                break;
            }
        }
    }

    void UpdateProjs()
    {
        foreach (var p in Projs.ToArray())
        {
            p.Px = p.X;
            p.Py = p.Y;
            if (!p.Lost)
            {
                if (p.Target.Alive && p.Target.Gen == p.Gen)
                {
                    p.Tx = p.Target.X + 6;
                    p.Ty = p.Target.Y + 6;
                }
                else
                    p.Lost = true;
            }
            float dx = p.Tx - p.X, dy = p.Ty - p.Y;
            float len = MathF.Sqrt(dx * dx + dy * dy);
            if (len > p.Speed)
            {
                p.X += dx / len * p.Speed;
                p.Y += dy / len * p.Speed;
                continue;
            }
            p.X = p.Tx;
            p.Y = p.Ty;
            Projs.Remove(p);
            if (p.Kind == "dakos")
            {
                AddFx("bash", p.X, p.Y - 4, 0.35f);
                foreach (var e in Enemies.ToArray())
                    if (Math.Abs(e.X + 6 - (int)p.X) <= 12 && Math.Abs(e.Y + 6 - (int)p.Y) <= 12)
                        DamageEnemy(e, p.Dmg);
                continue;
            }
            if (p.Lost)
                continue;
            if (p.Kind == "laser")
            {
                if (p.Target.Arm > 0)
                    p.Target.Arm--;
                AddFx("zap", p.X, p.Y, 0.25f);
            }
            else
                AddFx("spark", p.X, p.Y, 0.25f);
            DamageEnemy(p.Target, p.Dmg);
            if (p.Crit)
                FloatText(p.X * K, p.Y * K - 30, "x3!", Col.CRIT);
        }
    }

    // ====================================================================== effects

    void AddFx(string kind, float x, float y, float life)
    {
        if (Fxs.Count >= 40)
            return;
        Fxs.Add(new Fx { Kind = kind, X = x, Y = y, Life = life, Total = life });
    }

    /// <summary>a floating number, in pixels from the playfield's top left corner</summary>
    void FloatText(float px, float py, string s, uint col) =>
        Fxs.Add(new Fx { Kind = "text", X = px, Y = py, Life = 1f, Total = 1f, Text = s, Col = col });

    void UpdateFx()
    {
        Fxs.RemoveAll(f => (f.Life -= Data.TICK) <= 0);
    }

    // ====================================================================== build and sell

    /// <summary>"" when it can be built, or the text id of why not</summary>
    public string BuildCheck(int c, int r, int type)
    {
        if (!Has(type))
            return "locked";
        if (TowerAt[r, c] != null)
            return "cant_build";
        int ct = CellT[r, c];
        if (ct == 2 && type != Data.PSSSST)
            return "only_psssst";
        if (ct != 0 && !(ct == 2 && type == Data.PSSSST))
            return "cant_build";
        if (Funds < Data.TTYPES[type].Cost)
            return "cant_afford";
        if (Array.IndexOf(Towers, null) < 0)
            return "cant_build";
        return "";
    }

    public bool TryBuild(int c, int r)
    {
        if (GameOver != 0 || TowerAt[r, c] != null)
            return false;
        string why = BuildCheck(c, r, Selected);
        if (why != "")
        {
            Audio.Play("error");
            Msg = Data.T(why);
            MsgTimer = 50;
            return false;
        }
        var tt = Data.TTYPES[Selected];
        Funds -= tt.Cost;
        Score = Math.Min(65535, Score + tt.Dmg);
        var tw = new Tower { Type = Selected, C = c, R = r, Cx = c * 12 + 6, Cy = r * 12 + 6, OnBlocked = CellT[r, c] == 2 };
        Towers[Array.IndexOf(Towers, null)] = tw;
        TowerAt[r, c] = tw;
        SynUpdate();
        AddFx("poof", tw.Cx, tw.Cy, 0.35f);
        Audio.Play("build");
        if (BanTimer > 0)
            BanTimer = 1;
        return true;
    }

    public void TrySell(int c, int r)
    {
        var tw = TowerAt[r, c];
        if (tw == null || GameOver != 0)
            return;
        var tt = Data.TTYPES[tw.Type];
        Funds += tt.Sell;
        Score = Math.Max(0, Score - tt.Dmg);
        Towers[Array.IndexOf(Towers, tw)] = null;
        TowerAt[r, c] = null;
        SynUpdate();
        AddFx("poof", tw.Cx, tw.Cy, 0.35f);
        FloatText(tw.Cx * K, tw.Cy * K - 40, $"+{tt.Sell}", Col.GOLD);
        Audio.Play("sell");
    }

    // ====================================================================== neighbours

    public int SynFactorAt(int c, int r, int type)
    {
        foreach (var pair in Data.SYNERGY)
        {
            if (pair[0] != type)
                continue;
            foreach (var (dx, dy) in NEIGHBOURS)
            {
                int cc = c + dx, rr = r + dy;
                if (cc < 0 || rr < 0 || cc >= Data.COLS || rr >= Data.ROWS)
                    continue;
                var tw = TowerAt[rr, cc];
                if (tw != null && tw.Type == pair[1])
                    return pair[2];
            }
        }
        return 0;
    }

    public int SynCount() => Towers.Count(tw => tw != null && tw.Syn > 16);

    /// <summary>at most three bonuses: those that have one keep it, then the rest in order</summary>
    void SynUpdate()
    {
        int cnt = 0;
        var done = new HashSet<Tower>();
        for (int pass = 0; pass < 2; pass++)
            foreach (var tw in Towers)
            {
                if (tw == null || done.Contains(tw))
                    continue;
                if (pass == 0 && tw.Syn < 17)
                    continue;
                done.Add(tw);
                int f = SynFactorAt(tw.C, tw.R, tw.Type);
                if (f == 0)
                    tw.Syn = 0;
                else if (f < 16)
                    tw.Syn = f;
                else if (cnt < Data.SYN_MAX)
                {
                    cnt++;
                    tw.Syn = f;
                }
                else
                    tw.Syn = 0;
            }
    }

    // ====================================================================== end

    protected virtual void EndStage(int how)
    {
        GameOver = how;
        Projs.Clear();
        var lines = new List<string> { Data.StageText(StageNo, how == 1 ? 1 : 2) };
        if (how == 2)
            lines.Add(Lives > 0 ? Data.T("life_lost") : Data.T("gameover"));
        lines.Add(Data.T("press"));
        Banner = lines;
        BanTimer = -1;
        Audio.Play(how == 1 ? "wave" : "crush");
    }

    // ====================================================================== the bot

    void BotTick()
    {
        if (GameOver != 0)
            return;
        if (Plan.Count > 0 || botStep < 0)
        {
            PlanTick();
            return;
        }
        int want = BOT_PLAN[botStep % BOT_PLAN.Length];
        if (!Has(want))
            botStep++;
        else if (Funds >= Data.TTYPES[want].Cost)
        {
            int bestCover = -1, bc = 0, br = 0, rr = Data.TTYPES[want].Range;
            for (int r = 0; r < Data.ROWS; r++)
                for (int c = 0; c < Data.COLS; c++)
                {
                    if (BuildCheck(c, r, want) != "")
                        continue;
                    int cover = 0;
                    for (int r2 = Math.Max(0, r - 4); r2 < Math.Min(Data.ROWS, r + 5); r2++)
                        for (int c2 = Math.Max(0, c - 4); c2 < Math.Min(Data.COLS, c + 5); c2++)
                            if (CellT[r2, c2] == 1)
                            {
                                int dx = (c2 - c) * 12, dy = (r2 - r) * 12;
                                if (dx * dx + dy * dy <= rr * rr)
                                    cover++;
                            }
                    if (cover > bestCover)
                        (bestCover, bc, br) = (cover, c, r);
                }
            if (bestCover > 0)
            {
                int keep = Selected;
                Selected = want;
                TryBuild(bc, br);
                Selected = keep;
            }
            botStep++;
        }
        if (!WaveActive && CdSec < (WaveNum == 0 ? 40 : 12))
            CallWaveNow();
    }

    /// <summary>the CPC's way of testing a stage (VrouvesCPC/tools/scenario.py): build the
    /// plan in order as the vrouves come in, and call the next wave as soon as the next one
    /// cannot be afforded</summary>
    void PlanTick()
    {
        botStep = -1;
        while (Plan.Count > 0)
        {
            int t = Plan[0][0];
            if (!Has(t))
                foreach (int u in PLAN_SUBSTITUTE[t])
                    if (Has(u))
                    {
                        t = u;
                        break;
                    }
            if (Funds < Data.TTYPES[t].Cost)
                break;
            int keep = Selected;
            Selected = t;
            TryBuild(Plan[0][1], Plan[0][2]);
            Selected = keep;
            Plan.RemoveAt(0);
        }
        if (!WaveActive && (Plan.Count == 0 || Funds < Data.TTYPES[Plan[0][0]].Cost))
            CallWaveNow();
    }
}

using System.Text.Json;
using Vrouves;

// Plays every stage (or those named) headless and prints how each wave goes, as
// VrouvesWEB/tests/autoplay.gd does. With "plan", it plays the CPC's build plans
// (Vrouves/Assets/data/plans.json, from VrouvesCPC/tools/scenario.py) instead of the greedy order.
string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
using (var f = File.OpenRead(Path.Combine(root, "Vrouves/Assets/data/stages.json")))
    Data.LoadStages(f);
bool usePlan = args.Contains("plan");
var which = args.Where(a => int.TryParse(a, out _)).Select(int.Parse).ToList();
if (which.Count == 0)
    which = Enumerable.Range(1, Data.Stages.Count).ToList();
using (var f = File.OpenRead(Path.Combine(root, "Vrouves/Assets/data/plans.json")))
    Data.LoadPlans(f);
var plans = Data.Plans;
bool allOk = true;
foreach (int n in which)
    allOk &= usePlan ? PlayPlan(n) : PlayGreedy(n);
Console.WriteLine("RESULT " + (allOk ? "ALL WON" : "SOME LOST"));
return allOk ? 0 : 1;

bool Report(Stage g, int n, int ticks)
{
    bool won = g.GameOver == 1;
    Console.WriteLine($"STAGE {n} {(won ? "WON" : "LOST")}  hp {g.Hp} score {g.Score} after {ticks / 25} s");
    return won;
}

void WaveLine(Stage g, int n) =>
    Console.WriteLine($"  stage {n} wave {g.WaveNum}  hp {g.Hp,3} funds {g.Funds,4} score {g.Score,5} towers {g.Towers.Count(t => t != null)}");

bool PlayPlan(int n)
{
    var g = new Stage { Bot = true };
    g.Setup(n, 0, 3);
    foreach (var step in plans[n - 1])
        g.Plan.Add(step);
    int ticks = 0, last = 0;
    while (g.GameOver == 0 && ticks < 25 * 60 * 40)
    {
        g.GameTick();
        ticks++;
        if (g.WaveNum != last)
        {
            last = g.WaveNum;
            WaveLine(g, n);
        }
    }
    return Report(g, n, ticks);
}

// buildable cells, best first: how many road cells each would cover
List<(int cover, int c, int r)> Spots(Stage g, int type)
{
    var outp = new List<(int, int, int)>();
    int rr = Data.TTYPES[type].Range;
    for (int r = 0; r < Data.ROWS; r++)
        for (int c = 0; c < Data.COLS; c++)
        {
            string why = g.BuildCheck(c, r, type);
            if (why != "" && why != "cant_afford")
                continue;
            int cover = 0;
            for (int r2 = 0; r2 < Data.ROWS; r2++)
                for (int c2 = 0; c2 < Data.COLS; c2++)
                    if (g.CellT[r2, c2] == 1)
                    {
                        int dx = (c2 - c) * 12, dy = (r2 - r) * 12;
                        if (dx * dx + dy * dy <= rr * rr)
                            cover++;
                    }
            outp.Add((cover, c, r));
        }
    return outp.OrderByDescending(s => s.Item1).ToList();
}

bool PlayGreedy(int n)
{
    var g = new Stage();
    g.Setup(n, 0, 3);
    int[] plan = [Data.CRETAN, Data.CRETAN, Data.PSSSST, Data.CRETAN, Data.BRIDE, Data.YIAYIA, Data.BAKER,
        Data.CAULDRON, Data.CRETAN, Data.LYRA, Data.BRIDE, Data.PSSSST];
    int step = 0, ticks = 0, last = 0;
    while (g.GameOver == 0 && ticks < 25 * 60 * 40)
    {
        int want = plan[step % plan.Length];
        if (!g.Has(want))
        {
            step++;
            continue;
        }
        if (g.Funds >= Data.TTYPES[want].Cost)
        {
            var s = Spots(g, want);
            if (s.Count > 0)
            {
                g.Selected = want;
                g.TryBuild(s[0].c, s[0].r);
            }
            step++;
        }
        if (!g.WaveActive && g.WaveNum > 0 && g.CdSec < 13)
            g.CallWaveNow();
        if (g.WaveNum == 0 && g.CdSec < 40)
            g.CallWaveNow();
        g.GameTick();
        ticks++;
        if (g.WaveNum != last)
        {
            last = g.WaveNum;
            WaveLine(g, n);
        }
    }
    return Report(g, n, ticks);
}

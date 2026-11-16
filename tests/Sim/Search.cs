using Vrouves;

/// <summary>Looks for a build plan that wins a stage: random greedy plans first (each
/// defender on one of the best few free cells for it), then small changes to the best one
/// (a defender moved, changed or two swapped), keeping a change when the stage goes better.
/// Every candidate is played to the end by Stage's plan bot, as the CPC's tools/scenario.py
/// plans are.</summary>
static class Search
{
    /// <summary>how often a random plan picks each defender (a "wN=x" argument changes defender N's)</summary>
    public static readonly Dictionary<int, double> WEIGHT = new()
    {
        [Data.CRETAN] = 3, [Data.YIAYIA] = 1.5, [Data.BRIDE] = 2, [Data.PSSSST] = 2, [Data.CAULDRON] = 1,
        [Data.BAKER] = 1.5, [Data.LYRA] = 1, [Data.SHEPHERD] = 2, [Data.PRIEST] = 1,
    };
    const int LEN = 34;

    public sealed record Result(int[][] Plan, bool Won, int Hp, int Wave, int Score)
    {
        public long Fitness => (Won ? 1_000_000L : 0) + Wave * 10_000L + Hp * 100L + Score / 10;
    }

    public static Result Play(int n, int[][] plan)
    {
        var g = new Stage { Bot = true };
        g.Setup(n, 0, 3);
        foreach (var s in plan)
            g.Plan.Add(s);
        int ticks = 0;
        while (g.GameOver == 0 && ticks < 25 * 60 * 40)
        {
            g.GameTick();
            ticks++;
        }
        return new Result(plan, g.GameOver == 1, g.Hp, g.WaveNum, g.Score);
    }

    /// <summary>the road cells every invader walks, and the flight; per cell how many path steps pass it</summary>
    static (List<(int c, int r)> ground, List<(int c, int r)> air) Roads(StageDef d)
    {
        static List<(int, int)> Cells(int[][] wps)
        {
            var o = new List<(int, int)>();
            for (int i = 0; i + 1 < wps.Length; i++)
            {
                int dc = Math.Sign(wps[i + 1][0] - wps[i][0]), dr = Math.Sign(wps[i + 1][1] - wps[i][1]);
                int k = Math.Max(Math.Abs(wps[i + 1][0] - wps[i][0]), Math.Abs(wps[i + 1][1] - wps[i][1]));
                for (int j = 0; j < k; j++)
                    o.Add((wps[i][0] + dc * j, wps[i][1] + dr * j));
            }
            return o;
        }
        var g = Cells(d.Ground);
        if (d.Ground2.Length > 0)
            g.AddRange(Cells(d.Ground2));
        return (g, Cells(d.Air));
    }

    static double Cover(int type, int c, int r, List<(int c, int r)> ground, List<(int c, int r)> air)
    {
        var tt = Data.TTYPES[type];
        int rr = tt.Range * tt.Range;
        double v = 0;
        foreach (var (gc, gr) in ground)
        {
            int dx = (gc - c) * 12, dy = (gr - r) * 12;
            if (dx * dx + dy * dy <= rr)
                v++;
        }
        if (tt.Air)
            foreach (var (ac, ar) in air)
            {
                int dx = (ac - c) * 12, dy = (ar - r) * 12;
                if (dx * dx + dy * dy <= rr)
                    v += 0.6;
            }
        return v;
    }

    static readonly (int, int)[] NB = [(-1, 0), (1, 0), (0, -1), (0, 1)];

    /// <summary>a random greedy plan</summary>
    static int[][] RandomPlan(StageDef d, int[] have, Random rng, List<(int c, int r)> ground, List<(int c, int r)> air)
    {
        var taken = new Dictionary<(int, int), int>();
        var plan = new List<int[]>();
        have = have.Where(t => WEIGHT[t] > 0).ToArray();
        double total = have.Sum(t => WEIGHT[t]);
        for (int k = 0; k < LEN; k++)
        {
            double x = rng.NextDouble() * total;
            int type = have[^1];
            foreach (int t in have)
                if ((x -= WEIGHT[t]) < 0)
                {
                    type = t;
                    break;
                }
            if (k < 2)
                type = have.Contains(Data.CRETAN) ? Data.CRETAN : have[0];
            var spots = new List<(double v, int c, int r)>();
            for (int r = 0; r < Data.ROWS; r++)
                for (int c = 0; c < Data.COLS; c++)
                {
                    char ch = d.Grid[r][c];
                    bool free = ch is '.' or 'F' || (type == Data.PSSSST && ch is not ('#' or 'K' or 'P'));
                    if (!free || taken.ContainsKey((c, r)))
                        continue;
                    double v;
                    if (type == Data.PRIEST)
                    {
                        v = 0;
                        foreach (var (dx, dy) in NB)
                            if (taken.TryGetValue((c + dx, r + dy), out int nt) && nt != Data.PRIEST)
                                v += Data.TTYPES[nt].Cost / 10.0;
                    }
                    else
                    {
                        v = Cover(type, c, r, ground, air);
                        foreach (var pair in Data.SYNERGY)
                            if (pair[0] == type)
                                foreach (var (dx, dy) in NB)
                                    if (taken.TryGetValue((c + dx, r + dy), out int nt) && nt == pair[1])
                                        v *= pair[2] / 16.0;
                        foreach (var (dx, dy) in NB)
                            if (taken.TryGetValue((c + dx, r + dy), out int nt) && nt == Data.PRIEST)
                                v *= 1.3;
                    }
                    spots.Add((v * (0.8 + 0.4 * rng.NextDouble()), c, r));
                }
            if (spots.Count == 0)
                break;
            var best = spots.OrderByDescending(s => s.v).First();
            if (best.v <= 0)
                continue;
            taken[(best.c, best.r)] = type;
            plan.Add([type, best.c, best.r]);
        }
        return plan.ToArray();
    }

    static int[][] Mutate(int[][] plan, StageDef d, int[] have, Random rng)
    {
        var p = plan.Select(s => (int[])s.Clone()).ToList();
        int i = rng.Next(p.Count);
        switch (rng.Next(4))
        {
            case 0:     // swap two steps
            {
                int j = rng.Next(p.Count);
                (p[i], p[j]) = (p[j], p[i]);
                break;
            }
            case 1:     // another defender on the same cell
            {
                int t = have[rng.Next(have.Length)];
                char ch = d.Grid[p[i][2]][p[i][1]];
                if (t != Data.PSSSST && ch is not ('.' or 'F'))
                    return p.ToArray();
                p[i][0] = t;
                break;
            }
            case 2:     // move it to a free cell nearby
            {
                int c = Math.Clamp(p[i][1] + rng.Next(-2, 3), 0, Data.COLS - 1), r = Math.Clamp(p[i][2] + rng.Next(-2, 3), 0, Data.ROWS - 1);
                char ch = d.Grid[r][c];
                bool free = ch is '.' or 'F' || (p[i][0] == Data.PSSSST && ch is not ('#' or 'K' or 'P'));
                if (free && !p.Any(s => s[1] == c && s[2] == r))
                {
                    p[i][1] = c;
                    p[i][2] = r;
                }
                break;
            }
            default:    // drop it
                if (p.Count > 8)
                    p.RemoveAt(i);
                break;
        }
        return p.ToArray();
    }

    public static int[][] Run(int n, int tries)
    {
        var d = Data.Stages[n - 1];
        var have = Data.TOWER_ORDER.Where(d.Defenders.Contains).ToArray();
        var (ground, air) = Roads(d);
        int seeds = tries / 2;
        var results = new Result[seeds];
        Parallel.For(0, seeds, i =>
        {
            var rng = new Random(n * 100_003 + i);
            results[i] = Play(n, RandomPlan(d, have, rng, ground, air));
        });
        var best = results.MaxBy(r => r.Fitness)!;
        Console.WriteLine($"stage {n}: best random plan {(best.Won ? "WON" : "LOST")} wave {best.Wave} hp {best.Hp}, " +
                          $"{results.Count(r => r.Won)} of {seeds} won");
        // then improve the best: a round of mutations at a time, in parallel
        int rounds = (tries - seeds) / 16;
        for (int k = 0; k < rounds; k++)
        {
            var cand = new Result[16];
            var b = best;
            Parallel.For(0, 16, i =>
            {
                var rng = new Random(n * 7919 + k * 16 + i);
                var p = Mutate(b.Plan, d, have, rng);
                if (rng.Next(2) == 0)
                    p = Mutate(p, d, have, rng);
                cand[i] = Play(n, p);
            });
            var top = cand.MaxBy(r => r.Fitness)!;
            if (top.Fitness > best.Fitness)
                best = top;
        }
        Console.WriteLine($"stage {n}: best plan {(best.Won ? "WON" : "LOST")} wave {best.Wave} hp {best.Hp} score {best.Score}, {best.Plan.Length} defenders");
        return best.Plan;
    }
}

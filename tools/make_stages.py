#!/usr/bin/env python3
"""Puts the ten stages of the Android version between the CPC's olive press (9) and the
Bride's wedding (now 20) in Vrouves/Assets/data/stages.json:

    tools/make_stages.py

The CPC's ten stages (VrouvesPC/data/stages.json, through tools/import_assets.sh) are kept
as they are; the new ones are defined here: the map, the roads and the drones' flight as
waypoints (col, row), the five sacks, the waves and which defenders there are. Every road
and flight is checked: straight or 45 degree legs, roads only on '#' cells, every '#' cell
on a road.

Map cells, as in the CPC's: '.' free, '#' road, K the building, P its yard (the sacks
stand there); everything else blocks building but Psssst's: T olive or orange tree,
Y cypress, C prickly pear, G goat, S sheep, R rock, W stone wall, J jar, and the new ones,
~ water, H beehive, V vine, B hay bale, M windmill.
"""
import json
import os
import sys
import unicodedata

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PATH = os.path.join(ROOT, "Vrouves", "Assets", "data", "stages.json")

ALL8 = [1, 2, 3, 4, 5, 6, 7, 8]
ALL9 = ALL8 + [9]


def waves(k, gap=1.0):
    """the wedding's ten waves, every count times k and every gap times gap"""
    base = [
        [[2, 8, 36]],
        [[1, 15, 18], [2, 5, 28]],
        [[2, 12, 24], [1, 11, 16]],
        [[2, 12, 20], [3, 5, 48]],
        [[1, 20, 12], [2, 11, 18]],
        [[2, 16, 15], [3, 6, 38]],
        [[1, 22, 10], [2, 15, 14], [3, 5, 32]],
        [[3, 10, 28], [2, 17, 12], [1, 18, 9]],
        [[1, 26, 8], [2, 18, 11], [3, 10, 24]],
        [[2, 22, 9], [1, 24, 7], [3, 22, 14]],
    ]
    return [[[t, max(1, round(n * k)), max(5, round(g * gap))] for t, n, g in w] for w in base]


NEW = [
    dict(base="dairy", toughness=2, defenders=ALL8, waves=waves(0.85, 1.1),
         grid=["KKKKKP..T...S....T.T",
               "KKKKKP.....R.....Y..",
               "KKKKKP.##########...",
               "KKKKKP.#..S.....#..T",
               "PPPPPP##..WWWW..#.S.",
               "T.T.............#..T",
               "....R..T...S....#...",
               "T......WWWW.....#..T",
               "..S.............#.R.",
               "T..##############..T",
               "...#.....T....S.....",
               "T..#..WWWWW.....R..T",
               "...#################",
               "T.S..T...R...T..S..T"],
         ground=[[19, 12], [3, 12], [3, 9], [16, 9], [16, 2], [7, 2], [7, 4], [6, 4]],
         ground2=[], air=[[19, 6], [14, 11], [9, 11], [6, 8], [6, 4]],
         sacks=[[5, 1], [5, 3], [1, 4], [3, 4], [5, 4]]),
    dict(base="watermill", toughness=2, defenders=ALL8, waves=waves(0.9, 1.05),
         grid=["T..Y..T...~...T...YT",
               "..G.......~..#######",
               "########..~..#..S..T",
               "T......#..~..#.R....",
               "..T....#..~~.######.",
               "T.S....#..~~..T...#Y",
               "..#################T",
               "T.#..T.......S.....T",
               "..#.R.....J....S...T",
               "T.#..S.....T..PKKKKK",
               "..##########..PKKKKK",
               "T.R.......T#..PKKKKK",
               "Y..S.......###PKKKKK",
               "T..T..Y..T....PPPPPP"],
         ground=[[0, 2], [7, 2], [7, 6], [2, 6], [2, 10], [11, 10], [11, 12], [13, 12]],
         ground2=[[19, 1], [13, 1], [13, 4], [18, 4], [18, 6], [2, 6], [2, 10], [11, 10], [11, 12], [13, 12]],
         air=[[0, 0], [7, 7], [7, 12], [13, 12]],
         sacks=[[14, 10], [14, 11], [14, 13], [16, 13], [18, 13]]),
    dict(base="cave", toughness=3, defenders=ALL8, waves=waves(0.95, 1.0),
         grid=["R.RR..RKKKKKKRR.R..R",
               "..R...RKKKKKKR..R.T.",
               "T...R.RKKKKKKR.R....",
               "..R....PPPPPP#...R..",
               "R............####..T",
               "..R..R..........#.R.",
               "T..##############..R",
               "..R#..R....R.......R",
               "R..#.......R....R...",
               "...########..R....R.",
               "R.R.......#..R.....T",
               "####################",
               "..R..T....R....T..R.",
               "R..T..R..R.R..T..R.R"],
         ground=[[19, 11], [10, 11], [10, 9], [3, 9], [3, 6], [16, 6], [16, 4], [13, 4], [13, 3]],
         ground2=[[0, 11], [10, 11], [10, 9], [3, 9], [3, 6], [16, 6], [16, 4], [13, 4], [13, 3]],
         air=[[0, 13], [4, 9], [15, 9], [10, 4]],
         sacks=[[7, 3], [8, 3], [10, 3], [11, 3], [12, 3]]),
    dict(base="monastery", toughness=0, defenders=ALL9, waves=waves(0.95, 1.0),
         grid=["T..Y..T..Y..T..Y..T.",
               ".G.....T.....R....Y.",
               "T...S.....Y....C...T",
               "Y..###############.T",
               "...#.............#..",
               "T..#...KKKKK.....#.T",
               "Y..#...KKKKK..R..#.Y",
               "T..#...KKKKK.....#.T",
               "...#..PPPPPPP#...#..",
               "T..#.T.......#.G.#.T",
               "...###########...#..",
               "T.Y.....T....S...#.T",
               "##################..",
               "T..T..Y..T..Y..T..YT"],
         ground=[[0, 12], [17, 12], [17, 3], [3, 3], [3, 10], [13, 10], [13, 8]],
         ground2=[], air=[[19, 13], [15, 9], [15, 4], [13, 6], [13, 8]],
         sacks=[[6, 8], [7, 8], [9, 8], [11, 8], [12, 8]]),
    dict(base="apiary", toughness=3, defenders=ALL9, waves=waves(1.0, 1.0),
         grid=["T#..T.H.Y..H.T..H..T",
               ".#..####..####.H..Y.",
               "T#.H#..#H.#..#..H..T",
               ".#..#.H#..#H.#.....H",
               "H#..#..#..#..#.H..T.",
               ".#.H#..#.H#..#PKKKKK",
               "T#..#.H#..#..#PKKKKK",
               ".#..#..#..#H..PKKKKK",
               "H#..#.H#..#...PKKKKK",
               ".#.H#..#.H#..HPPPPPP",
               "T#..#..#..#..T....H.",
               ".#..#.H#..#.H...T...",
               ".####..####..H..H..T",
               "T..Y..T..Y..T..Y..YT"],
         ground=[[1, 0], [1, 12], [4, 12], [4, 1], [7, 1], [7, 12], [10, 12], [10, 1], [13, 1], [13, 6]],
         ground2=[], air=[[0, 13], [6, 7], [11, 2], [17, 2], [13, 6]],
         sacks=[[14, 5], [14, 7], [14, 9], [16, 9], [18, 9]]),
    dict(base="distillery", toughness=2, defenders=ALL9, waves=waves(1.0, 0.95),
         grid=["T..Y...T..J...T..Y.T",
               "###################.",
               "T.....G......J....#T",
               "..J..T....T......T#.",
               "T.#################T",
               "..#..J...T....J.....",
               "T.#.....G.....T...JT",
               "..###############...",
               "T.J....T......J.#..T",
               "..T...J.........#.J.",
               "T......PPPPPPP###..T",
               "..T....PKKKKKP..J...",
               "T..J...PKKKKKP....YT",
               "Y..T...PKKKKKP..T..Y"],
         ground=[[0, 1], [18, 1], [18, 4], [2, 4], [2, 7], [16, 7], [16, 10], [14, 10]],
         ground2=[], air=[[0, 0], [0, 3], [7, 10], [14, 10]],
         sacks=[[7, 11], [7, 13], [13, 11], [13, 13], [13, 10]]),
    dict(base="windmills", toughness=3, defenders=ALL9, waves=waves(1.05, 0.95),
         grid=["T..M.....T..M.PKKKKK",
               ".############.PKKKKK",
               "T#..R..M....#.PKKKKK",
               ".#.T......R.##PKKKKK",
               "M####...M.....PPPPPP",
               "...T#..R...T.....M..",
               "T...####..M...R....T",
               "..M....#..T.....M...",
               "T..R...####...R....T",
               "###..M....#..M....R.",
               "..#.......####..T...",
               "T.#..T..M....#..M..T",
               "..############...R..",
               "T..M..T..R..T..M..T."],
         ground=[[0, 9], [2, 9], [2, 12], [13, 12], [13, 10], [10, 10], [10, 8], [7, 8], [7, 6], [4, 6],
                 [4, 4], [1, 4], [1, 1], [12, 1], [12, 3], [13, 3]],
         ground2=[], air=[[12, 13], [4, 5], [6, 3], [13, 3]],
         sacks=[[14, 0], [14, 2], [15, 4], [17, 4], [19, 4]]),
    dict(base="threshing", toughness=0, defenders=ALL9, waves=waves(1.0, 0.95),
         grid=["T..B..T..YY..T..B..T",
               "###################.",
               "T.B..T...G...B....#.",
               ".################.#.",
               ".#.B..T..YY..T..#.#T",
               ".#.####.KKKK..B.#.#.",
               "T#.#..#.KKKK....#.#T",
               ".#.#.B#.PKKP.G..#.#.",
               "T#.#..##PPPP....#.#T",
               ".#.#.G...BB..T..#.#.",
               "Y#.##############.#Y",
               ".#..B..T..YY..T..B#.",
               "T##################T",
               "Y..T..B..T..T..B..TY"],
         ground=[[0, 1], [18, 1], [18, 12], [1, 12], [1, 3], [16, 3], [16, 10], [3, 10], [3, 5], [6, 5],
                 [6, 8], [7, 8]],
         ground2=[], air=[[19, 13], [14, 8], [7, 8]],
         sacks=[[8, 7], [11, 7], [8, 8], [10, 8], [11, 8]]),
    dict(base="vineyard", toughness=1, defenders=ALL9, waves=waves(1.05, 0.9),
         grid=["T..Y..T..T..Y..T..YT",
               "....T..#############",
               "Y.....T#.VVV.VVV.T..",
               "..T....###########..",
               "PPPPP.T..VVV.VVV.#.Y",
               "KKKKP#############..",
               "KKKKP.....T....J...T",
               "KKKKP..T.....T......",
               "KKKKP#############..",
               "PPPPP.Y..VVV.VVV.#.T",
               "..T....###########..",
               "Y.....T#.VVV.VVV.T..",
               "....T..#############",
               "T..Y..T..T..Y..T..YT"],
         ground=[[19, 1], [7, 1], [7, 3], [17, 3], [17, 5], [5, 5]],
         ground2=[[19, 12], [7, 12], [7, 10], [17, 10], [17, 8], [5, 8]],
         air=[[19, 13], [13, 7], [5, 7]],
         sacks=[[4, 5], [4, 6], [4, 8], [2, 4], [2, 9]]),
    dict(base="summit", toughness=3, defenders=ALL9, waves=waves(1.1, 0.9),
         grid=["R.R.R..RKKKKR..R.R.R",
               "..R..R..KKKK..R..R..",
               "R....R.RKKKKR.R....R",
               "..R....PPPPPP..R..R.",
               "R.########...R.....R",
               "..#..R....R....R..G.",
               "R.################.R",
               "..R..R...R...R..R#..",
               "R.################.R",
               "..#.R...R....R..R...",
               "R.################.R",
               "..R..R...R..R...R#..",
               "##################.R",
               "R..R..R..R..R..R..RR"],
         ground=[[0, 12], [17, 12], [17, 10], [2, 10], [2, 8], [17, 8], [17, 6], [2, 6], [2, 4], [9, 4]],
         ground2=[], air=[[18, 13], [9, 4]],
         sacks=[[7, 3], [8, 3], [10, 3], [11, 3], [12, 3]]),
]
NEW_BASES = {s["base"] for s in NEW}

# the banners (Data.cs has the same in STAGE_TEXT): name, won, lost, in English and Greek
STRINGS = {
    "olive_press": [["Stage 09: The olive press", "Πίστα 09: Το λιοτρίβι"],
                    ["Olive press saved! Now, to the cheese dairy!", "Το λιοτρίβι σώθηκε! Στο τυροκομείο!"],
                    ["The olive press has fallen!", "Το λιοτρίβι έπεσε!"]],
    "dairy": [["Stage 10: The cheese dairy", "Πίστα 10: Το τυροκομείο"],
              ["Cheese dairy saved! Now, to the watermill!", "Το τυροκομείο σώθηκε! Στον νερόμυλο!"],
              ["The cheese dairy has fallen!", "Το τυροκομείο έπεσε!"]],
    "watermill": [["Stage 11: The watermill", "Πίστα 11: Ο νερόμυλος"],
                  ["Watermill saved! Now, to the Sfendoni cave!", "Ο νερόμυλος σώθηκε! Στο σπήλαιο Σφενδόνη!"],
                  ["The watermill has fallen!", "Ο νερόμυλος έπεσε!"]],
    "cave": [["Stage 12: The Sfendoni cave", "Πίστα 12: Το σπήλαιο Σφενδόνη"],
             ["Cave saved! Now, to the monastery!", "Το σπήλαιο σώθηκε! Στο μοναστήρι!"],
             ["The cave has fallen!", "Το σπήλαιο έπεσε!"]],
    "monastery": [["Stage 13: The monastery", "Πίστα 13: Το μοναστήρι"],
                  ["Monastery saved! Now, to the beehives!", "Το μοναστήρι σώθηκε! Στα μελίσσια!"],
                  ["The monastery has fallen!", "Το μοναστήρι έπεσε!"]],
    "apiary": [["Stage 14: The beehives", "Πίστα 14: Τα μελίσσια"],
               ["Beehives saved! Now, to the raki still!", "Τα μελίσσια σώθηκαν! Στο ρακοκάζανο!"],
               ["The beehives have fallen!", "Τα μελίσσια χάθηκαν!"]],
    "distillery": [["Stage 15: The raki still", "Πίστα 15: Το ρακοκάζανο"],
                   ["Raki still saved! Now, to the windmills!", "Το ρακοκάζανο σώθηκε! Στους ανεμόμυλους!"],
                   ["The raki still has fallen!", "Το ρακοκάζανο έπεσε!"]],
    "windmills": [["Stage 16: The windmills", "Πίστα 16: Οι ανεμόμυλοι"],
                  ["Windmills saved! Now, to the threshing floor!", "Οι ανεμόμυλοι σώθηκαν! Στο αλώνι!"],
                  ["The windmills have fallen!", "Οι ανεμόμυλοι έπεσαν!"]],
    "threshing": [["Stage 17: The threshing floor", "Πίστα 17: Το αλώνι"],
                  ["Threshing floor saved! Now, to the vineyard!", "Το αλώνι σώθηκε! Στο αμπέλι!"],
                  ["The threshing floor has fallen!", "Το αλώνι έπεσε!"]],
    "vineyard": [["Stage 18: The vineyard", "Πίστα 18: Το αμπέλι"],
                 ["Vineyard saved! Now, up Psiloritis!", "Το αμπέλι σώθηκε! Στον Ψηλορείτη!"],
                 ["The vineyard has fallen!", "Το αμπέλι χάθηκε!"]],
    "summit": [["Stage 19: The summit chapel", "Πίστα 19: Το ξωκλήσι της κορυφής"],
               ["Summit saved! Now, to the wedding!", "Η κορυφή σώθηκε! Στο γάμο!"],
               ["The summit chapel has fallen!", "Το ξωκλήσι έπεσε!"]],
    "wedding": [["Stage 20: The Bride's wedding", "Πίστα 20: Ο γάμος της Νύφης"],
                ["Wedding saved! Long live the couple!", "Ο γάμος σώθηκε! Ζήτω το ζευγάρι!"],
                ["The wedding has fallen!", "Ο γάμος χάλασε!"]],
}


def caps(t):
    """capitals without accents, as Greek is written in capitals"""
    d = unicodedata.normalize("NFD", t.upper())
    return unicodedata.normalize("NFC", "".join(c for c in d if not unicodedata.combining(c)))


def cells(wps):
    out = []
    for (a, b) in zip(wps, wps[1:]):
        dc, dr = b[0] - a[0], b[1] - a[1]
        assert dc == 0 or dr == 0 or abs(dc) == abs(dr), ("not straight or 45 degrees", a, b)
        n = max(abs(dc), abs(dr))
        sc, sr = (dc > 0) - (dc < 0), (dr > 0) - (dr < 0)
        out += [(a[0] + sc * i, a[1] + sr * i) for i in range(n)]
    return out + [tuple(wps[-1])]


def check(no, s):
    g = s["grid"]
    assert len(g) == 14 and all(len(r) == 20 for r in g), (no, [len(r) for r in g])
    roads = set()
    for key in ("ground", "ground2"):
        if s[key]:
            cs = cells(s[key])
            for (c, r) in cs:
                assert g[r][c] == "#", (no, key, "not a road", c, r)
            assert len(set(cs)) == len(cs), (no, key, "crosses itself")
            c, r = cs[-1]
            near = [g[rr][cc] for cc, rr in ((c - 1, r), (c + 1, r), (c, r - 1), (c, r + 1))
                    if 0 <= cc < 20 and 0 <= rr < 14]
            assert "P" in near or "K" in near, (no, key, "does not end at the building")
            roads |= set(cs)
    every = {(c, r) for r in range(14) for c in range(20) if g[r][c] == "#"}
    assert every == roads, (no, "road cells off the roads", sorted(every - roads))
    cells(s["air"])
    for (c, r) in s["sacks"]:
        assert g[r][c] == "P", (no, "sack not in the yard", c, r)
    assert len(s["sacks"]) == 5 and len(s["waves"]) == 10


def main():
    # tuning: tools/make_stages.py '{"dairy": [k, gap, toughness]}' sets those stages' waves(k, gap)
    if len(sys.argv) > 1:
        for base, kg in json.loads(sys.argv[1]).items():
            st = next(s for s in NEW if s["base"] == base)
            st["waves"] = waves(kg[0], kg[1])
            if len(kg) > 2:
                st["toughness"] = kg[2]
    with open(PATH, encoding="utf-8") as f:
        data = json.load(f)
    old = [s for s in data["stages"] if s["base"] not in NEW_BASES]
    assert len(old) == 10 and old[8]["base"] == "olive_press" and old[9]["base"] == "wedding"
    stages = old[:9] + [dict(id=0, **s) for s in NEW] + old[9:]
    # the wedding: the Priest is at the altar with the Bride, the Shepherd comes to help
    stages[-1]["defenders"] = [1, 2, 4, 5, 6, 7, 8]
    for i, s in enumerate(stages):
        s["id"] = i + 1
        check(i + 1, s)
        # as the CPC's export has them: the map without its roads, the banners in capitals
        s.setdefault("map", [r.replace("#", ".") for r in s["grid"]])
        s["strings"] = {k: [caps(t) for t in STRINGS[s["base"]][j]] for j, k in enumerate(("B_STAGE", "B_WIN", "B_LOSE"))} \
            if s["base"] in STRINGS else s["strings"]
    keys = ["id", "base", "map", "grid", "ground", "ground2", "air", "sacks", "waves", "toughness", "defenders", "strings"]
    data["stages"] = [{k: s[k] for k in keys} for s in stages]
    with open(PATH, "w", encoding="utf-8") as f:
        f.write(json.dumps(data, indent=1, ensure_ascii=False))
    for s in stages:
        n = sum(len(cells(s[k])) for k in ("ground", "ground2") if s[k])
        print("%2d %-12s road %3d  toughness %d  invaders %d" % (s["id"], s["base"], n, s["toughness"],
                                                                 sum(g[1] for w in s["waves"] for g in w)))


if __name__ == "__main__":
    main()

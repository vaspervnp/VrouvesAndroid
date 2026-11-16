using System.Text.Json;

namespace Vrouves;

/// <summary>An invader type: base hp, hp per wave, speed (1/16 step per tick), armour,
/// reward, damage to the sacks (= points when stopped).</summary>
public sealed record EType(int Hp, int Hpw, int Spd, int Arm, int Reward, int Dmg, string Name);

/// <summary>A defender type: cost, range (path steps), cooldown (ticks), damage, whether it
/// hits drones, its projectile, its sound, what it sells for and its sprite.</summary>
public sealed record TType(int Cost, int Range, int Cd, int Dmg, bool Air, string Proj, string Sfx, int Sell, string Spr);

/// <summary>One stage, as data/stages.json has it (taken from the CPC original by
/// VrouvesPC/tools/export_stages.py).</summary>
public sealed class StageDef
{
    public int Id;
    public string Base = "";
    public string[] Grid = [];
    public int[][] Ground = [];
    public int[][] Ground2 = [];
    public int[][] Air = [];
    public int[][] Sacks = [];
    public int[][][] Waves = [];     // per wave: groups of [type, count, ticks between]
    public int Toughness;
    public int[] Defenders = [];
}

/// <summary>Everything the game knows before it starts: the stages, the invader and
/// defender tables, the texts in English and Greek, the settings and the high scores.
/// No Android types here, so the logic also runs in the headless test.</summary>
public static class Data
{
    public const int COLS = 20;
    public const int ROWS = 14;
    public const int UNITS = 12;                 // path steps (and range units) per cell
    public const float TICK = 1f / 25f;          // one logic tick, as on the CPC
    public const int NUM_WAVES = 10;
    public const int START_FUNDS = 120;
    public const int START_HP = 100;
    public const int FIRST_COUNTDOWN = 45;
    public const int NEXT_COUNTDOWN = 15;
    public const int MAX_ENEMIES = 16;
    public const int SYN_MAX = 3;

    public const int DRONE = 1, GUARD = 2, TANK = 3;
    public const int CRETAN = 1, YIAYIA = 2, BRIDE = 3, PSSSST = 4, CAULDRON = 5, BAKER = 6, LYRA = 7,
        SHEPHERD = 8, PRIEST = 9;

    public static readonly Dictionary<int, EType> ETYPES = new()
    {
        [DRONE] = new(6, 3, 20, 0, 3, 4, "drone"),
        [GUARD] = new(16, 8, 9, 2, 4, 8, "guard"),
        [TANK] = new(50, 19, 7, 4, 10, 20, "tank"),
    };

    public static readonly Dictionary<int, TType> TTYPES = new()
    {
        [CRETAN] = new(35, 30, 18, 6, true, "bullet", "rifle", 17, "cretan"),
        [YIAYIA] = new(50, 18, 22, 14, false, "", "bash", 25, "yiayia"),
        [BRIDE] = new(70, 28, 6, 4, true, "heart", "pistol", 35, "bride"),
        [PSSSST] = new(80, 36, 10, 5, true, "laser", "laser", 40, "psssst"),
        [CAULDRON] = new(110, 22, 32, 8, false, "", "puff", 55, "cauldron"),
        [BAKER] = new(60, 34, 36, 16, true, "dakos", "throw", 30, "baker"),
        [LYRA] = new(90, 26, 26, 4, true, "", "lyra", 45, "lyra"),
        [SHEPHERD] = new(70, 44, 26, 14, true, "stone", "sling", 35, "shepherd"),
        [PRIEST] = new(60, 30, 75, 0, true, "", "bell", 30, "priest"),
    };

    /// <summary>the Shepherd's stone throws a guard back this many path steps</summary>
    public const int KNOCKBACK = 8;
    /// <summary>the Priest's bell holds the drones and guards near him still this many ticks</summary>
    public const int BELL_STUN = 20;

    /// <summary>the order of the defender cards, as the keys 1-9</summary>
    public static readonly int[] TOWER_ORDER = [CRETAN, YIAYIA, BRIDE, PSSSST, CAULDRON, BAKER, LYRA, SHEPHERD, PRIEST];

    /// <summary>the stage each defender joins in (a banner says so); the others are there from stage 1</summary>
    public static readonly Dictionary<int, int> JOINS = new()
    {
        [2] = YIAYIA, [3] = BRIDE, [4] = BAKER, [5] = CAULDRON, [8] = LYRA, [10] = SHEPHERD, [13] = PRIEST,
    };

    /// <summary>neighbour bonus: defender, next to, factor in 1/16; the first pair that holds counts</summary>
    public static readonly int[][] SYNERGY =
    [
        [YIAYIA, BAKER, 19], [BRIDE, CRETAN, 24], [PSSSST, CRETAN, 13], [BAKER, CRETAN, 8],
        [LYRA, LYRA, 8], [LYRA, YIAYIA, 24], [SHEPHERD, LYRA, 24],
    ];

    /// <summary>the difficulties in the order the menu goes through them: lives for the whole game</summary>
    public static readonly int[] DIFFICULTY_LIVES = [3, 2, 0, 4];

    public static List<StageDef> Stages = [];
    public static int Lang;                      // 0 English, 1 Greek
    public static bool MusicOn = true;
    public static int Difficulty;
    public static List<(string Name, int Score)> Hiscores = [];

    /// <summary>where the settings and high scores are kept (SharedPreferences on Android)</summary>
    public static Action? SaveSettingsHook, SaveHiscoresHook, SaveProgressHook;

    /// <summary>the game to continue: the last stage played, with the score and lives it
    /// started with; SavedStage 0 when there is none</summary>
    public static int SavedStage, SavedScore, SavedLives;

    public static void SaveProgress(int stage, int score, int lives)
    {
        (SavedStage, SavedScore, SavedLives) = (stage, score, lives);
        SaveProgressHook?.Invoke();
    }

    public static readonly (string, int)[] DEFAULT_HISCORES =
    [
        ("VAS", 6000), ("ZON", 5000), ("KRI", 4000), ("YIA", 3000),
        ("NYF", 2500), ("FOU", 2000), ("PAP", 1500), ("KAT", 1000),
    ];

    // ------------------------------------------------------------------ stages

    /// <summary>the CPC's build plans (VrouvesCPC/tools/scenario.py), per stage [type, col, row]
    /// in order, which the bot follows</summary>
    public static int[][][] Plans = [];

    public static void LoadPlans(Stream s)
    {
        using var doc = JsonDocument.Parse(s);
        Plans = doc.RootElement.GetProperty("plans").EnumerateArray()
            .Select(st => st.EnumerateArray().Select(p => p.EnumerateArray().Select(v => v.GetInt32()).ToArray()).ToArray())
            .ToArray();
    }

    public static void LoadStages(Stream s)
    {
        using var doc = JsonDocument.Parse(s);
        Stages = [];
        foreach (var st in doc.RootElement.GetProperty("stages").EnumerateArray())
        {
            Stages.Add(new StageDef
            {
                Id = st.GetProperty("id").GetInt32(),
                Base = st.GetProperty("base").GetString() ?? "",
                Grid = st.GetProperty("grid").EnumerateArray().Select(r => r.GetString() ?? "").ToArray(),
                Ground = Pairs(st.GetProperty("ground")),
                Ground2 = Pairs(st.GetProperty("ground2")),
                Air = Pairs(st.GetProperty("air")),
                Sacks = Pairs(st.GetProperty("sacks")),
                Waves = st.GetProperty("waves").EnumerateArray().Select(Pairs).ToArray(),
                Toughness = (int)st.GetProperty("toughness").GetDouble(),
                Defenders = st.GetProperty("defenders").EnumerateArray().Select(v => (int)v.GetDouble()).ToArray(),
            });
        }
    }

    static int[][] Pairs(JsonElement a) =>
        a.EnumerateArray().Select(p => p.EnumerateArray().Select(v => (int)v.GetDouble()).ToArray()).ToArray();

    // ------------------------------------------------------------------ settings

    public static void SaveSettings() => SaveSettingsHook?.Invoke();

    public static void ToggleLang()
    {
        Lang = 1 - Lang;
        SaveSettings();
    }

    // ------------------------------------------------------------------ high scores

    public static void ResetHiscores() => Hiscores = DEFAULT_HISCORES.ToList();

    public static int HiscoreRank(int score)
    {
        for (int i = 0; i < Hiscores.Count; i++)
            if (score > Hiscores[i].Score)
                return i;
        return -1;
    }

    public static int AddHiscore(string name, int score)
    {
        int i = HiscoreRank(score);
        if (i < 0)
            return -1;
        Hiscores.Insert(i, (name, score));
        Hiscores.RemoveRange(8, Hiscores.Count - 8);
        SaveHiscoresHook?.Invoke();
        return i;
    }

    // ------------------------------------------------------------------ texts

    /// <summary>id -> [English, Greek]</summary>
    public static readonly Dictionary<string, string[]> TEXT = new()
    {
        ["title"] = ["Let's Go For Vrouves", "Πάμε για Βρούβες"],
        ["subtitle"] = ["Defend the bitter greens of Zoniana!", "Σώστε τα πικρά χόρτα των Ζωνιανών!"],
        ["start"] = ["Start", "Έναρξη"],
        ["continue_at"] = ["Continue: stage {0}", "Συνέχεια: πίστα {0}"],
        ["new_game"] = ["New game", "Νέο παιχνίδι"],
        ["howto"] = ["How to play", "Οδηγίες"],
        ["story_t"] = ["The story", "Η ιστορία"],
        ["defenders_t"] = ["The defenders", "Οι υπερασπιστές"],
        ["invaders_t"] = ["The invaders of F&M Co.", "Οι εισβολείς της F&M Co."],
        ["hiscores_t"] = ["High scores", "Καλύτερα σκορ"],
        ["continue"] = ["Continue", "Συνέχεια"],
        ["to_title"] = ["Back to the title", "Επιστροφή στον τίτλο"],
        ["language"] = ["Ελληνικά", "English"],
        ["music"] = ["Music: ON", "Μουσική: ΝΑΙ"],
        ["music_off"] = ["Music: OFF", "Μουσική: ΟΧΙ"],
        ["dif0"] = ["Difficulty: Medium - 3 lives", "Δυσκολία: Μέτρια - 3 ζωές"],
        ["dif1"] = ["Difficulty: Hard - 2 lives", "Δυσκολία: Δύσκολη - 2 ζωές"],
        ["dif2"] = ["Difficulty: Hardest - no lives", "Δυσκολία: Πολύ δύσκολη - 0 ζωές"],
        ["dif3"] = ["Difficulty: Easy - 4 lives", "Δυσκολία: Εύκολη - 4 ζωές"],
        ["credit"] = ["REVIVE8BIT - 2026 - VASPER", "REVIVE8BIT - 2026 - VASPER"],
        ["back"] = ["Back", "Πίσω"],
        ["story"] = ["Armed with EU Directive 8647/2026, the corporate Goliaths at Frankenstein & Moreau Co. are deploying bio-drones, hazard squads and heavy machinery to destroy 'vrouves' - the wild, 'copyrighted' mountain greens of Crete.\n\nIn the village of Zoniana, deploy the defenders to protect the kafenio and its precious sacks of bitter greens!\n\nLuckily the rebel corporate drone Psssst has defected, and fights alongside the heavily fortified villagers.",
            "Οπλισμένοι με την Οδηγία ΕΕ 8647/2026, οι κολοσσοί της Frankenstein & Moreau Co. στέλνουν βιο-ντρον, συνεργεία και βαριά μηχανήματα για να ξεριζώσουν τις 'βρούβες' - τα άγρια, 'κατοχυρωμένα' χόρτα των βουνών της Κρήτης.\n\nΣτα Ζωνιανά, στήσε τους υπερασπιστές για να προστατέψεις το καφενείο και τα πολύτιμα σακιά με τα πικρά χόρτα!\n\nΕυτυχώς το επαναστατημένο ντρον Ψσσστ αυτομόλησε και πολεμά δίπλα στους οχυρωμένους χωριανούς."],
        ["controls"] = ["Choose a defender on the right, then touch a tile and hold: the ring shows its range, green if it can go there. Slide to another tile if you like, and lift your finger to build.\nTouch a defender once to see what it sells for, and again to sell it. Next wave calls the invaders early, for a bonus. Back pauses the game.",
            "Διάλεξε υπερασπιστή δεξιά και κράτα το δάχτυλο σε ένα τετράγωνο: ο κύκλος δείχνει το βεληνεκές, πράσινο αν χτίζεται εκεί. Σύρε σε άλλο τετράγωνο αν θες, και σήκωσε το δάχτυλο για να χτίσεις.\nΠάτα έναν υπερασπιστή μία φορά για να δεις πόσο πουλιέται, και ξανά για να τον πουλήσεις. Το Νέο κύμα φέρνει νωρίτερα τους εισβολείς, με μπόνους. Το Πίσω κάνει παύση."],
        ["synergy_help"] = ["Neighbours matter! Bride next to a Cretan +50%, Yiayia next to a Baker +20%, Lyra player next to a Yiayia +50%, Shepherd next to a Lyra player +50%; Baker next to a Cretan -50%, Psssst next to a Cretan -20%, two Lyra players together -50%. At most three bonuses at a time. The Priest's neighbours all shoot faster.",
            "Οι γείτονες μετράνε! Νύφη δίπλα σε Κρητικό +50%, Γιαγιά δίπλα σε Φούρναρη +20%, Λυράρης δίπλα σε Γιαγιά +50%, Βοσκός δίπλα σε Λυράρη +50%· Φούρναρης δίπλα σε Κρητικό -50%, Ψσσστ δίπλα σε Κρητικό -20%, δύο Λυράρηδες μαζί -50%. Το πολύ τρία μπόνους μαζί. Όσοι είναι δίπλα στον Παπά ρίχνουν πιο γρήγορα."],
        ["name1"] = ["Cretan", "Κρητικός"],
        ["name2"] = ["Yiayia", "Γιαγιά"],
        ["name3"] = ["Bride", "Νύφη"],
        ["name4"] = ["Psssst", "Ψσσστ"],
        ["name5"] = ["Cauldron", "Καζάνι"],
        ["name6"] = ["Baker", "Φούρναρης"],
        ["name7"] = ["Lyra player", "Λυράρης"],
        ["name8"] = ["Shepherd", "Βοσκός"],
        ["name9"] = ["Priest", "Παπάς"],
        ["desc1"] = ["In midnight black. Hunting rifle, hits drones and walkers.", "Μαυροφόρος με κυνηγετικό τουφέκι. Χτυπά ντρον και πεζούς."],
        ["desc2"] = ["Staff of chastisement and a pot of bitter leaves: slows everyone near her.", "Μαγκούρα και πικρά χόρτα: φρενάρει όλους γύρω της."],
        ["desc3"] = ["Wedding white, rapid pistol. One shot in four is a triple critical.", "Νυφικό και γρήγορο πιστόλι. Μία στις τέσσερις, τριπλή ζημιά."],
        ["desc4"] = ["The defected drone. Laser melts armour. Hovers over walls and trees.", "Το αυτόμολο ντρον. Λέιζερ που λιώνει θώρακες. Πετά πάνω από τοίχους."],
        ["desc5"] = ["Unlicensed lipids & ascorbic acids: toxic area cloud, ignores armour.", "Αδειοδότητα λιπίδια & ασκορβικά: τοξικό σύννεφο, αγνοεί θώρακες."],
        ["desc6"] = ["Hurls dakos (barley rusks) that splash everything around.", "Πετάει ντάκους (κριθαροκούλουρα) που πιτσιλάνε τα πάντα."],
        ["desc7"] = ["His tune hits every invader in reach, drones too, and slows the drones.", "Η μαντινάδα του χτυπά όλους γύρω του, και ντρον, και φρενάρει τα ντρον."],
        ["desc8"] = ["Sling with the longest reach, drones too. His stones throw the guards back.", "Σφεντόνα με το μεγαλύτερο βεληνεκές, και για ντρον. Οι πέτρες του πετάνε πίσω τους φύλακες."],
        ["desc9"] = ["Blesses his neighbours: they shoot faster. His bell stops drones and guards still.", "Ευλογεί τους γείτονές του: ρίχνουν πιο γρήγορα. Η καμπάνα του ακινητοποιεί ντρον και φύλακες."],
        ["ename1"] = ["Bio-drone", "Βιο-ντρον"],
        ["ename2"] = ["Compliance guard", "Φύλακας συμμόρφωσης"],
        ["ename3"] = ["Weed destroyer", "Καταστροφέας χόρτων"],
        ["edesc1"] = ["Fast and fragile. Flies straight over the walls. Yiayia and the cauldron can't reach it.", "Γρήγορο, εύθραυστο. Πετά πάνω από τους τοίχους. Γιαγιά και καζάνι δεν το φτάνουν."],
        ["edesc2"] = ["Armoured walker with a weed-whacker and herbicide.", "Θωρακισμένος πεζός με χορτοκοπτικό και ζιζανιοκτόνο."],
        ["edesc3"] = ["Treaded tank with lots of health. Dents cauldrons next to the road.", "Ερπυστριοφόρο με πολλή ζωή. Στραβώνει τα καζάνια δίπλα στο δρόμο."],
        ["score"] = ["Score", "Σκορ"],
        ["lives"] = ["Lives", "Ζωές"],
        ["wave"] = ["Wave", "Κύμα"],
        ["funds"] = ["Vrouves", "Βρούβες"],
        ["hp"] = ["Sacks", "Σακιά"],
        ["next_in"] = ["Next in {0}", "Επόμενο σε {0}"],
        ["go"] = ["GO!", "ΠΑΜΕ!"],
        ["next_btn"] = ["Next wave", "Νέο κύμα"],
        ["speed_btn"] = ["Speed x{0}", "Ταχύτητα x{0}"],
        ["pause_btn"] = ["Pause", "Παύση"],
        ["sell"] = ["Touch again: sell for +{0}", "Πάτα ξανά: πούλα για +{0}"],
        ["locked"] = ["Joins in a later stage", "Έρχεται σε επόμενη πίστα"],
        ["cant_afford"] = ["Not enough vrouves!", "Δεν φτάνουν οι βρούβες!"],
        ["cant_build"] = ["Can't build there", "Δεν χτίζεται εδώ"],
        ["only_psssst"] = ["Only Psssst can hover here", "Μόνο το Ψσσστ πετάει εδώ"],
        ["neighbour"] = ["Neighbour bonus", "Μπόνους γειτονιάς"],
        ["blessed"] = ["Blessed: shoots faster", "Ευλογημένος: ρίχνει πιο γρήγορα"],
        ["bonus_full"] = ["(bonus places full)", "(γεμάτες οι θέσεις μπόνους)"],
        ["call"] = ["Build, then call the next wave", "Χτίσε και κάλεσε το επόμενο κύμα"],
        ["clear"] = ["Wave cleared! Bonus vrouves!", "Το κύμα αποκρούστηκε! Μπόνους!"],
        ["final"] = ["Final wave! Here come the tanks!", "Τελικό κύμα! Έρχονται τα τανκς!"],
        ["newdef"] = ["A new defender joins the village!", "Νέος αμυντικός στο χωριό!"],
        ["altar"] = ["The Bride and the Priest are at the altar!", "Η Νύφη και ο Παπάς είναι στην εκκλησία!"],
        ["altar2"] = ["Hold them off without her!", "Κρατήστε τους μακριά χωρίς αυτή!"],
        ["quit_ask"] = ["Quit this game?", "Έξοδος από το παιχνίδι;"],
        ["quit_ask2"] = ["Back again quits to the title", "Πάτα ξανά Πίσω για έξοδο στον τίτλο"],
        ["press"] = ["Touch the screen", "Άγγιξε την οθόνη"],
        ["paused"] = ["Paused", "Παύση"],
        ["paused2"] = ["The invaders are waiting", "Οι εισβολείς περιμένουν"],
        ["gameover"] = ["Game over", "Τέλος παιχνιδιού"],
        ["life_lost"] = ["You lose a life. Try the stage again!", "Χάνεις μια ζωή. Ξανά από την αρχή!"],
        ["newhs"] = ["New high score!", "Νέο ρεκόρ!"],
        ["entername"] = ["Touch your name (3 letters), then OK", "Γράψε το όνομά σου (3 γράμματα) και OK"],
        ["ok"] = ["OK", "OK"],
        ["del"] = ["Delete", "Σβήσε"],
        ["ending"] = ["The Bride's wedding: the whole village dances in front of the church!", "Ο γάμος της Νύφης: όλο το χωριό γλεντά μπροστά στην εκκλησία!"],
        ["ending2"] = ["The vrouves of Crete are safe. Kali orexi!", "Οι βρούβες της Κρήτης σώθηκαν. Καλή όρεξη!"],
        ["final_score"] = ["Final score: {0}", "Τελικό σκορ: {0}"],
    };

    public static string T(string id) => TEXT.TryGetValue(id, out var s) ? s[Lang] : id;

    public static string T(string id, int v) => string.Format(T(id), v);

    /// <summary>per stage: name, won, lost</summary>
    static readonly string[][][] STAGE_TEXT =
    [
        [["Stage 1: The Kafenio", "Πίστα 1: Το καφενείο"],
            ["Kafenio saved! Now, to the church!", "Το καφενείο σώθηκε! Στην εκκλησία!"],
            ["The kafenio has fallen!", "Το καφενείο έπεσε!"]],
        [["Stage 2: The village church", "Πίστα 2: Η εκκλησία του χωριού"],
            ["Church saved! Now, to the town hall!", "Η εκκλησία σώθηκε! Στο δημαρχείο!"],
            ["The church has fallen!", "Η εκκλησία έπεσε!"]],
        [["Stage 3: The town hall", "Πίστα 3: Το δημαρχείο"],
            ["Town hall saved! Now, to the bakery!", "Το δημαρχείο σώθηκε! Στο φούρνο!"],
            ["The town hall has fallen!", "Το δημαρχείο έπεσε!"]],
        [["Stage 4: The bakery", "Πίστα 4: Ο φούρνος"],
            ["Bakery saved! Now, to the school!", "Ο φούρνος σώθηκε! Στο σχολείο!"],
            ["The bakery has fallen!", "Ο φούρνος έπεσε!"]],
        [["Stage 5: The school", "Πίστα 5: Το σχολείο"],
            ["School saved! Now, to the pitch!", "Το σχολείο σώθηκε! Στο γήπεδο!"],
            ["The school has fallen!", "Το σχολείο έπεσε!"]],
        [["Stage 6: The village pitch", "Πίστα 6: Το γήπεδο του χωριού"],
            ["Pitch saved! Now, to the cemetery!", "Το γήπεδο σώθηκε! Στο νεκροταφείο!"],
            ["The pitch has fallen!", "Το γήπεδο έπεσε!"]],
        [["Stage 7: The cemetery church", "Πίστα 7: Η εκκλησία του νεκροταφείου"],
            ["Cemetery saved! Now, up the mountain!", "Το νεκροταφείο σώθηκε! Στο βουνό!"],
            ["The cemetery church has fallen!", "Η εκκλησία του νεκροταφείου έπεσε!"]],
        [["Stage 8: The sheepfold", "Πίστα 8: Η στάνη στο βουνό"],
            ["Sheepfold saved! Now, to the olive press!", "Η στάνη σώθηκε! Στο λιοτρίβι!"],
            ["The sheepfold has fallen!", "Η στάνη έπεσε!"]],
        [["Stage 9: The olive press", "Πίστα 9: Το λιοτρίβι"],
            ["Olive press saved! Now, to the cheese dairy!", "Το λιοτρίβι σώθηκε! Στο τυροκομείο!"],
            ["The olive press has fallen!", "Το λιοτρίβι έπεσε!"]],
        [["Stage 10: The cheese dairy", "Πίστα 10: Το τυροκομείο"],
            ["Cheese dairy saved! Now, to the watermill!", "Το τυροκομείο σώθηκε! Στον νερόμυλο!"],
            ["The cheese dairy has fallen!", "Το τυροκομείο έπεσε!"]],
        [["Stage 11: The watermill", "Πίστα 11: Ο νερόμυλος"],
            ["Watermill saved! Now, to the Sfendoni cave!", "Ο νερόμυλος σώθηκε! Στο σπήλαιο Σφενδόνη!"],
            ["The watermill has fallen!", "Ο νερόμυλος έπεσε!"]],
        [["Stage 12: The Sfendoni cave", "Πίστα 12: Το σπήλαιο Σφενδόνη"],
            ["Cave saved! Now, to the monastery!", "Το σπήλαιο σώθηκε! Στο μοναστήρι!"],
            ["The cave has fallen!", "Το σπήλαιο έπεσε!"]],
        [["Stage 13: The monastery", "Πίστα 13: Το μοναστήρι"],
            ["Monastery saved! Now, to the beehives!", "Το μοναστήρι σώθηκε! Στα μελίσσια!"],
            ["The monastery has fallen!", "Το μοναστήρι έπεσε!"]],
        [["Stage 14: The beehives", "Πίστα 14: Τα μελίσσια"],
            ["Beehives saved! Now, to the raki still!", "Τα μελίσσια σώθηκαν! Στο ρακοκάζανο!"],
            ["The beehives have fallen!", "Τα μελίσσια χάθηκαν!"]],
        [["Stage 15: The raki still", "Πίστα 15: Το ρακοκάζανο"],
            ["Raki still saved! Now, to the windmills!", "Το ρακοκάζανο σώθηκε! Στους ανεμόμυλους!"],
            ["The raki still has fallen!", "Το ρακοκάζανο έπεσε!"]],
        [["Stage 16: The windmills", "Πίστα 16: Οι ανεμόμυλοι"],
            ["Windmills saved! Now, to the threshing floor!", "Οι ανεμόμυλοι σώθηκαν! Στο αλώνι!"],
            ["The windmills have fallen!", "Οι ανεμόμυλοι έπεσαν!"]],
        [["Stage 17: The threshing floor", "Πίστα 17: Το αλώνι"],
            ["Threshing floor saved! Now, to the vineyard!", "Το αλώνι σώθηκε! Στο αμπέλι!"],
            ["The threshing floor has fallen!", "Το αλώνι έπεσε!"]],
        [["Stage 18: The vineyard", "Πίστα 18: Το αμπέλι"],
            ["Vineyard saved! Now, up Psiloritis!", "Το αμπέλι σώθηκε! Στον Ψηλορείτη!"],
            ["The vineyard has fallen!", "Το αμπέλι χάθηκε!"]],
        [["Stage 19: The summit chapel", "Πίστα 19: Το ξωκλήσι της κορυφής"],
            ["Summit saved! Now, to the wedding!", "Η κορυφή σώθηκε! Στο γάμο!"],
            ["The summit chapel has fallen!", "Το ξωκλήσι έπεσε!"]],
        [["Stage 20: The Bride's wedding", "Πίστα 20: Ο γάμος της Νύφης"],
            ["Wedding saved! Long live the couple!", "Ο γάμος σώθηκε! Ζήτω το ζευγάρι!"],
            ["The wedding has fallen!", "Ο γάμος χάλασε!"]],
    ];

    /// <summary>what = 0 name, 1 won, 2 lost</summary>
    public static string StageText(int stageNo, int what) => STAGE_TEXT[stageNo - 1][what][Lang];
}

/// <summary>The sounds the logic asks for; the Android side plays them.</summary>
public static class Audio
{
    public static Action<string>? Sink;
    public static void Play(string name) => Sink?.Invoke(name);
}

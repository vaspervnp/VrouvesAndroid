using Android.Content;
using Android.Media;

namespace Vrouves;

/// <summary>Music and sound effects (Assets/audio, made by VrouvesPC/tools/make_audio.py from
/// the CPC's tune and AY sound tables; the sling and the bell by tools/make_sfx.py). Six voices as in the Godot version: like the AY
/// channel on the CPC, a sound only cuts in over one of lower or equal priority.</summary>
public static class Sfx
{
    static readonly string[] NAMES = ["rifle", "pistol", "laser", "throw", "bash", "puff", "lyra", "sling", "bell", "die", "hurt", "build",
        "error", "sell", "wave", "crush", "move"];
    static readonly Dictionary<string, int> PRIO = new()
    {
        ["move"] = 0, ["pistol"] = 1, ["rifle"] = 1, ["laser"] = 1, ["throw"] = 1, ["lyra"] = 1, ["sling"] = 1, ["puff"] = 2, ["bell"] = 2, ["bash"] = 2,
        ["die"] = 3, ["sell"] = 4, ["build"] = 4, ["error"] = 4, ["crush"] = 5, ["hurt"] = 6, ["wave"] = 7,
    };
    const int VOICES = 6;
    const float SFX_VOL = 0.5f;          // -6 dB
    const float MUSIC_VOL = 0.35f;       // -9 dB

    sealed class Voice
    {
        public int Stream;
        public string Name = "";
        public long End;
    }

    static SoundPool? pool;
    static readonly Dictionary<string, int> ids = [];
    static readonly Dictionary<string, long> lengthMs = [];
    static readonly Dictionary<string, long> last = [];
    static readonly Voice[] voices = Enumerable.Range(0, VOICES).Select(_ => new Voice()).ToArray();
    static MediaPlayer? music;
    static bool musicWanted;
    static bool appPaused;
    static readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();

    public static void Init(Context ctx)
    {
        var attrs = new AudioAttributes.Builder()!.SetUsage(AudioUsageKind.Game)!.SetContentType(AudioContentType.Sonification)!.Build();
        pool = new SoundPool.Builder()!.SetMaxStreams(VOICES + 2)!.SetAudioAttributes(attrs)!.Build()!;
        foreach (var n in NAMES)
        {
            using var fd = ctx.Assets!.OpenFd($"audio/sfx_{n}.wav");
            ids[n] = pool.Load(fd, 1);
            lengthMs[n] = WavMs(ctx, $"audio/sfx_{n}.wav");
        }
        try
        {
            music = new MediaPlayer();
            using var fd = ctx.Assets!.OpenFd("audio/music.ogg");
            music.SetDataSource(fd.FileDescriptor, fd.StartOffset, fd.Length);
            music.SetAudioAttributes(new AudioAttributes.Builder()!.SetUsage(AudioUsageKind.Game)!.SetContentType(AudioContentType.Music)!.Build()!);
            music.Looping = true;
            music.SetVolume(MUSIC_VOL, MUSIC_VOL);
            music.Prepare();
        }
        catch (Exception ex)
        {
            Android.Util.Log.Warn("Vrouves", "no music: " + ex.Message);
            music = null;
        }
        Audio.Sink = Play;
    }

    /// <summary>how long a PCM WAV plays, from its header</summary>
    static long WavMs(Context ctx, string path)
    {
        using var s = ctx.Assets!.Open(path);
        var h = new byte[44];
        int n = 0;
        while (n < 44)
        {
            int r = s.Read(h, n, 44 - n);
            if (r <= 0)
                return 300;
            n += r;
        }
        int ch = BitConverter.ToInt16(h, 22), rate = BitConverter.ToInt32(h, 24), bits = BitConverter.ToInt16(h, 34);
        int data = BitConverter.ToInt32(h, 40);
        long bytesPerSec = (long)rate * ch * bits / 8;
        return bytesPerSec > 0 ? data * 1000L / bytesPerSec : 300;
    }

    public static void Play(string name)
    {
        if (pool == null || appPaused || !ids.TryGetValue(name, out int id))
            return;
        long now = clock.ElapsedMilliseconds;
        // the same shot from a dozen towers in one tick is one shot
        if (last.TryGetValue(name, out long t) && t > now - 45)
            return;
        last[name] = now;
        Voice? best = voices.FirstOrDefault(v => v.End <= now);
        if (best == null)
        {
            // take over the one playing the least important sound, if this one matters as much
            int lo = 99;
            foreach (var v in voices)
            {
                int pr = PRIO.GetValueOrDefault(v.Name, 0);
                if (pr < lo)
                {
                    lo = pr;
                    best = v;
                }
            }
            if (best == null || lo > PRIO.GetValueOrDefault(name, 0))
                return;
            pool.Stop(best.Stream);
        }
        best.Stream = pool.Play(id, SFX_VOL, SFX_VOL, PRIO.GetValueOrDefault(name, 0), 0, 1f);
        best.Name = name;
        best.End = now + lengthMs[name];
    }

    public static void Music(bool on)
    {
        musicWanted = on;
        if (music == null)
            return;
        if (on && Data.MusicOn && !appPaused)
        {
            if (!music.IsPlaying)
                music.Start();
        }
        else if (music.IsPlaying)
        {
            music.Pause();
            if (!on)
                music.SeekTo(0);
        }
    }

    public static void ToggleMusic()
    {
        Data.MusicOn = !Data.MusicOn;
        Data.SaveSettings();
        Music(musicWanted);
    }

    /// <summary>the app went to the background: no sound at all</summary>
    public static void OnAppPause()
    {
        appPaused = true;
        pool?.AutoPause();
        if (music is { IsPlaying: true })
            music.Pause();
    }

    public static void OnAppResume()
    {
        appPaused = false;
        pool?.AutoResume();
        Music(musicWanted);
    }
}

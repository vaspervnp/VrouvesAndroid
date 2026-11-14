using Android.Content.Res;
using Android.Graphics;
using Rect = Android.Graphics.Rect;

namespace Vrouves;

public enum Align { Left, Center, Right }

/// <summary>Drawing on an Android Canvas in the screens' own coordinates, in the manner of
/// Godot's CanvasItem calls the original scripts use: colours are 0xAARRGGBB, text is
/// placed by its baseline and may be aligned within a width, sprites are drawn around an
/// anchor point with a scale, a rotation and a tint.</summary>
public sealed class Gfx
{
    public Canvas C = null!;
    public readonly Typeface Title, Bold, Regular;

    readonly AssetManager assets;
    readonly Paint fill = new() { AntiAlias = true };
    readonly Paint stroke = new() { AntiAlias = true };
    readonly Paint text = new() { AntiAlias = true, SubpixelText = true };
    readonly Paint img = new() { AntiAlias = true, FilterBitmap = true, Dither = true };
    readonly RectF rf = new();
    readonly Rect rs = new();
    readonly Dictionary<string, Bitmap> tex = [];
    readonly Dictionary<uint, ColorFilter> tints = [];

    public Gfx(AssetManager assets)
    {
        this.assets = assets;
        Title = Typeface.CreateFromAsset(assets, "fonts/NotoSansDisplay-Black.ttf")!;
        Bold = Typeface.CreateFromAsset(assets, "fonts/NotoSans-Bold.ttf")!;
        Regular = Typeface.CreateFromAsset(assets, "fonts/NotoSans-Regular.ttf")!;
        stroke.SetStyle(Paint.Style.Stroke);
        text.StrokeJoin = Paint.Join.Round;
        text.StrokeMiter = 2f;
    }

    static Color Argb(uint c) => new((int)c);

    // ------------------------------------------------------------------ pictures

    public Bitmap LoadBitmap(string path, bool mipmaps)
    {
        using var s = assets.Open(path);
        var b = BitmapFactory.DecodeStream(s)!;
        b.HasMipMap = mipmaps;
        return b;
    }

    /// <summary>a sprite from Assets/sprites, loaded the first time it is asked for</summary>
    public Bitmap Tex(string name)
    {
        if (!tex.TryGetValue(name, out var b))
            tex[name] = b = LoadBitmap($"sprites/{name}.png", true);
        return b;
    }

    /// <summary>load every sprite now: loading one the first time it shows up stalls a frame</summary>
    public void PreloadSprites()
    {
        foreach (var f in assets.List("sprites") ?? [])
            if (f.EndsWith(".png"))
                Tex(f[..^4]);
    }

    public void Image(Bitmap b, float x, float y, float w, float h, float alpha = 1f)
    {
        rf.Set(x, y, x + w, y + h);
        img.SetColorFilter(null);
        img.Alpha = (int)(Math.Clamp(alpha, 0, 1) * 255);
        C.DrawBitmap(b, null, rf, img);
    }

    /// <summary>a picture with its point (ax, ay) at (x, y), scaled, turned (radians) and
    /// tinted (each channel multiplied by the tint's)</summary>
    public void Sprite(Bitmap b, float x, float y, float sx, float sy, float ax, float ay, float rot = 0f, uint tint = Col.WHITE)
    {
        C.Save();
        C.Translate(x, y);
        if (rot != 0f)
            C.Rotate(rot * 180f / MathF.PI);
        C.Scale(sx, sy);
        img.Alpha = (int)(tint >> 24);
        img.SetColorFilter(Tint(tint & 0xFFFFFF));
        C.DrawBitmap(b, -ax, -ay, img);
        C.Restore();
    }

    ColorFilter? Tint(uint rgb)
    {
        if (rgb == 0xFFFFFF)
            return null;
        if (!tints.TryGetValue(rgb, out var f))
        {
            float r = ((rgb >> 16) & 255) / 255f, g = ((rgb >> 8) & 255) / 255f, b = (rgb & 255) / 255f;
            tints[rgb] = f = new ColorMatrixColorFilter([r, 0, 0, 0, 0, 0, g, 0, 0, 0, 0, 0, b, 0, 0, 0, 0, 0, 1, 0]);
            if (tints.Count > 512)
                tints.Clear();
        }
        return f;
    }

    /// <summary>a sprite of Assets/sprites with its ground point at (x, y), as the Godot
    /// version's draw_sprite: big pictures stand at 200/256 of their height, small ones at 3/4</summary>
    public void Sprite(string name, float x, float y, float scale, bool flip = false, uint tint = Col.WHITE)
    {
        var b = Tex(name);
        float ay = b.Height > 128 ? b.Height * 200f / 256f : b.Height * 0.75f;
        Sprite(b, x, y, flip ? -scale : scale, scale, b.Width / 2f, ay, 0f, tint);
    }

    // ------------------------------------------------------------------ shapes

    public void Rect(float x, float y, float w, float h, uint col)
    {
        fill.Color = Argb(col);
        C.DrawRect(x, y, x + w, y + h, fill);
    }

    public void RectLine(float x, float y, float w, float h, uint col, float width)
    {
        stroke.Color = Argb(col);
        stroke.StrokeWidth = width;
        C.DrawRect(x, y, x + w, y + h, stroke);
    }

    public void RoundRect(float x, float y, float w, float h, float rad, uint col)
    {
        fill.Color = Argb(col);
        rf.Set(x, y, x + w, y + h);
        C.DrawRoundRect(rf, rad, rad, fill);
    }

    public void RoundRectLine(float x, float y, float w, float h, float rad, uint col, float width)
    {
        stroke.Color = Argb(col);
        stroke.StrokeWidth = width;
        rf.Set(x, y, x + w, y + h);
        C.DrawRoundRect(rf, rad, rad, stroke);
    }

    public void Circle(float cx, float cy, float r, uint col)
    {
        fill.Color = Argb(col);
        C.DrawCircle(cx, cy, r, fill);
    }

    public void Ellipse(float cx, float cy, float rx, float ry, uint col)
    {
        fill.Color = Argb(col);
        rf.Set(cx - rx, cy - ry, cx + rx, cy + ry);
        C.DrawOval(rf, fill);
    }

    /// <summary>an arc from angle a0 over the sweep (radians, clockwise as on the screen)</summary>
    public void Arc(float cx, float cy, float r, float a0, float sweep, uint col, float width)
    {
        stroke.Color = Argb(col);
        stroke.StrokeWidth = width;
        rf.Set(cx - r, cy - r, cx + r, cy + r);
        C.DrawArc(rf, a0 * 180f / MathF.PI, sweep * 180f / MathF.PI, false, stroke);
    }

    public void Line(float x1, float y1, float x2, float y2, uint col, float width, bool round = false)
    {
        stroke.Color = Argb(col);
        stroke.StrokeWidth = width;
        stroke.StrokeCap = round ? Paint.Cap.Round : Paint.Cap.Butt;
        C.DrawLine(x1, y1, x2, y2, stroke);
        stroke.StrokeCap = Paint.Cap.Butt;
    }

    // ------------------------------------------------------------------ text

    Paint TextPaint(float size, Typeface? f)
    {
        text.SetTypeface(f ?? Bold);
        text.TextSize = size;
        return text;
    }

    public float Width(string s, float size, Typeface? f = null) => TextPaint(size, f).MeasureText(s);

    /// <summary>a line of text with its baseline at y, starting at x, or aligned within the
    /// width from x; with an outline (its width in pixels) behind it. Returns its width.</summary>
    public float Text(float x, float y, string s, float size, uint col, Align align = Align.Left, float width = -1,
        Typeface? f = null, float outline = 0, uint outlineCol = 0xD9000000)
    {
        var p = TextPaint(size, f);
        float w = p.MeasureText(s);
        if (width >= 0 && align == Align.Center)
            x += (width - w) / 2;
        else if (width >= 0 && align == Align.Right)
            x += width - w;
        else if (width < 0 && align == Align.Center)
            x -= w / 2;
        else if (width < 0 && align == Align.Right)
            x -= w;
        if (outline > 0)
        {
            p.SetStyle(Paint.Style.Stroke);
            p.StrokeWidth = outline;
            p.Color = Argb(outlineCol);
            C.DrawText(s, x, y, p);
            p.SetStyle(Paint.Style.Fill);
        }
        p.Color = Argb(col);
        C.DrawText(s, x, y, p);
        return w;
    }

    /// <summary>the lines a text breaks into within the width</summary>
    public List<string> Wrap(string s, float width, float size, Typeface? f = null)
    {
        var p = TextPaint(size, f);
        var lines = new List<string>();
        foreach (var para in s.Split('\n'))
        {
            string line = "";
            foreach (var word in para.Split(' '))
            {
                string tryLine = line == "" ? word : line + " " + word;
                if (line != "" && p.MeasureText(tryLine) > width)
                {
                    lines.Add(line);
                    line = word;
                }
                else
                    line = tryLine;
            }
            lines.Add(line);
        }
        return lines;
    }

    public static float LineHeight(float size) => size * 1.36f;

    /// <summary>text broken into lines within the width, the first baseline at y; returns
    /// the y below the last line</summary>
    public float Multiline(float x, float y, string s, float width, float size, uint col, Align align = Align.Left,
        Typeface? f = null, int maxLines = -1, float outline = 0)
    {
        var lines = Wrap(s, width, size, f);
        if (maxLines > 0 && lines.Count > maxLines)
            lines.RemoveRange(maxLines, lines.Count - maxLines);
        foreach (var l in lines)
        {
            Text(x, y, l, size, col, align, width, f, outline);
            y += LineHeight(size);
        }
        return y;
    }
}

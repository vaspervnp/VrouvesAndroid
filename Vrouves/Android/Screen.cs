using Android.Views;

namespace Vrouves;

/// <summary>A screen of the game (title, stage, ending). It is laid out in its own
/// coordinates: DesignH units high and as wide as the display's shape makes that (at least
/// MinW; on a narrower display it is MinW wide and taller instead). GameView sets W and H
/// before every frame.</summary>
public abstract class Screen
{
    public float W, H;
    /// <summary>how far the display goes beyond (0, 0, W, H) on each side, beside a camera
    /// cutout: only backgrounds should be drawn there</summary>
    public float ExtL, ExtT, ExtR, ExtB;
    public virtual float DesignH => 1024f;
    public virtual float MinW => 1280f;

    public abstract void Update(float dt);
    public abstract void Draw(Gfx g);

    public virtual void Down(float x, float y) { }
    public virtual void Move(float x, float y) { }
    public virtual void Up(float x, float y) { }
    public virtual void Cancel() { }

    /// <summary>a key of a keyboard or game pad; true when it was used</summary>
    public virtual bool Key(Keycode k, KeyEvent e) => false;

    /// <summary>the Back button or gesture; false lets the app close</summary>
    public virtual bool Back() => false;

    /// <summary>the app goes to the background</summary>
    public virtual void AppPaused() { }

    /// <summary>the screen is replaced by another</summary>
    public virtual void Leave() { }

    public static bool In(float x, float y, (float x, float y, float w, float h) r) =>
        x >= r.x && y >= r.y && x < r.x + r.w && y < r.y + r.h;
}

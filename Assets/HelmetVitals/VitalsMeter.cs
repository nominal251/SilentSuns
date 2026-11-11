using UnityEngine;
using UnityEngine.UI;

public enum VitalsFace { Terminal, SideEye, Cursor, SevenSegment, Cyclops, Moon, Brackets, SplitMask, Spark, Crooked }
public enum VitalsBar { Visor, MemoryBlocks, Battery, Steps, TwinFilament, Chevrons, DotMatrix, Ruler, FuseLinks, Wedge }

[RequireComponent(typeof(CanvasRenderer))]
public sealed class VitalsMeter : MaskableGraphic
{
    // Layout follows the visible edge of the faceplate. The backing extends past the panel.
    private const float BackingPadding = 5f;
    private const float Gutter = 11f;
    private const float Inset = Gutter - BackingPadding;
    private const float FaceWidth = 50f;
    private const float FaceHeight = 44f;
    private const float ReadoutLeft = Inset + FaceWidth + Gutter;
    private const float RowHeight = (FaceHeight - Gutter) * .5f;

    [SerializeField] private VitalsPalette palette;
    [SerializeField] private Texture2D options;
    [SerializeField] private VitalsFace faceStyle = VitalsFace.Terminal;
    [SerializeField] private VitalsBar barStyle = VitalsBar.MemoryBlocks;
    [SerializeField, Range(.1f, 1f)] private float reactionSeconds = .56f;
    [Tooltip("Minimum health lost in one hit to show the heavy impact expression.")]
    [SerializeField, Range(1f, 100f)] private float heavyHitDamage = 15f;

    private float health = 100f;
    private float startingEdge;
    private HealthCondition condition;
    private HealthCondition outgoingFace;
    private bool hasReading;
    private bool healing;
    private float progress = 1f;
    private float faceProgress = 1f;
    private float impact;
    private bool faceLifts;
    private float faceStrength;
    private float blink;
    private float pulse;
    private float idleClock;
    private bool heavyHit;
    private float hitHold;
    private float lightHitCooldown;
    private Vector2 readoutVelocity;
    public Vector2 ReadoutOffset { get; private set; }
    public Color SignalColor => Color.Lerp(palette.Signal(condition), palette.lettering, .22f * pulse);
    public override Texture mainTexture => options ? options : base.mainTexture;

    private float MovingEdge => Mathf.Lerp(health, startingEdge, (1 - progress) * (1 - progress));
    private HealthCondition VisibleFace => faceProgress < .20f ? outgoingFace : condition;

    public void SetReading(float value, HealthCondition state)
    {
        if (hasReading && value == health && state == condition) return;
        startingEdge = MovingEdge;
        float change = Mathf.Abs(value - health);
        bool changedCondition = state != condition;
        bool animate = hasReading && value > 0 && !SilentSuns.Menu.GamePreferences.ReducedMotion;
        healing = value > health;
        if (!animate || healing) hitHold = lightHitCooldown = 0;
        else if (change > 0 && faceStyle == VitalsFace.Terminal &&
                 (change >= heavyHitDamage || (hitHold <= 0 && lightHitCooldown <= 0)))
        {
            heavyHit = change >= heavyHitDamage;
            hitHold = heavyHit ? .27f : .14f;
            lightHitCooldown = hitHold + .24f;
        }
        if (!hasReading || SilentSuns.Menu.GamePreferences.ReducedMotion)
        {
            ReadoutOffset = readoutVelocity = Vector2.zero;
        }
        else if (change > 0)
        {
            // Fractional regeneration and tightly spaced hits add to the motion already in flight.
            float strength = Mathf.Clamp01(change / 50f);
            float kick = Mathf.Min(1, change) * Mathf.Lerp(40, 168, strength);
            if (healing)
                readoutVelocity.y = Mathf.Min(140, readoutVelocity.y + kick);
            else
            {
                readoutVelocity.x = Mathf.Max(-172, readoutVelocity.x - kick);
                readoutVelocity.y = Mathf.Max(-36, readoutVelocity.y - kick * .18f);
            }
        }
        impact = Mathf.InverseLerp(0, 24, change);
        progress = animate && change > 0 ? 0 : 1;
        if (!animate || changedCondition || change >= 6)
        {
            outgoingFace = VisibleFace;
            faceStrength = changedCondition ? 1 : impact;
            faceProgress = animate ? 0 : 1;
            faceLifts = healing;
            idleClock = blink = pulse = 0;
        }
        health = value;
        condition = state;
        hasReading = true;
        SetVerticesDirty();
    }

    protected override void OnDisable()
    {
        hasReading = false;
        progress = faceProgress = 1;
        idleClock = blink = pulse = 0;
        hitHold = lightHitCooldown = 0;
        ReadoutOffset = readoutVelocity = Vector2.zero;
        base.OnDisable();
    }

    private void Update()
    {
        if (!hasReading) return;
        bool moving = progress < 1 || faceProgress < 1 || hitHold > 0;
        Vector2 oldOffset = ReadoutOffset;
        float oldBlink = blink, oldPulse = pulse;
        blink = pulse = 0;
        if (SilentSuns.Menu.GamePreferences.ReducedMotion)
        {
            progress = faceProgress = 1;
            hitHold = lightHitCooldown = 0;
            ReadoutOffset = readoutVelocity = Vector2.zero;
        }
        else
        {
            AdvanceReadout(Time.deltaTime);
            hitHold = Mathf.Max(0, hitHold - Time.deltaTime);
            lightHitCooldown = Mathf.Max(0, lightHitCooldown - Time.deltaTime);
            float dt = Time.deltaTime / Mathf.Max(.1f, reactionSeconds);
            progress = Mathf.Min(1, progress + dt / Mathf.Lerp(.42f, 1, impact));
            faceProgress = Mathf.Min(1, faceProgress + dt);
            idleClock += Time.deltaTime;
            if (health > 0 && faceProgress >= 1)
            {
                if (condition == HealthCondition.Good && faceStyle == VitalsFace.Terminal)
                    blink = Mathf.Clamp01(1 - Mathf.Abs(Mathf.Repeat(idleClock, 4.1f) - 3.65f) / .105f);
                else if (condition == HealthCondition.Danger)
                {
                    float beat = Mathf.Repeat(idleClock, 1.32f);
                    pulse = Mathf.SmoothStep(0, 1, Mathf.Clamp01(1 - Mathf.Abs(beat - .17f) / .17f));
                }
            }
        }
        if (moving || blink != oldBlink || pulse != oldPulse || oldOffset != ReadoutOffset) SetVerticesDirty();
    }

    private void AdvanceReadout(float dt)
    {
        if (readoutVelocity == Vector2.zero && ReadoutOffset == Vector2.zero) return;
        // Damped horizontal oscillation for a hit. Vertical lift returns without bouncing.
        const float drag = 11f, frequency = 38f, liftDrag = 13f;
        Vector2 position = ReadoutOffset;
        float sin = Mathf.Sin(frequency * dt), cos = Mathf.Cos(frequency * dt);
        float decay = Mathf.Exp(-drag * dt);
        float wave = (readoutVelocity.x + drag * position.x) / frequency;
        float x = decay * (position.x * cos + wave * sin);
        readoutVelocity.x = decay * frequency * (wave * cos - position.x * sin) - drag * x;

        float lift = readoutVelocity.y + liftDrag * position.y;
        float settle = Mathf.Exp(-liftDrag * dt);
        float y = settle * (position.y + lift * dt);
        readoutVelocity.y = settle * lift - liftDrag * y;
        ReadoutOffset = new Vector2(x, y);
        if (ReadoutOffset.sqrMagnitude < .0001f && readoutVelocity.sqrMagnitude < .01f)
            ReadoutOffset = readoutVelocity = Vector2.zero;
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (!options || !palette) return;
        Color signal = SignalColor;
        // Atlas source rectangles use top-left image coordinates.
        DrawBacking(mesh);
        Quad(mesh, new Rect(ReadoutLeft, Inset, 114, RowHeight), new Rect(570, 915, 255, 45), signal);
        bool framed = faceStyle == VitalsFace.Terminal;
        var plate = new Rect(Inset, Inset, FaceWidth, FaceHeight);
        if (framed) Quad(mesh, plate, new Rect(430, 910, 125, 110), signal);

        // Pinch the outgoing expression, then open the new one with a short rebound.
        float pinch = faceStrength * (faceProgress < .20f ? faceProgress / .20f : 1 - Mathf.InverseLerp(.20f, .47f, faceProgress));
        float rebound = faceStrength * Mathf.Sin(Mathf.PI * Mathf.InverseLerp(.20f, .85f, faceProgress));
        float w = 53 + 6 * pinch - 2 * rebound;
        float h = 40 - (hitHold > 0 ? 10 : 23) * pinch + 4 * rebound;
        if (framed) { w *= .82f; h *= .82f; }
        float dx = faceLifts ? 0 : -4 * pinch + 2 * rebound;
        float dy = faceLifts ? 4 * rebound : -2 * pinch;
        var icon = new Rect(plate.center.x + dx - w / 2, plate.center.y + dy - h / 2, w, h);
        var expression = VisibleFace;
        int row = expression == HealthCondition.Good ? 0 : expression == HealthCondition.Caution ? 1 : 2;
        var source = new Rect(2 + 100 * (int)faceStyle, 2 + 76 * row, 96, 72);
        if (framed && outgoingFace != condition && faceProgress >= .20f && faceProgress < .80f)
        {
            bool nearDanger = outgoingFace == HealthCondition.Danger || condition == HealthCondition.Danger;
            source = new Rect(nearDanger ? 304 : 204, 912, 96, 72);
        }
        Color ink = framed ? palette.glass : Color.Lerp(palette.Signal(expression), palette.lettering, .22f * pulse);
        if (hitHold > 0 && faceStyle == VitalsFace.Terminal)
        {
            var hitSource = new Rect(heavyHit ? 102 : 2, 912, 96, 72);
            Quad(mesh, icon, hitSource, ink);
        }
        else if (blink > 0)
        {
            // Only the upper half contains eyes. Keep the smile on its baseline.
            float half = icon.height * .5f;
            float eyelid = half * (1 - .84f * blink);
            Quad(mesh, new Rect(icon.x, icon.y + half + (half - eyelid) * .45f, w, eyelid),
                new Rect(source.x, source.y, 96, 36), ink);
            Quad(mesh, new Rect(icon.x, icon.y, w, half), new Rect(source.x, source.y + 36, 96, 36), ink);
        }
        else Quad(mesh, icon, source, ink);

        int y = 256 + 64 * (int)barStyle;
        var track = TrackRect;
        bool blocks = barStyle == VitalsBar.MemoryBlocks;
        if (blocks) Quad(mesh, track, new Rect(565, 972, 336, 40), signal);
        Color emptyCells = Color.Lerp(signal, palette.lettering, .65f);
        Quad(mesh, track, new Rect(2, y, 504, 60), blocks ? emptyCells : Color.white);
        if (health <= 0) return;
        float edge = FillWidth(MovingEdge);
        float fill = FillWidth(health);
        if (progress < 1)
        {
            // The changed cells light briefly. A bright leading edge follows the cell mask.
            float trail = healing ? fill : Mathf.Max(fill, edge);
            Color afterglow = Color.Lerp(palette.glass, signal, healing ? .30f : .55f);
            BarSlice(mesh, y, 0, trail, afterglow);
            if (healing) fill = Mathf.Min(fill, edge);
        }
        BarSlice(mesh, y, 0, fill, signal);
        if (progress < 1)
        {
            float glow = Mathf.Sin(Mathf.PI * progress);
            float tip = healing ? fill : Mathf.Max(fill, edge);
            // The last live cell catches light even on a one-point tick.
            BarSlice(mesh, y, Mathf.Max(0, tip - Mathf.Lerp(5, 15, impact)), tip,
                Color.Lerp(signal, palette.lettering, glow * .85f));
            if (healing && impact > .25f)
            {
                float sweep = Mathf.Lerp(FillWidth(startingEdge), fill, progress);
                BarSlice(mesh, y, Mathf.Max(0, sweep - 6), Mathf.Min(fill, sweep),
                    Color.Lerp(signal, palette.lettering, glow * .46f));
            }
        }
    }

    private void DrawBacking(VertexHelper mesh)
    {
        const float corner = 8f;
        float left = -BackingPadding, right = rectTransform.rect.width + BackingPadding;
        float bottom = -BackingPadding, top = rectTransform.rect.height + BackingPadding;
        var uv = new Vector2(1023.5f / options.width, 1f - 1023.5f / options.height);
        Color32 tint = palette.glass;
        int first = mesh.currentVertCount;
        mesh.AddVert(new Vector2(left, bottom + corner), tint, uv);
        mesh.AddVert(new Vector2(left, top - corner), tint, uv);
        mesh.AddVert(new Vector2(left + corner, top), tint, uv);
        mesh.AddVert(new Vector2(right - corner, top), tint, uv);
        mesh.AddVert(new Vector2(right, top - corner), tint, uv);
        mesh.AddVert(new Vector2(right, bottom + corner), tint, uv);
        mesh.AddVert(new Vector2(right - corner, bottom), tint, uv);
        mesh.AddVert(new Vector2(left + corner, bottom), tint, uv);
        for (int i = 1; i < 7; ++i)
            mesh.AddTriangle(first, first + i, first + i + 1);
    }

    private float FillWidth(float percent)
    {
        if (percent <= 0) return 0;
        if (percent >= 100) return 168;
        if (barStyle == VitalsBar.MemoryBlocks)
        {
            int cells = Mathf.Clamp(Mathf.RoundToInt(18 * percent / 100f), 1, 17);
            return 2.1f + 9.06f * cells;
        }
        return 4 + 160 * (percent / 100f);
    }

    private Rect TrackRect
    {
        get
        {
            // The block rail has three transparent texels above and below its 34-texel frame.
            float overscan = barStyle == VitalsBar.MemoryBlocks ? RowHeight * 3f / 34f : 0f;
            return new Rect(ReadoutLeft, Inset + RowHeight + Gutter - overscan,
                rectTransform.rect.width - Inset - ReadoutLeft, RowHeight + 2f * overscan);
        }
    }

    private void BarSlice(VertexHelper mesh, int y, float left, float right, Color tint)
    {
        if (right <= left) return;
        float width = right - left;
        Rect slice = TrackRect;
        float scale = slice.width / 168f;
        slice.x += left * scale;
        slice.width = width * scale;
        Quad(mesh, slice, new Rect(514 + 3 * left, y, 3 * width, 60), tint);
    }

    private void Quad(VertexHelper mesh, Rect rect, Rect source, Color32 color, Color32? upperColor = null)
    {
        float l = source.xMin / options.width, r = source.xMax / options.width;
        float t = 1 - source.yMin / options.height, b = 1 - source.yMax / options.height;
        int first = mesh.currentVertCount;
        mesh.AddVert(new Vector2(rect.xMin, rect.yMin), color, new Vector2(l, b));
        mesh.AddVert(new Vector2(rect.xMin, rect.yMax), upperColor ?? color, new Vector2(l, t));
        mesh.AddVert(new Vector2(rect.xMax, rect.yMax), upperColor ?? color, new Vector2(r, t));
        mesh.AddVert(new Vector2(rect.xMax, rect.yMin), color, new Vector2(r, b));
        mesh.AddTriangle(first, first + 1, first + 2);
        mesh.AddTriangle(first + 2, first + 3, first);
    }
}

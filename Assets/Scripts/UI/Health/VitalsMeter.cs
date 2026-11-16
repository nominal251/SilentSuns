using UnityEngine;
using UnityEngine.UI;

public enum VitalsFace { Terminal, SideEye, Cursor, SevenSegment, Cyclops, Moon, Brackets, SplitMask, Spark, Crooked }

[RequireComponent(typeof(CanvasRenderer))]
public sealed class VitalsMeter : MaskableGraphic
{
    // Dimensions are in canvas units, relative to the lower-left panel corner.
    private const float Gutter = 6f;
    private const float Inset = 15f;
    private const float FaceWidth = 48f;
    private const float FaceHeight = 42.4f;
    private const float ReadoutLeft = Inset + FaceWidth + Gutter;

    [SerializeField] private VitalsPalette palette;
    [SerializeField] private Texture2D options;
    [SerializeField] private VitalsFace faceStyle = VitalsFace.Terminal;
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
    private bool recoveringFace;
    private Color recoveryStartColor;
    private float impact;
    private bool faceLifts;
    private float faceStrength;
    private float blink;
    private float pulse;
    private float fullHealthShimmer;
    private float idleClock;
    private bool heavyHit;
    private HealthCondition hitFrom;
    private float hitDuration;
    private float hitHold;
    private float lightHitCooldown;
    private Vector2 readoutVelocity;
    public Vector2 ReadoutOffset { get; private set; }
    public Color SignalColor
    {
        get
        {
            Color target = palette.Signal(condition);
            return recoveringFace
                ? Color.Lerp(recoveryStartColor, target, Mathf.SmoothStep(0, 1, faceProgress))
                : Color.Lerp(target, palette.glass, .16f * pulse);
        }
    }
    public override Texture mainTexture => options ? options : base.mainTexture;

    private float MovingEdge => Mathf.Lerp(health, startingEdge, (1 - progress) * (1 - progress));
    private HealthCondition VisibleFace => !recoveringFace && faceProgress < .20f ? outgoingFace : condition;

    public void SetReading(float value, HealthCondition state)
    {
        if (hasReading && value == health && state == condition) return;
        startingEdge = MovingEdge;
        float change = Mathf.Abs(value - health);
        bool changedCondition = state != condition;
        bool animate = hasReading && value > 0 && !SilentSuns.Menu.GamePreferences.ReducedMotion;
        healing = value > health;
        if (change > 0 || !animate)
            fullHealthShimmer = animate && healing && value >= 100f ? 1f : 0f;
        if (!animate || healing) hitHold = lightHitCooldown = 0;
        else if (change > 0 && faceStyle == VitalsFace.Terminal &&
                 (change >= heavyHitDamage || changedCondition || (hitHold <= 0 && lightHitCooldown <= 0)))
        {
            hitFrom = condition;
            heavyHit = change >= heavyHitDamage;
            hitDuration = reactionSeconds * (heavyHit ? 1.2f : .60f);
            bandHit = changedCondition ? SelectBandHit(condition, state, heavyHit) : null;
            if (bandHit != null)
                hitDuration = bandHit[bandHit.Length - 1].EndFrame / 30f * (reactionSeconds / .56f);
            hitHold = hitDuration;
            lightHitCooldown = hitDuration + .08f;
        }
        if (!hasReading || SilentSuns.Menu.GamePreferences.ReducedMotion)
        {
            ReadoutOffset = readoutVelocity = Vector2.zero;
        }
        else if (change > 0 && !healing)
        {
            float strength = Mathf.Clamp01(change / 50f);
            float kick = Mathf.Min(1, change) * Mathf.Lerp(40, 168, strength);
            readoutVelocity.x = Mathf.Max(-172, readoutVelocity.x - kick);
            readoutVelocity.y = Mathf.Max(-36, readoutVelocity.y - kick * .18f);
        }
        impact = Mathf.InverseLerp(0, 24, change);
        progress = animate && change > 0 ? 0 : 1;
        bool continuingRecovery = recoveringFace && faceProgress < 1 && healing && !changedCondition;
        if (!animate || changedCondition || (!continuingRecovery && (change >= 6 || recoveringFace && !healing)))
        {
            outgoingFace = VisibleFace;
            bool beginRecovery = animate && healing && faceStyle == VitalsFace.Terminal &&
                ((condition == HealthCondition.Danger && state == HealthCondition.Caution) ||
                 (condition == HealthCondition.Caution && state == HealthCondition.Good));
            if (beginRecovery)
            {
                recoveryStartColor = SignalColor;
                outgoingFace = condition;
            }
            recoveringFace = beginRecovery;
            faceStrength = changedCondition ? 1 : impact;
            faceProgress = animate ? 0 : 1;
            faceLifts = healing;
            idleClock = blink = pulse = 0;
        }
        if (hitHold > 0)
        {
            outgoingFace = state;
            faceStrength = 0;
        }
        health = value;
        condition = state;
        hasReading = true;
        SetVerticesDirty();
    }

    protected override void OnDisable()
    {
        hasReading = false;
        fullHealthShimmer = 0;
        recoveringFace = false;
        progress = faceProgress = 1;
        idleClock = blink = pulse = 0;
        hitHold = lightHitCooldown = 0;
        ReadoutOffset = readoutVelocity = Vector2.zero;
        base.OnDisable();
    }

    private void Update()
    {
        if (!hasReading) return;
        bool moving = progress < 1 || faceProgress < 1 || hitHold > 0 || fullHealthShimmer > 0;
        Vector2 oldOffset = ReadoutOffset;
        float oldBlink = blink, oldPulse = pulse;
        blink = pulse = 0;
        if (SilentSuns.Menu.GamePreferences.ReducedMotion)
        {
            fullHealthShimmer = 0;
            recoveringFace = false;
            progress = faceProgress = 1;
            hitHold = lightHitCooldown = 0;
            ReadoutOffset = readoutVelocity = Vector2.zero;
        }
        else
        {
            AdvanceReadout(Time.deltaTime);
            fullHealthShimmer = Mathf.MoveTowards(fullHealthShimmer, 0, Time.deltaTime / .85f);
            hitHold = Mathf.Max(0, hitHold - Time.deltaTime);
            lightHitCooldown = Mathf.Max(0, lightHitCooldown - Time.deltaTime);
            float dt = Time.deltaTime / Mathf.Max(.1f, reactionSeconds);
            progress = Mathf.Min(1, progress + dt / Mathf.Lerp(.42f, 1, impact));
            faceProgress = Mathf.Min(1, faceProgress + dt);
            if (faceProgress >= 1) recoveringFace = false;
            idleClock += Time.deltaTime;
            if (health > 0 && faceProgress >= 1)
            {
                if (condition == HealthCondition.Good && faceStyle == VitalsFace.Terminal)
                    blink = Mathf.Clamp01(1 - Mathf.Abs(Mathf.Repeat(idleClock, 4.1f) - 3.65f) / .105f);
                else if (condition == HealthCondition.Danger)
                {
                    float phase = Mathf.Repeat((idleClock - reactionSeconds) / 2.4f, 1f);
                    pulse = .5f - .5f * Mathf.Cos(phase * 2 * Mathf.PI);
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
        Solid(mesh, new Rect(ReadoutLeft, Inset, 132, 23), palette.frame);
        bool framed = faceStyle == VitalsFace.Terminal;
        var plate = new Rect(Inset, Inset, FaceWidth, FaceHeight);
        if (framed) DrawFaceplate(mesh, plate);

        // Pinch the outgoing expression, then open the new one with a short rebound.
        float pinch = faceStrength * (faceProgress < .20f ? faceProgress / .20f : 1 - Mathf.InverseLerp(.20f, .47f, faceProgress));
        float rebound = faceStrength * Mathf.Sin(Mathf.PI * Mathf.InverseLerp(.20f, .85f, faceProgress));
        if (recoveringFace) pinch = rebound = 0;
        float w = 53 + 6 * pinch - 2 * rebound;
        float h = 40 - (hitHold > 0 ? 10 : 23) * pinch + 4 * rebound;
        if (framed) { w *= .82f; h *= .82f; }
        float dx = faceLifts ? 0 : -4 * pinch + 2 * rebound;
        float dy = faceLifts ? 4 * rebound : -2 * pinch;
        var icon = new Rect(plate.center.x + dx - w / 2, plate.center.y + dy - h / 2, w, h);
        var expression = VisibleFace;
        int row = expression == HealthCondition.Good ? 0 : expression == HealthCondition.Caution ? 1 : 2;
        var source = new Rect(2 + 100 * (int)faceStyle, 2 + 76 * row, 96, 72);
        if (framed && !recoveringFace && outgoingFace != condition && faceProgress >= .20f && faceProgress < .80f)
        {
            bool nearDanger = outgoingFace == HealthCondition.Danger || condition == HealthCondition.Danger;
            source = new Rect(nearDanger ? 304 : 204, 912, 96, 72);
        }
        Color ink = framed ? palette.glass : Color.Lerp(palette.Signal(expression), palette.lettering, .22f * pulse);
        if (hitHold > 0 && faceStyle == VitalsFace.Terminal)
        {
            DrawHitFace(mesh, icon, source, ink);
        }
        else if (recoveringFace && condition == HealthCondition.Caution)
        {
            // At the default reaction speed the left eye leads by nine frames at 30 fps.
            DrawRecoveringEye(mesh, icon, false, faceProgress, ink);
            DrawRecoveringEye(mesh, icon, true, faceProgress - .54f, ink);
            Quad(mesh, new Rect(icon.x, icon.y, w, h * 10 / 24), new Rect(2, 120, 96, 30), ink);
        }
        else if (recoveringFace && faceProgress < .72f)
        {
            // Three pixel poses take the brackets through a flat stroke into an arch.
            int pose = Mathf.Min(2, Mathf.FloorToInt(faceProgress / .24f));
            Quad(mesh, new Rect(icon.x, icon.yMax - h * .5f, w, h / 3),
                new Rect(920, 912 + pose * 24, 96, 24), ink);
            Quad(mesh, new Rect(icon.x, icon.y, w, h * .5f), new Rect(2, 38, 96, 36), ink);
        }
        else if (blink > 0)
        {
            float half = icon.height * .5f;
            float eyelid = half * (1 - .84f * blink);
            Quad(mesh, new Rect(icon.x, icon.y + half + (half - eyelid) * .45f, w, eyelid),
                new Rect(source.x, source.y, 96, 36), ink);
            Quad(mesh, new Rect(icon.x, icon.y, w, half), new Rect(source.x, source.y + 36, 96, 36), ink);
        }
        else Quad(mesh, icon, source, ink);

        DrawSegments(mesh);
    }

    private void DrawSegments(VertexHelper mesh)
    {
        Rect track = new Rect(ReadoutLeft, 44, rectTransform.rect.width - ReadoutLeft - Inset, 14);
        Solid(mesh, track, palette.frame);
        Rect inner = new Rect(track.x + 1, track.y + 1, track.width - 2, track.height - 2);
        Solid(mesh, inner, palette.glass);
        int filled = Mathf.Clamp(Mathf.RoundToInt(health * 18 / 100), health > 0 ? 1 : 0, 18);
        int moving = Mathf.Clamp(Mathf.RoundToInt(MovingEdge * 18 / 100), 0, 18);
        float width = (inner.width - 2 - 17) / 18;
        for (int i = 0; i < 18; i++)
        {
            Rect cell = new Rect(inner.x + 1 + i * (width + 1), inner.y + 1, width, inner.height - 2);
            bool lit = i < (healing ? Mathf.Min(filled, moving) : filled);
            Color tint = lit ? palette.lettering : palette.emptyCell;
            if (!lit && i < Mathf.Max(filled, moving) && progress < 1)
                tint = Color.Lerp(palette.emptyCell, palette.lettering, healing ? .35f : .55f);
            if (lit && fullHealthShimmer > 0)
            {
                float distance = Mathf.Abs(i - (1 - fullHealthShimmer) * 22 + 2);
                tint = Color.Lerp(tint, Color.white, Mathf.Clamp01(1 - distance / 2) * .7f);
            }
            Solid(mesh, cell, tint);
            if (lit) continue;
            // Short diagonals stay inside their cell even when its width changes.
            for (float line = -cell.height; line < cell.width; line += 3)
            {
                float from = Mathf.Max(0, -line);
                float to = Mathf.Min(cell.height, cell.width - line);
                if (to <= from) continue;
                Vector2 a = new Vector2(cell.x + line + from, cell.y + from);
                Vector2 b = new Vector2(cell.x + line + to, cell.y + to);
                Vector2 edge = new Vector2(.28f, -.28f);
                AddSolidQuad(mesh, a, b, b + edge, a + edge, palette.glass);
            }
        }
    }

    private void DrawHitFace(VertexHelper mesh, Rect icon, Rect destination, Color ink)
    {
        float phase = 1 - hitHold / hitDuration;
        if (bandHit != null)
        {
            DrawBandHit(mesh, icon, phase, ink);
            return;
        }
        Rect impactPose = new Rect(heavyHit ? 102 : 2, 912, 96, 72);
        Rect previous = new Rect(2, hitFrom == HealthCondition.Good ? 2 : hitFrom == HealthCondition.Caution ? 78 : 154, 96, 72);
        Rect leftEye, rightEye, mouth;
        if (heavyHit)
        {
            // Brace, go wide-eyed, then let one eye refocus before the other.
            Rect stunned = new Rect(920, 985, 96, 39);
            leftEye = phase < .30f ? impactPose : phase < .63f ? stunned : destination;
            rightEye = phase < .30f ? impactPose : phase < .80f ? stunned : destination;
            mouth = phase < .63f ? impactPose : destination;
        }
        else
        {
            // The near eye winces while the far eye registers the new condition.
            leftEye = phase < .66f ? impactPose : destination;
            rightEye = phase < .30f ? previous : destination;
            mouth = phase < .30f ? impactPose : phase < .66f ? new Rect(204, 912, 96, 72) : destination;
        }
        float halfWidth = icon.width * .5f;
        float eyeHeight = icon.height * 13 / 24f;
        float eyeY = icon.yMax - eyeHeight;
        Quad(mesh, new Rect(icon.x, eyeY, halfWidth, eyeHeight), new Rect(leftEye.x, leftEye.y, 48, 39), ink);
        Quad(mesh, new Rect(icon.x + halfWidth, eyeY, halfWidth, eyeHeight), new Rect(rightEye.x + 48, rightEye.y, 48, 39), ink);
        Quad(mesh, new Rect(icon.x, icon.y, icon.width, icon.height - eyeHeight), new Rect(mouth.x, mouth.y + 39, 96, 33), ink);
    }

    private enum HitMark { Smile, Bracket, Cross, Wince, Brace, Stare, Flat, Corner, OpenMouth }

    private static Vector2 HitSprite(HitMark mark) => mark switch
    {
        HitMark.Smile => new Vector2(2, 2),
        HitMark.Bracket => new Vector2(2, 78),
        HitMark.Cross => new Vector2(2, 154),
        HitMark.Wince => new Vector2(2, 912),
        HitMark.Brace => new Vector2(102, 912),
        HitMark.Stare => new Vector2(920, 985),
        HitMark.Flat => new Vector2(204, 912),
        HitMark.Corner => new Vector2(2, 229),
        HitMark.OpenMouth => new Vector2(98, 229),
        _ => Vector2.zero
    };

    private readonly struct HitPose
    {
        public readonly int EndFrame;
        public readonly HitMark Left, Right, Mouth;

        public HitPose(int endFrame, HitMark left, HitMark right, HitMark mouth)
        {
            EndFrame = endFrame;
            Left = left;
            Right = right;
            Mouth = mouth;
        }
    }

    // Hold endpoints from the 30 fps studies. reactionSeconds scales the entire sequence.
    private static readonly HitPose[] LightWince = {
        new(3, HitMark.Wince, HitMark.Smile, HitMark.Wince),
        new(7, HitMark.Wince, HitMark.Bracket, HitMark.Flat),
        new(10, HitMark.Corner, HitMark.Bracket, HitMark.Bracket)
    };
    private static readonly HitPose[] HeavyWince = {
        new(5, HitMark.Brace, HitMark.Brace, HitMark.Brace),
        new(10, HitMark.Wince, HitMark.Stare, HitMark.Brace),
        new(15, HitMark.Wince, HitMark.Bracket, HitMark.OpenMouth),
        new(19, HitMark.Corner, HitMark.Bracket, HitMark.Bracket)
    };
    private static readonly HitPose[] LightCross = {
        new(3, HitMark.Wince, HitMark.Bracket, HitMark.Wince),
        new(7, HitMark.Cross, HitMark.Bracket, HitMark.Flat),
        new(10, HitMark.Cross, HitMark.Corner, HitMark.Cross)
    };
    private static readonly HitPose[] HeavyCross = {
        new(5, HitMark.Brace, HitMark.Brace, HitMark.Brace),
        new(10, HitMark.Stare, HitMark.Stare, HitMark.Brace),
        new(15, HitMark.Cross, HitMark.Stare, HitMark.OpenMouth),
        new(19, HitMark.Cross, HitMark.Corner, HitMark.Cross)
    };
    private static readonly HitPose[] BraceAndStare = {
        new(5, HitMark.Brace, HitMark.Brace, HitMark.Brace),
        new(12, HitMark.Stare, HitMark.Stare, HitMark.Brace),
        new(16, HitMark.Cross, HitMark.Stare, HitMark.Cross)
    };
    private HitPose[] bandHit;

    private static HitPose[] SelectBandHit(HealthCondition from, HealthCondition to, bool heavy)
    {
        if (from == HealthCondition.Good && to == HealthCondition.Danger) return BraceAndStare;
        if (to == HealthCondition.Caution) return heavy ? HeavyWince : LightWince;
        if (to == HealthCondition.Danger) return heavy ? HeavyCross : LightCross;
        return null;
    }

    private void DrawBandHit(VertexHelper mesh, Rect icon, float phase, Color ink)
    {
        HitPose pose = bandHit[bandHit.Length - 1];
        float frame = phase * pose.EndFrame;
        foreach (HitPose candidate in bandHit)
        {
            if (frame >= candidate.EndFrame) continue;
            pose = candidate;
            break;
        }
        DrawBandHitEye(mesh, icon, pose.Left, false, ink);
        DrawBandHitEye(mesh, icon, pose.Right, true, ink);
        HitMark mouth = pose.Mouth;
        Vector2 uv = HitSprite(mouth);
        if (mouth == HitMark.OpenMouth)
            Quad(mesh, new Rect(icon.x, icon.y + icon.height * 4 / 24, icon.width, icon.height * 4 / 24),
                new Rect(uv.x, uv.y, 96, 12), ink);
        else
            Quad(mesh, new Rect(icon.x, icon.y, icon.width, icon.height * 11 / 24),
                new Rect(uv.x, uv.y + 39, 96, 33), ink);
    }

    private void DrawBandHitEye(VertexHelper mesh, Rect icon, HitMark mark, bool right, Color ink)
    {
        Vector2 uv = HitSprite(mark);
        float half = icon.width * .5f;
        int top = mark == HitMark.Corner ? 4 : 0;
        int rows = mark == HitMark.Corner ? 6 : 13;
        Quad(mesh, new Rect(icon.x + (right ? half : 0), icon.yMax - icon.height * (top + rows) / 24,
                half, icon.height * rows / 24), new Rect(uv.x + (right ? 48 : 0), uv.y, 48, rows * 3), ink);
    }

    private void DrawRecoveringEye(VertexHelper mesh, Rect icon, bool right, float phase, Color ink)
    {
        // Face atlas poses use a 32 x 24 design grid at 3x resolution.
        bool critical = phase < 0;
        bool corner = !critical && phase < .18f;
        int startRow = corner ? 7 : 0;
        int height = corner ? 6 : 14;
        float pixel = icon.height / 24f;
        float top = corner ? 5.5f : 0;
        float halfWidth = icon.width * .5f;
        var rect = new Rect(icon.x + (right ? halfWidth : 0), icon.yMax - (top + height) * pixel,
            halfWidth, height * pixel);
        var sprite = new Rect(right ? 50 : 2, (critical ? 154 : 78) + startRow * 3, 48, height * 3);
        Quad(mesh, rect, sprite, ink);
    }
    private void DrawBacking(VertexHelper mesh)
    {
        Rect panel = new Rect(0, 0, rectTransform.rect.width, rectTransform.rect.height);
        Solid(mesh, panel, palette.glass);
        const float margin = 8, rule = 1;
        float right = panel.width - margin, top = panel.height - 14;
        Solid(mesh, new Rect(margin, margin, right - margin, rule), palette.frame);
        Solid(mesh, new Rect(margin, margin, rule, top - margin), palette.frame);
        Solid(mesh, new Rect(right - rule, margin, rule, top - margin), palette.frame);
        Solid(mesh, new Rect(margin, top, ReadoutLeft - margin - 4, rule), palette.frame);
        Solid(mesh, new Rect(ReadoutLeft + 52, top, right - ReadoutLeft - 52, rule), palette.frame);
    }

    private void DrawFaceplate(VertexHelper mesh, Rect plate)
    {
        float corner = plate.width * .1337f;
        Vector2 uv = new Vector2(1023.5f / options.width, 1 - 1023.5f / options.height);
        int first = mesh.currentVertCount;
        mesh.AddVert(new Vector2(plate.x, plate.y + corner), palette.lettering, uv);
        mesh.AddVert(new Vector2(plate.x, plate.yMax - corner), palette.lettering, uv);
        mesh.AddVert(new Vector2(plate.x + corner, plate.yMax), palette.lettering, uv);
        mesh.AddVert(new Vector2(plate.xMax - corner, plate.yMax), palette.lettering, uv);
        mesh.AddVert(new Vector2(plate.xMax, plate.yMax - corner), palette.lettering, uv);
        mesh.AddVert(new Vector2(plate.xMax, plate.y + corner), palette.lettering, uv);
        mesh.AddVert(new Vector2(plate.xMax - corner, plate.y), palette.lettering, uv);
        mesh.AddVert(new Vector2(plate.x + corner, plate.y), palette.lettering, uv);
        for (int i = 1; i < 7; i++) mesh.AddTriangle(first, first + i, first + i + 1);
    }

    private void Solid(VertexHelper mesh, Rect rect, Color tint)
    {
        AddSolidQuad(mesh, new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMin, rect.yMax),
            new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMax, rect.yMin), tint);
    }

    private void AddSolidQuad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 tint)
    {
        Vector2 uv = new Vector2(1023.5f / options.width, 1 - 1023.5f / options.height);
        int first = mesh.currentVertCount;
        mesh.AddVert(a, tint, uv);
        mesh.AddVert(b, tint, uv);
        mesh.AddVert(c, tint, uv);
        mesh.AddVert(d, tint, uv);
        mesh.AddTriangle(first, first + 1, first + 2);
        mesh.AddTriangle(first, first + 2, first + 3);
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

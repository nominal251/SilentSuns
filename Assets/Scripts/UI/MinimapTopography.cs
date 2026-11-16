using UnityEngine;
using UnityEngine.UI;

// Samples the level's collision surfaces and draws height lines behind the map markers.
// This is a top-down height map, not a pathfinder. Caves and stacked floors need a separate map.
public sealed class MinimapTopography
{
    private const int GridSize = 81;
    private const int ImageSize = 320;
    private const int SamplesPerFrame = 256;
    private readonly float radius, halfWidth, step, contourSpacing, scanHeight;
    private readonly int layers;
    private readonly RawImage image;
    private readonly Texture2D texture;
    private readonly float[] heights = new float[GridSize * GridSize];
    private readonly BoxCollider[] boxesAtSamples = new BoxCollider[GridSize * GridSize];
    private readonly RaycastHit[] hits = new RaycastHit[64];
    private readonly Color32[] pixels = new Color32[ImageSize * ImageSize];
    private readonly Vector2[] crossings = new Vector2[4];
    private Vector3 scanCenter, drawnCenter;
    private int nextSample = -1;
    private float nextRefresh;
    private bool hasMap;
    private Collider[] nearbyColliders;

    public MinimapTopography(RectTransform parent, Sprite circle, Vector2 center, float plotRadius,
        float worldRadius, LayerMask groundLayers, float heightInterval, float heightRange)
    {
        radius = Mathf.Max(1, worldRadius);
        halfWidth = radius * 1.5f;
        step = halfWidth * 2 / (GridSize - 1);
        contourSpacing = Mathf.Max(.5f, heightInterval);
        scanHeight = Mathf.Max(10, heightRange);
        layers = groundLayers & ~LayerMask.GetMask("UI", "2DScreen", "Ignore Raycast");

        // The square height image rotates inside a circular window, just like the markers.
        var window = new GameObject("Topography window", typeof(RectTransform), typeof(Image), typeof(Mask));
        window.transform.SetParent(parent, false);
        window.layer = parent.gameObject.layer;
        var rect = window.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = Vector2.one * .5f;
        rect.anchoredPosition = center;
        rect.sizeDelta = Vector2.one * plotRadius * 2;
        var maskImage = window.GetComponent<Image>();
        maskImage.sprite = circle;
        maskImage.raycastTarget = false;
        window.GetComponent<Mask>().showMaskGraphic = false;
        image = new GameObject("Ground contours", typeof(RawImage)).GetComponent<RawImage>();
        image.transform.SetParent(rect, false);
        image.gameObject.layer = parent.gameObject.layer;
        image.rectTransform.sizeDelta = Vector2.one * plotRadius * 3;
        image.raycastTarget = false;
        texture = new Texture2D(ImageSize, ImageSize, TextureFormat.RGBA32, false)
        {
            name = "Local ground heights", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixels32(pixels);
        texture.Apply(false);
        image.texture = texture;
    }

    public void Update(Vector3 position, float heading)
    {
        if (nextSample < 0 && (!hasMap || Time.unscaledTime >= nextRefresh ||
            Vector3.Distance(position, drawnCenter) > radius * .1f))
            BeginScan(position);
        // Do not leave an old patch visible after teleporting to another part of the level.
        if (Vector3.Distance(position, hasMap ? drawnCenter : scanCenter) > radius * 2)
        {
            image.enabled = false;
            BeginScan(position);
        }
        if (nextSample >= 0)
        {
            // Spread physics checks over several frames. Turning only rotates the cached picture.
            int end = Mathf.Min(heights.Length, nextSample + SamplesPerFrame);
            for (; nextSample < end; nextSample++)
            {
                int x = nextSample % GridSize, z = nextSample / GridSize;
                Vector3 point = scanCenter + new Vector3(x * step - halfWidth, 0, z * step - halfWidth);
                heights[nextSample] = SampleSurface(point, out boxesAtSamples[nextSample]);
            }
            if (nextSample == heights.Length)
            {
                DrawContours();
                drawnCenter = scanCenter;
                hasMap = image.enabled = true;
                nextSample = -1;
                nextRefresh = Time.unscaledTime + 3;
            }
        }
        Vector3 offset = drawnCenter - position;
        float yaw = heading * Mathf.Deg2Rad;
        float scale = image.rectTransform.sizeDelta.x / (halfWidth * 2);
        image.rectTransform.anchoredPosition = new Vector2(offset.x * Mathf.Cos(yaw) - offset.z * Mathf.Sin(yaw),
            offset.x * Mathf.Sin(yaw) + offset.z * Mathf.Cos(yaw)) * scale;
        image.rectTransform.localRotation = Quaternion.Euler(0, 0, heading);
    }

    private void BeginScan(Vector3 position)
    {
        // Keep the grid tied to the world. Walking must not move the sample points across a cube's edges.
        scanCenter = new Vector3(Mathf.Round(position.x / step) * step, position.y,
            Mathf.Round(position.z / step) * step);
        // Include small blocks even when they fit between height samples.
        nearbyColliders = Physics.OverlapBox(scanCenter, new Vector3(halfWidth, scanHeight, halfWidth),
            Quaternion.identity, layers, QueryTriggerInteraction.Ignore);
        nextSample = 0;
    }

    private float SampleHeight(Vector3 position)
    {
        return SampleSurface(position, out _);
    }

    private float SampleSurface(Vector3 position, out BoxCollider box)
    {
        box = null;
        int count = Physics.RaycastNonAlloc(position + Vector3.up * scanHeight, Vector3.down,
            hits, scanHeight * 2, layers, QueryTriggerInteraction.Ignore);
        // A full hit buffer might have missed the top surface. Leave that sample unknown.
        if (count == hits.Length) return float.NaN;
        float highest = float.NaN;
        for (int i = 0; i < count; i++)
        {
            // Players, enemies and held props must not become hills on the map.
            if (hits[i].collider.attachedRigidbody != null) continue;
            float y = hits[i].point.y;
            if (float.IsNaN(highest) || y > highest)
            {
                highest = y;
                box = UprightBox(hits[i].collider);
            }
        }
        // Physics can put a flat floor just above or below zero by a tiny amount.
        // Keep millimetre precision so that noise doesn't turn into fake contour lines.
        return float.IsNaN(highest) ? highest : Mathf.Round(highest * 1000f) / 1000f;
    }

    private static BoxCollider UprightBox(Collider collider)
    {
        // Sloping boxes still need height contours. Flat-topped blocks get their real straight edges.
        var box = collider as BoxCollider;
        return box != null && box.attachedRigidbody == null &&
            Mathf.Abs(Vector3.Dot(box.transform.up, Vector3.up)) > .9999f ? box : null;
    }

    private void DrawContours()
    {
        System.Array.Clear(pixels, 0, pixels.Length);
        // Four pixels per grid cell keeps overlapping scans on the same pixel grid too.
        float pixelStep = (float)ImageSize / (GridSize - 1);
        for (int z = 0; z < GridSize - 1; z++)
        for (int x = 0; x < GridSize - 1; x++)
        {
            float a = heights[z * GridSize + x], b = heights[z * GridSize + x + 1];
            float c = heights[(z + 1) * GridSize + x + 1], d = heights[(z + 1) * GridSize + x];
            // Missing ground stays blank; never bridge a gap with an invented surface.
            if (float.IsNaN(a) || float.IsNaN(b) || float.IsNaN(c) || float.IsNaN(d)) continue;
            float low = Mathf.Min(Mathf.Min(a, b), Mathf.Min(c, d));
            float high = Mathf.Max(Mathf.Max(a, b), Mathf.Max(c, d));
            float slope = Mathf.Max(Mathf.Max(Mathf.Abs(a - b), Mathf.Abs(b - c)),
                Mathf.Max(Mathf.Abs(c - d), Mathf.Abs(d - a))) / step;
            bool steep = slope > 1f;
            Vector2 origin = new Vector2(x, z) * pixelStep - Vector2.one * .5f;
            // A faint fill separates height bands; amber edges call out sharp changes in ground height.
            byte shade = (byte)(18 + Mathf.PingPong(Mathf.Floor((a + b + c + d) / 4 / contourSpacing), 4) * 3);
            for (int py = Mathf.Max(0, Mathf.CeilToInt(origin.y)); py <= Mathf.Min(ImageSize - 1, Mathf.FloorToInt(origin.y + pixelStep)); py++)
            for (int px = Mathf.Max(0, Mathf.CeilToInt(origin.x)); px <= Mathf.Min(ImageSize - 1, Mathf.FloorToInt(origin.x + pixelStep)); px++)
                if (pixels[py * ImageSize + px].a == 0) pixels[py * ImageSize + px] = new Color32(130, 142, 120, shade);

            // Joining a block's roof to the ground makes rounded, nested "hill" lines.
            // Draw that block from its collider below instead of inventing a slope between samples.
            if (boxesAtSamples[z * GridSize + x] != null || boxesAtSamples[z * GridSize + x + 1] != null ||
                boxesAtSamples[(z + 1) * GridSize + x] != null || boxesAtSamples[(z + 1) * GridSize + x + 1] != null)
                continue;

            float first = Mathf.Ceil(low / contourSpacing) * contourSpacing;
            float interval = Mathf.Max(contourSpacing, Mathf.Ceil((high - low) / (32 * contourSpacing)) * contourSpacing);
            for (float height = first; height < high; height += interval)
            {
                int count = 0;
                AddCrossing(a, b, height, origin, origin + Vector2.right * pixelStep, ref count);
                AddCrossing(b, c, height, origin + Vector2.right * pixelStep, origin + Vector2.one * pixelStep, ref count);
                AddCrossing(c, d, height, origin + Vector2.one * pixelStep, origin + Vector2.up * pixelStep, ref count);
                AddCrossing(d, a, height, origin + Vector2.up * pixelStep, origin, ref count);
                bool major = Mathf.RoundToInt(height / contourSpacing) % 5 == 0;
                Color32 color = steep ? new Color32(186, 143, 75, 190) : new Color32(166, 175, 139, (byte)(major ? 160 : 95));
                for (int i = 0; i + 1 < count; i += 2) DrawLine(crossings[i], crossings[i + 1], color);
            }
        }
        DrawBoxEdges();
        texture.SetPixels32(pixels);
        texture.Apply(false);
    }

    private Vector2 WorldToPixel(Vector3 point)
    {
        return new Vector2(point.x - scanCenter.x + halfWidth, point.z - scanCenter.z + halfWidth) *
            (ImageSize / (halfWidth * 2)) - Vector2.one * .5f;
    }

    private void DrawBoxEdges()
    {
        foreach (var collider in nearbyColliders)
        {
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy) continue;
            var box = UprightBox(collider);
            if (box == null) continue;
            var half = box.size * .5f;
            var center = box.transform.TransformPoint(box.center);
            for (int side = 0; side < 4; side++)
            {
                // Local corners preserve the box's rotation and scale; world bounds would not.
                Vector3 Corner(int index) => box.transform.TransformPoint(box.center + new Vector3(
                    index == 0 || index == 3 ? -half.x : half.x, 0, index < 2 ? -half.z : half.z));
                Vector2 start = WorldToPixel(Corner(side)), end = WorldToPixel(Corner((side + 1) % 4));
                if (!ClipLine(ref start, ref end)) continue;
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(start, end)));
                for (int i = 0; i <= steps; i++)
                {
                    Vector2 pixel = Vector2.Lerp(start, end, (float)i / steps);
                    Vector3 point = scanCenter + new Vector3((pixel.x + .5f) / ImageSize * halfWidth * 2 - halfWidth,
                        0, (pixel.y + .5f) / ImageSize * halfWidth * 2 - halfWidth);
                    // Look just inside the edge. Hidden blocks should not show through a roof above them.
                    Vector3 inward = new Vector3(center.x - point.x, 0, center.z - point.z).normalized * .01f;
                    SampleSurface(point + inward, out var visibleBox);
                    if (visibleBox == box) DrawLine(pixel, pixel, new Color32(186, 143, 75, 190));
                }
            }
        }
    }

    private static bool ClipLine(ref Vector2 start, ref Vector2 end)
    {
        // Trim lines to the texture before drawing, rather than piling off-screen pixels onto its border.
        Vector2 delta = end - start;
        float first = 0, last = 1;
        for (int axis = 0; axis < 2; axis++)
        {
            if (Mathf.Abs(delta[axis]) < .00001f)
            {
                if (start[axis] < 0 || start[axis] > ImageSize - 1) return false;
                continue;
            }
            float a = -start[axis] / delta[axis], b = (ImageSize - 1 - start[axis]) / delta[axis];
            first = Mathf.Max(first, Mathf.Min(a, b));
            last = Mathf.Min(last, Mathf.Max(a, b));
            if (first > last) return false;
        }
        end = start + delta * last;
        start += delta * first;
        return true;
    }

    private void AddCrossing(float a, float b, float height, Vector2 start, Vector2 end, ref int count)
    {
        if ((a < height) == (b < height)) return;
        crossings[count++] = Vector2.Lerp(start, end, (height - a) / (b - a));
    }

    private void DrawLine(Vector2 a, Vector2 b, Color32 color)
    {
        if (!ClipLine(ref a, ref b)) return;
        int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b)));
        for (int i = 0; i <= steps; i++)
        {
            Vector2 point = Vector2.Lerp(a, b, (float)i / steps);
            int index = Mathf.Clamp(Mathf.RoundToInt(point.y), 0, ImageSize - 1) * ImageSize +
                Mathf.Clamp(Mathf.RoundToInt(point.x), 0, ImageSize - 1);
            if (pixels[index].a < color.a) pixels[index] = color;
        }
    }

    public void Dispose()
    {
        Object.Destroy(texture);
    }
}

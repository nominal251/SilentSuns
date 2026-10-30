using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SilentSuns.Menu;

// Reads position, objectives and oxygen. This screen never changes gameplay progress.
public class Minimap : MonoBehaviour
{
    [Header("Navigation display")]
    [SerializeField] private RawImage mapImage;
    [SerializeField] private TMP_FontAsset navigationFont;
    [SerializeField] private string playerName = "Player";
    [SerializeField, Min(1)] private float viewRadius = 40f;
    [Tooltip("Turn this off when placing the display on the handheld tool's canvas.")]
    [SerializeField] private bool anchorToScreen = true;
    [Tooltip("Optional return destination. Without one, show the last visited refill station.")]
    [SerializeField] private Transform returnPoint;
    [SerializeField] private Color backgroundColor = new Color32(5, 14, 18, 255);
    [Header("Markers")]
    [SerializeField] private Sprite playerIcon;
    [SerializeField] private Color playerIconColor = new Color32(222, 221, 183, 255);
    [SerializeField, Min(.1f)] private float playerIconScale = 1f;
    [SerializeField, Min(.1f)] private float objectiveIconScale = 1f;
    [SerializeField] private bool clampToEdge = true;

    public enum MarkerKind { Objective, Anomaly, Signal }
    [System.Serializable]
    public class MarkerStyle
    {
        public Objective objective;
        public MarkerKind kind;
    }
    [Tooltip("Optional display symbols. Objectives without an override use the yellow square.")]
    [SerializeField] private List<MarkerStyle> markerStyles = new List<MarkerStyle>();

    private PlayerMovement playerMovement;
    private ObjectiveSystem objectiveSystem;
    private RectTransform mapRect, navigationRect, oxygenNeedle;
    private Canvas canvas;
    private Image playerImage, returnImage;
    private TMP_Text targetText, targetDetail, returnText, oxygenText, oxygenState;
    private readonly List<(Objective objective, Image icon)> objectiveIcons = new List<(Objective, Image)>();
    private readonly List<TMP_Text> compassLabels = new List<TMP_Text>();
    private readonly List<Sprite> artwork = new List<Sprite>();
    private Sprite disc, ring, diamond, arrow, signal, refill;
    private GameObject displayRoot;
    private static readonly Color Ink = new Color32(222, 221, 183, 255);
    private static readonly Color Muted = new Color32(112, 121, 101, 255);
    private static readonly Color Amber = new Color32(242, 179, 38, 255);
    private static readonly Color Red = new Color32(231, 65, 46, 255);
    private static readonly Vector2 PlotCenter = new Vector2(0, 10);
    private const float PlotRadius = 160;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }
    private static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!TerminalGameSession.IsSurveyScene(scene.name) || FindFirstObjectByType<Minimap>(FindObjectsInactive.Include) != null) return;
        var prefab = Resources.Load<GameObject>("SurveyMinimap");
        if (prefab != null) Instantiate(prefab);
    }
    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        if (canvas != null && anchorToScreen) canvas.enabled = false;
        if (mapImage == null) { enabled = false; return; }
        mapRect = mapImage.rectTransform;
        mapImage.texture = null;
        mapImage.color = Color.clear;
        mapImage.raycastTarget = false;
        // Old scene copies may still have the rectangular terrain mask.
        var oldMask = mapImage.GetComponent<RectMask2D>();
        if (oldMask != null) oldMask.enabled = false;
        disc = MakeSprite(false, false);
        ring = MakeSprite(true, false);
        diamond = MakeSprite(false, true);
        arrow = MakeArrow();
        signal = MakeSymbol(false);
        refill = MakeSymbol(true);
        BuildDisplay();
    }
    private void BuildDisplay()
    {
        // All parts share this screen. The tool can scale the whole canvas later.
        var root = TerminalUI.Rect("Navigation display", mapRect, 0, 0, 1, 1);
        displayRoot = root.gameObject;
        // This display only shows data, including when it is placed on an interactive tool canvas.
        var passive = displayRoot.AddComponent<CanvasGroup>();
        passive.interactable = passive.blocksRaycasts = false;
        Box("Screen", root, backgroundColor, Vector2.zero, new Vector2(640, 480));
        navigationRect = TerminalUI.Rect("Navigation circle", root, 28f / 640, 52f / 480, 388f / 640, 380f / 480);
        Draw("Navigation rim", navigationRect, ring, Ink, PlotCenter, 380);
        // Only the main bearings and a faint cross sit behind the markers.
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90;
            var tick = Box("Bearing tick " + i, navigationRect, Ink, PlotCenter + Bearing(angle) * 184, new Vector2(1, 9));
            tick.rectTransform.localRotation = Quaternion.Euler(0, 0, -angle);
        }
        Color guide = new Color(Muted.r, Muted.g, Muted.b, .22f);
        Box("Horizontal guide", navigationRect, guide, PlotCenter, new Vector2(305, 1));
        Box("Vertical guide", navigationRect, guide, PlotCenter, new Vector2(1, 305));
        foreach (string direction in new[] { "N", "E", "S", "W" })
        {
            var label = Label("Compass " + direction, navigationRect, direction, 0, 25, 29, Ink);
            label.rectTransform.sizeDelta = new Vector2(25 / .72f, 40);
            // Center the visible letter, including fonts with uneven side spacing.
            label.alignment = TextAlignmentOptions.MidlineGeoAligned;
            compassLabels.Add(label);
        }
        returnImage = Draw("Return marker", navigationRect, refill, Ink, PlotCenter, 17);
        playerImage = Draw("Player marker", navigationRect, playerIcon != null ? playerIcon : arrow, playerIconColor, PlotCenter, 23 * playerIconScale);
        targetText = PanelLabel("Nearest objective", root, "", 40, 452, 270, 18, Ink);
        targetDetail = PanelLabel("Objective distance", root, "", 314, 452, 90, 18, Ink);
        targetDetail.alignment = TextAlignmentOptions.Right;

        PanelLabel("Oxygen label", root, "O2", 452, 122, 158, 20, Ink).alignment = TextAlignmentOptions.Center;
        var dial = TerminalUI.Rect("Oxygen dial", root, 459f / 640, 158f / 480, 148f / 640, 148f / 480);
        // A tank-style dial. Its numbers are percentages because gameplay stores supply, not pressure.
        for (int i = 0; i <= 40; i++)
        {
            float angle = -135 + i * 6.75f;
            var edge = Box("Oxygen rim " + i, dial, Ink, Bearing(angle) * 77, new Vector2(9.5f, 2));
            edge.rectTransform.localRotation = Quaternion.Euler(0, 0, -angle);
            if (i % 4 == 0)
            {
                var tick = Box("Oxygen tick " + i, dial, Ink, Bearing(angle) * 70, new Vector2(1, 7));
                tick.rectTransform.localRotation = Quaternion.Euler(0, 0, -angle);
            }
            if (i < 7)
            {
                var warning = Box("Low supply zone " + i, dial, Red, Bearing(angle + 3) * 75, new Vector2(7, 4));
                warning.rectTransform.localRotation = Quaternion.Euler(0, 0, -angle - 3);
            }
            if (i % 20 == 0)
            {
                var number = Label("Gauge number " + i, dial, (i * 2.5f).ToString("0"), 0, 34, 14, Ink);
                number.rectTransform.anchoredPosition = Bearing(angle) * 51;
            }
        }
        oxygenNeedle = Draw("Oxygen needle", dial, arrow, Ink, Vector2.zero, 1).rectTransform;
        oxygenNeedle.pivot = new Vector2(.5f, 0);
        oxygenNeedle.sizeDelta = new Vector2(7, 61);
        Draw("Needle hub", dial, disc, Ink, Vector2.zero, 10);
        oxygenText = Label("Oxygen amount", dial, "", -62, 74, 22, Ink);
        oxygenState = Label("Oxygen state", dial, "", -98, 174, 14, Ink);
        returnText = PanelLabel("Return distance", root, "", 445, 389, 180, 14, Muted);
        returnText.alignment = TextAlignmentOptions.Center;
    }
    private static Vector2 Bearing(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
    }
    private static Image Box(string name, RectTransform parent, Color color, Vector2 position, Vector2 size)
    {
        var image = Draw(name, parent, null, color, position, 1);
        image.rectTransform.sizeDelta = size;
        return image;
    }
    // Positions are measured from the top-left of the 640 by 480 screen.
    private TMP_Text PanelLabel(string name, RectTransform parent, string value, float x, float y, float width, float size, Color color)
    {
        var text = Label(name, parent, value, 240 - y, width, size, color);
        text.rectTransform.anchoredPosition = new Vector2(x - 320 + width / 2, 240 - y);
        text.alignment = TextAlignmentOptions.Left;
        return text;
    }
    private void LateUpdate()
    {
        if (mapImage == null) return;
        if (playerMovement == null)
        {
            var named = GameObject.Find(playerName);
            playerMovement = named != null ? named.GetComponent<PlayerMovement>() : null;
            if (playerMovement == null) playerMovement = FindFirstObjectByType<PlayerMovement>();
        }
        // Same gate as oxygen: hide during menus, intro and ship flight.
        bool show = playerMovement != null && playerMovement.CanUseSuitOxygen;
        if (canvas != null && anchorToScreen) canvas.enabled = show;
        mapImage.gameObject.SetActive(show);
        if (!show) return;
        if (anchorToScreen)
        {
            float margin = Mathf.Max(.025f, GamePreferences.SafeArea);
            mapRect.anchorMin = mapRect.anchorMax = new Vector2(margin, 1 - margin);
        }
        if (objectiveSystem == null) objectiveSystem = FindFirstObjectByType<ObjectiveSystem>();
        SyncIcons();
        UpdateNavigation();
        UpdateOxygen();
    }

    // The future tool can provide these directly instead of searching the scene.
    public void SetSources(PlayerMovement movement, ObjectiveSystem objectives, Transform destination = null)
    {
        playerMovement = movement;
        objectiveSystem = objectives;
        returnPoint = destination;
    }
    private Vector2 PlotPosition(Vector3 destination)
    {
        Vector3 offset = destination - playerMovement.transform.position;
        float yaw = playerMovement.transform.eulerAngles.y * Mathf.Deg2Rad;
        // Player faces up. World markers turn the opposite way as the player turns.
        return new Vector2(offset.x * Mathf.Cos(yaw) - offset.z * Mathf.Sin(yaw),
            offset.x * Mathf.Sin(yaw) + offset.z * Mathf.Cos(yaw)) * (PlotRadius / Mathf.Max(1, viewRadius));
    }
    private void UpdateNavigation()
    {
        float heading = playerMovement.transform.eulerAngles.y;
        for (int i = 0; i < compassLabels.Count; i++)
        {
            float angle = (i * 90 - heading) * Mathf.Deg2Rad;
            compassLabels[i].rectTransform.anchoredPosition = PlotCenter + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * 169;
        }
        Objective nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (var pair in objectiveIcons)
        {
            bool available = pair.objective.gameObject.activeInHierarchy && (pair.objective.IsRunning || pair.objective.IsComplete);
            Vector2 point = PlotPosition(pair.objective.transform.position);
            bool outside = point.magnitude > PlotRadius - 8;
            pair.icon.gameObject.SetActive(available && (clampToEdge || !outside));
            if (!available) continue;
            bool done = pair.objective.IsComplete;
            // Completed markers stay dim. Distant objectives point along the circle's edge.
            MarkerKind kind = MarkerKind.Objective;
            foreach (var style in markerStyles)
                if (style != null && style.objective == pair.objective) { kind = style.kind; break; }
            pair.icon.sprite = outside && !done ? arrow : kind == MarkerKind.Anomaly ? diamond : kind == MarkerKind.Signal ? signal : null;
            pair.icon.rectTransform.localRotation = outside && !done ? Quaternion.Euler(0, 0, -Mathf.Atan2(point.x, point.y) * Mathf.Rad2Deg) : Quaternion.identity;
            pair.icon.rectTransform.anchoredPosition = PlotCenter + Vector2.ClampMagnitude(point, PlotRadius - 8);
            Color color = kind == MarkerKind.Anomaly ? Red : kind == MarkerKind.Signal ? Ink : Amber;
            pair.icon.color = done ? new Color(Muted.r, Muted.g, Muted.b, .65f) : color;
            pair.icon.rectTransform.sizeDelta = Vector2.one * (done ? 8 : 12) * Mathf.Clamp(objectiveIconScale, .5f, 1.5f);
            float distance = Vector3.Distance(playerMovement.transform.position, pair.objective.transform.position);
            if (!done && distance < nearestDistance) { nearest = pair.objective; nearestDistance = distance; }
        }
        targetText.text = nearest != null ? nearest.Title.ToUpperInvariant() : "NO ACTIVE OBJECTIVES";
        targetDetail.text = nearest != null ? $"{nearestDistance:0} m" : "";
        Transform destination = returnPoint != null ? returnPoint : playerMovement.CurrentOxygenStation;
        returnImage.gameObject.SetActive(destination != null);
        returnText.text = "";
        returnText.color = Ink;
        if (destination != null)
        {
            returnImage.rectTransform.anchoredPosition = PlotCenter + Vector2.ClampMagnitude(PlotPosition(destination.position), PlotRadius - 8);
            float distance = Vector3.Distance(playerMovement.transform.position, destination.position);
            returnText.text = $"{(returnPoint != null ? "RETURN" : "REFILL")}  {distance:0}m";
            returnText.color = distance > playerMovement.OxygenRemaining ? Red : Ink;
        }
        playerImage.rectTransform.SetAsLastSibling();
    }
    private void UpdateOxygen()
    {
        // PlayerMovement already converts the shared supply into its distance budget.
        float fraction = playerMovement.OxygenFraction;
        oxygenNeedle.localRotation = Quaternion.Euler(0, 0, 135 - fraction * 270);
        oxygenText.text = $"{Mathf.Clamp(Mathf.RoundToInt(fraction * 100), fraction > 0 ? 1 : 0, 100)}%";
        Color color = fraction <= .15f ? Red : fraction <= .3f ? Amber : Ink;
        oxygenText.color = fraction <= .3f ? color : Ink;
        oxygenState.color = color;
        oxygenState.text = fraction <= .15f ? "CRITICAL" : fraction <= .3f ? "LOW O2" : "";
    }
    private void SyncIcons()
    {
        for (int i = objectiveIcons.Count - 1; i >= 0; i--)
        {
            var pair = objectiveIcons[i];
            if (pair.objective != null && objectiveSystem != null && objectiveSystem.Objectives.Contains(pair.objective)) continue;
            if (pair.icon != null) Destroy(pair.icon.gameObject);
            objectiveIcons.RemoveAt(i);
        }
        if (objectiveSystem == null) return;
        foreach (var objective in objectiveSystem.Objectives)
        {
            if (objective == null || objectiveIcons.Exists(pair => pair.objective == objective)) continue;
            objectiveIcons.Add((objective, Draw("ObjectiveIcon_" + objective.Title, navigationRect, diamond, Amber, PlotCenter, 12)));
        }
    }
    public void SetPlayerIcon(Sprite sprite, Color color)
    {
        if (playerImage == null) return;
        playerImage.sprite = sprite != null ? sprite : arrow;
        playerImage.color = color;
    }
    private static Image Draw(string name, RectTransform parent, Sprite sprite, Color color, Vector2 position, float size)
    {
        var image = new GameObject(name, typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.gameObject.layer = parent.gameObject.layer;
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = Vector2.one * .5f;
        image.rectTransform.anchoredPosition = position;
        image.rectTransform.sizeDelta = Vector2.one * size;
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
    private TMP_Text Label(string name, RectTransform parent, string value, float y, float width, float size, Color color)
    {
        var text = new GameObject(name, typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        text.transform.SetParent(parent, false);
        text.gameObject.layer = parent.gameObject.layer;
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = Vector2.one * .5f;
        text.rectTransform.anchoredPosition = new Vector2(0, y);
        text.rectTransform.sizeDelta = new Vector2(width / .72f, Mathf.Max(22, size * 1.4f));
        text.font = navigationFont != null ? navigationFont : TMP_Settings.defaultFontAsset;
        text.rectTransform.localScale = new Vector3(.72f, 1, 1);
        text.fontSize = size;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.text = value;
        text.raycastTarget = false;
        return text;
    }
    private Sprite MakeSprite(bool outline, bool square)
    {
        const int size = 384;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 point = new Vector2(x + .5f, y + .5f) - Vector2.one * size / 2;
            float distance = square ? Mathf.Abs(point.x) + Mathf.Abs(point.y) : point.magnitude;
            float alpha = Mathf.Clamp01(size / 2f - distance);
            if (outline) alpha *= Mathf.Clamp01(distance - (size / 2f - 2.5f));
            texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
        }
        texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f);
        artwork.Add(sprite);
        return sprite;
    }
    private Sprite MakeSymbol(bool station)
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = Mathf.Abs(x - 15.5f), dy = Mathf.Abs(y - 15.5f);
            float radius = Mathf.Sqrt(dx * dx + dy * dy);
            bool lit = station ? (radius >= 12 && radius <= 15) || radius < 5
                : Mathf.Max(dx, dy) >= 11 && Mathf.Max(dx, dy) <= 14;
            texture.SetPixel(x, y, lit ? Color.white : Color.clear);
        }
        texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f);
        artwork.Add(sprite);
        return sprite;
    }
    private Sprite MakeArrow()
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float halfWidth = (size - 1 - y) * .46f;
            bool inside = Mathf.Abs(x - 15.5f) <= halfWidth && y > 3 && (y >= 10 || Mathf.Abs(x - 15.5f) > (10 - y) * .65f);
            texture.SetPixel(x, y, inside ? Color.white : Color.clear);
        }
        texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f);
        artwork.Add(sprite);
        return sprite;
    }
    private void OnDisable()
    {
        if (canvas != null && anchorToScreen) canvas.enabled = false;
        if (mapImage != null) mapImage.gameObject.SetActive(false);
    }
    private void OnDestroy()
    {
        if (displayRoot != null) Destroy(displayRoot);
        foreach (var sprite in artwork)
        {
            if (sprite == null) continue;
            Destroy(sprite.texture);
            Destroy(sprite);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UIButton = UnityEngine.UI.Button;
using SilentSuns.Menu;

// Reads position, objectives and oxygen. This screen never changes gameplay progress.
[DefaultExecutionOrder(-90)]
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
    [Header("Ground contours")]
    [SerializeField] private bool showTopography = true;
    [Tooltip("Ground and level geometry to sample. Triggers and Rigidbody objects are ignored.")]
    [SerializeField] private LayerMask topographyLayers = Physics.DefaultRaycastLayers;
    [SerializeField, Min(.5f)] private float contourHeight = 2f;
    [Tooltip("How far above and below the player to look for ground.")]
    [SerializeField, Min(10)] private float topographyHeightRange = 160f;
    [Header("Markers")]
    [SerializeField] private Sprite playerIcon;
    [SerializeField] private Color playerIconColor = new Color32(222, 221, 183, 255);
    [SerializeField, Min(.1f)] private float playerIconScale = 1f;
    [SerializeField, Min(.1f)] private float objectiveIconScale = 1f;
    [SerializeField] private bool clampToEdge = true;

    public enum MarkerKind { Objective, Anomaly, Signal }
    public enum TrackerMode { Objective, Anomaly, Signal, Refill }
    public TrackerMode SelectedTracker { get; private set; }
    public bool TrackerOpen { get; private set; }
    [System.Serializable]
    public class MarkerStyle
    {
        public Objective objective;
        public MarkerKind kind;
    }
    [Tooltip("Optional display symbols. Objectives without an override use the yellow star.")]
    [SerializeField] private List<MarkerStyle> markerStyles = new List<MarkerStyle>();

    private PlayerMovement playerMovement;
    private bool sourcesAssigned;
    private ObjectiveSystem objectiveSystem;
    private RectTransform mapRect, navigationRect, oxygenNeedle, returnOxygenTick, objectiveDirection;
    private Canvas canvas;
    private Image playerImage, returnImage;
    private TMP_Text objectiveHeading, targetText, targetDetail, returnText, oxygenText, oxygenState;
    private GameObject trackerPanel;
    private CanvasGroup displayInput;
    private GraphicRaycaster pointerInput;
    private readonly List<UIButton> trackerButtons = new List<UIButton>();
    private readonly List<Image> trackerSelections = new List<Image>();
    private Image trackedPoint;
    private MapMarker refillTarget;
    private Transform nearestRefill;
    private ResetOxygen[] refillStations = new ResetOxygen[0];
    private float nextStationCheck;
    private Vector3 mapPosition, lastPlayerPosition, mapFollowOffset;
    private float mapHeading;
    private bool poseReady;
    private readonly List<RectTransform> bearingTicks = new List<RectTransform>();
    private class MapMarker
    {
        public Objective objective;
        public Image icon, elevation, leader, backing, labelBacking;
        public TMP_Text label;
        public Vector2 point;
        public Vector2 layoutOffset, drawnOffset;
        public bool placed, wantsLabel;
        public int labelSlot = -1;
        public float clearTime, labelAlpha;
        public MarkerKind kind;
    }
    private readonly List<MapMarker> objectiveIcons = new List<MapMarker>();
    private readonly List<Rect> occupiedAreas = new List<Rect>();
    private MapMarker returnMarker;
    private MapMarker nearestMarker;
    private readonly List<TMP_Text> compassLabels = new List<TMP_Text>();
    private readonly List<Sprite> artwork = new List<Sprite>();
    private Sprite disc, ring, diamond, arrow, signal, refill, star;
    private GameObject displayRoot;
    private MinimapTopography topography;
    private static readonly Color Ink = new Color32(222, 221, 183, 255);
    private static readonly Color Muted = new Color32(112, 121, 101, 255);
    private static readonly Color Amber = new Color32(242, 179, 38, 255);
    private static readonly Color Red = new Color32(231, 65, 46, 255);
    private static readonly Color Green = new Color32(140, 207, 145, 255);
    private static readonly Vector2 PlotCenter = new Vector2(0, 10);
    private const float PlotRadius = 180;
    // Leave space between edge markers and the compass letters.
    private const float MarkerRadius = 164;

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
        star = MakeStar();
        BuildDisplay();
    }
    private void BuildDisplay()
    {
        // All parts share this screen. The tool can scale the whole canvas later.
        var root = TerminalUI.Rect("Navigation display", mapRect, 0, 0, 1, 1);
        displayRoot = root.gameObject;
        // The normal map is passive. Only the tracker selector accepts clicks.
        displayInput = displayRoot.AddComponent<CanvasGroup>();
        displayInput.interactable = displayInput.blocksRaycasts = false;
        if (canvas != null)
        {
            pointerInput = GetComponent<GraphicRaycaster>();
            if (pointerInput == null) pointerInput = gameObject.AddComponent<GraphicRaycaster>();
            pointerInput.enabled = false;
        }
        Box("Screen", root, backgroundColor, Vector2.zero, new Vector2(640, 480));
        Frame("Screen edge", root, 1, 1, 638, 478, new Color(Muted.r, Muted.g, Muted.b, .45f));
        navigationRect = TerminalUI.Rect("Navigation circle", root, 12f / 640, 88f / 480, 388f / 640, 380f / 480);
        // A little breathing room below the title without making the whole display larger.
        navigationRect.localScale = Vector3.one * .94f;
        navigationRect.anchoredPosition += Vector2.down * 8;
        if (showTopography)
            topography = new MinimapTopography(navigationRect, disc, PlotCenter, PlotRadius,
                viewRadius, topographyLayers, contourHeight, topographyHeightRange);
        Draw("Navigation rim", navigationRect, ring, Ink, PlotCenter, 380);
        // These ticks turn with the compass letters, while the player keeps facing up.
        for (int i = 0; i < 16; i++)
        {
            var tick = Box("Bearing tick " + i, navigationRect, Ink, PlotCenter, new Vector2(2, i % 4 == 0 ? 8 : 5));
            bearingTicks.Add(tick.rectTransform);
        }
        Draw("Range guides", navigationRect, MakeGuides(), new Color(Muted.r, Muted.g, Muted.b, .3f), PlotCenter, PlotRadius * 2);
        foreach (string direction in new[] { "N", "E", "S", "W" })
        {
            var label = Label("Compass " + direction, navigationRect, direction, 0, 25, 27, Ink);
            label.rectTransform.sizeDelta = new Vector2(34, 40);
            // Center the visible letter, including fonts with uneven side spacing.
            label.alignment = TextAlignmentOptions.MidlineGeoAligned;
            compassLabels.Add(label);
        }
        returnMarker = CreateMarker("Return marker");
        returnImage = returnMarker.icon;
        returnImage.sprite = refill;
        returnText = returnMarker.label;
        returnText.name = "Return distance";
        refillTarget = CreateMarker("Tracked refill");
        refillTarget.icon.sprite = refill;
        ShowMarker(refillTarget, false);
        trackedPoint = Draw("Tracked point", navigationRect, ring, Amber, PlotCenter, 34);
        trackedPoint.gameObject.SetActive(false);
        playerImage = Draw("Player marker", navigationRect, playerIcon != null ? playerIcon : arrow, playerIconColor, PlotCenter, 23 * playerIconScale);
        objectiveHeading = PanelLabel("Objective heading", root, "NEAREST OBJECTIVE", 24, 24, 420, 24, Ink);
        objectiveHeading.fontStyle = FontStyles.Bold;
        targetText = PanelLabel("Nearest objective", root, "", 24, 57, 420, 30, Amber);
        targetText.fontStyle = FontStyles.Bold;
        Box("Objective divider", root, new Color(Muted.r, Muted.g, Muted.b, .5f), new Vector2(0, 151), new Vector2(592, 1));
        objectiveDirection = Draw("Objective direction", root, arrow, Amber, new Vector2(156, 192), 23).rectTransform;
        targetDetail = PanelLabel("Objective distance", root, "", 492, 48, 124, 28, Ink);
        targetDetail.alignment = TextAlignmentOptions.Left;

        // Keep the current supply together below the dial, separate from the return threshold.
        var oxygenLabel = PanelLabel("Oxygen label", root, "O2", 440, 332, 64, 26, Ink);
        oxygenLabel.fontStyle = FontStyles.Bold;
        oxygenText = PanelLabel("Oxygen amount", root, "", 504, 332, 104, 28, Ink);
        oxygenText.alignment = TextAlignmentOptions.Right;
        oxygenText.fontStyle = FontStyles.Bold;
        var dial = TerminalUI.Rect("Oxygen dial", root, 446f / 640, 162f / 480, 148f / 640, 148f / 480);
        // A tank-style dial. Its numbers are percentages because gameplay stores supply, not pressure.
        for (int i = 0; i <= 40; i++)
        {
            float angle = -135 + i * 6.75f;
            var edge = Box("Oxygen rim " + i, dial, Ink, Bearing(angle) * 77, new Vector2(9.5f, 2));
            edge.rectTransform.localRotation = Quaternion.Euler(0, 0, -angle);
            if (i % 4 == 0)
            {
                var tick = Box("Oxygen tick " + i, dial, Ink, Bearing(angle) * 70, new Vector2(2, 7));
                tick.rectTransform.localRotation = Quaternion.Euler(0, 0, -angle);
            }
            if (i < 7)
            {
                var warning = Box("Low supply zone " + i, dial, Red, Bearing(angle + 3) * 75, new Vector2(7, 4));
                warning.rectTransform.localRotation = Quaternion.Euler(0, 0, -angle - 3);
            }
            if (i % 20 == 0)
            {
                var number = Label("Gauge number " + i, dial, (i * 2.5f).ToString("0"), 0, 48, 20, Muted);
                number.rectTransform.anchoredPosition = Bearing(angle) * 51;
            }
        }
        oxygenNeedle = Draw("Oxygen needle", dial, arrow, Ink, Vector2.zero, 1).rectTransform;
        oxygenNeedle.pivot = new Vector2(.5f, 0);
        oxygenNeedle.sizeDelta = new Vector2(7, 61);
        Draw("Needle hub", dial, disc, Ink, Vector2.zero, 10);
        returnOxygenTick = Box("Return oxygen mark", dial, Green, Vector2.zero, new Vector2(3, 19)).rectTransform;
        oxygenState = PanelLabel("Oxygen state", root, "", 424, 390, 192, 22, Ink);
        oxygenState.alignment = TextAlignmentOptions.Center;
        oxygenState.fontStyle = FontStyles.Bold;
        oxygenState.rectTransform.sizeDelta = new Vector2(192, 58);
        BuildTrackerSelector(root);
    }

    private void BuildTrackerSelector(RectTransform root)
    {
        var panel = TerminalUI.Rect("Tracker selector", root, 0, 0, 1, 1);
        trackerPanel = panel.gameObject;
        var cover = Box("Selector screen", panel, backgroundColor, Vector2.zero, new Vector2(640, 480));
        cover.raycastTarget = true;
        Frame("Selector edge", panel, 1, 1, 638, 478, Muted);
        PanelLabel("Selector title", panel, "SELECT TRACKER", 32, 36, 560, 30, Ink).fontStyle = FontStyles.Bold;
        Box("Selector divider", panel, Muted, new Vector2(0, 171), new Vector2(576, 1));
        for (int i = 0; i < 4; i++)
        {
            var mode = (TrackerMode)i;
            float y = 92 + i * 80;
            Color color = TrackerColor(mode);
            var row = TerminalUI.Rect("Track " + mode, panel, 32f / 640, y / 480, 576f / 640, 64f / 480);
            // The entire row is clickable; the symbol and text share the map's colors.
            var face = Box("Button face", row, Color.white, Vector2.zero, new Vector2(576, 64));
            face.raycastTarget = true;
            var button = row.gameObject.AddComponent<UIButton>();
            button.targetGraphic = face;
            var colors = button.colors;
            colors.normalColor = new Color(.045f, .08f, .09f, 1);
            colors.disabledColor = colors.normalColor;
            colors.highlightedColor = Color.Lerp(colors.normalColor, color, .23f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = Color.Lerp(colors.normalColor, color, .4f);
            colors.fadeDuration = .12f;
            button.colors = colors;
            face.CrossFadeColor(colors.normalColor, 0, true, true);
            Frame("Tracker row", panel, 32, y, 576, 64, new Color(Muted.r, Muted.g, Muted.b, .6f));
            Draw("Tracker icon", row, mode == TrackerMode.Objective ? star : mode == TrackerMode.Anomaly ? diamond : mode == TrackerMode.Signal ? signal : refill,
                color, new Vector2(-246, 0), 30);
            var text = Label("Tracker name", row, mode.ToString().ToUpperInvariant(), 0, 400, 30, color);
            text.alignment = TextAlignmentOptions.Left;
            text.fontStyle = FontStyles.Bold;
            text.rectTransform.anchoredPosition = new Vector2(0, 0);
            var selected = Box("Selected tracker", row, color, new Vector2(-284, 0), new Vector2(5, 62));
            trackerSelections.Add(selected);
            button.onClick.AddListener(() => { TerminalInteractionAudio.Instance?.Mouse(); SelectTracker(mode); });
            trackerButtons.Add(button);
        }
        for (int i = 0; i < trackerButtons.Count; i++)
            trackerButtons[i].navigation = new Navigation { mode = Navigation.Mode.Explicit,
                selectOnUp = trackerButtons[(i + 3) % 4], selectOnDown = trackerButtons[(i + 1) % 4] };
        PanelLabel("Selector back", panel, "M / BACK", 32, 448, 560, 20, Muted);
        trackerPanel.SetActive(false);
    }

    private static Color TrackerColor(TrackerMode mode) => mode == TrackerMode.Anomaly ? Red :
        mode == TrackerMode.Signal ? Ink : mode == TrackerMode.Refill ? Green : Amber;

    private void Update()
    {
        if (TrackerOpen && (playerMovement == null || !playerMovement.CanUseSuitOxygen ||
            mapImage == null || !mapImage.gameObject.activeInHierarchy)) CloseTracker();
        if (TerminalBindings.Listening || Time.frameCount <= TerminalBindings.ConsumedFrame) return;
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame) ToggleTracker();
    }

    // These methods also let the handheld tool open this same screen later.
    public void ToggleTracker()
    {
        if (TrackerOpen) { CloseTracker(); return; }
        if (playerMovement == null || !playerMovement.CanUseSuitOxygen || !mapImage.gameObject.activeInHierarchy) return;
        if (EventSystem.current == null)
        {
            var events = new GameObject("Navigation input", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        TerminalToolInput.Begin(this, CloseTracker);
        TrackerOpen = true;
        trackerPanel.SetActive(true);
        trackerPanel.transform.SetAsLastSibling();
        displayInput.interactable = displayInput.blocksRaycasts = true;
        if (pointerInput != null) pointerInput.enabled = true;
        for (int i = 0; i < trackerSelections.Count; i++) trackerSelections[i].gameObject.SetActive(i == (int)SelectedTracker);
        EventSystem.current.SetSelectedGameObject(trackerButtons[(int)SelectedTracker].gameObject);
    }

    public void CloseTracker()
    {
        if (!TrackerOpen) return;
        TrackerOpen = false;
        trackerPanel.SetActive(false);
        displayInput.interactable = displayInput.blocksRaycasts = false;
        if (pointerInput != null) pointerInput.enabled = false;
        TerminalToolInput.End(this);
    }

    public void SelectTracker(TrackerMode mode)
    {
        if ((int)mode < 0 || (int)mode > 3) return;
        SelectedTracker = mode;
        nearestMarker = null;
        nearestRefill = null;
        nextStationCheck = 0;
        CloseTracker();
    }
    private static void Frame(string name, RectTransform parent, float x, float y, float width, float height, Color color)
    {
        Vector2 center = new Vector2(x - 320 + width / 2, 240 - y - height / 2);
        Box(name + " top", parent, color, center + Vector2.up * height / 2, new Vector2(width, 2));
        Box(name + " bottom", parent, color, center + Vector2.down * height / 2, new Vector2(width, 2));
        Box(name + " left", parent, color, center + Vector2.left * width / 2, new Vector2(2, height));
        Box(name + " right", parent, color, center + Vector2.right * width / 2, new Vector2(2, height));
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
        if (!show) { CloseTracker(); poseReady = false; return; }
        if (anchorToScreen)
        {
            float margin = Mathf.Max(.025f, GamePreferences.SafeArea);
            mapRect.anchorMin = mapRect.anchorMax = new Vector2(margin, 1 - margin);
        }
        if (!sourcesAssigned && (objectiveSystem == null || objectiveSystem.gameObject.scene != SceneManager.GetActiveScene()))
        {
            // Landing keeps the ship loaded. Track the active planet, not its parked ship screen.
            objectiveSystem = null;
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                objectiveSystem = root.GetComponentInChildren<ObjectiveSystem>();
                if (objectiveSystem != null) break;
            }
        }
        SyncIcons();
        UpdateMapPose();
        topography?.Update(mapPosition, mapHeading);
        UpdateNavigation();
        UpdateLabelFade(returnMarker);
        UpdateLabelFade(refillTarget);
        foreach (var marker in objectiveIcons) UpdateLabelFade(marker);
        UpdateOxygen();
    }

    private void UpdateMapPose()
    {
        Vector3 position = playerMovement.transform.position;
        float heading = playerMovement.transform.eulerAngles.y;
        // Ground, symbols and compass use one smoothed pose so they stay lined up.
        // A new scene or teleport should settle immediately rather than scroll across the level.
        if (!poseReady || Vector3.Distance(mapPosition, position) > viewRadius * 2)
        {
            mapPosition = position;
            lastPlayerPosition = position;
            mapFollowOffset = Vector3.zero;
            mapHeading = heading;
            poseReady = true;
        }
        float follow = 1 - Mathf.Exp(-Time.deltaTime / .09f);
        // Keep the lag as a small offset so large world coordinates don't stall the easing.
        mapFollowOffset = (mapFollowOffset + (lastPlayerPosition - position)) * (1 - follow);
        mapPosition = position + mapFollowOffset;
        lastPlayerPosition = position;
        mapHeading = Mathf.LerpAngle(mapHeading, heading, follow);
    }

    // The future tool can provide these directly instead of searching the scene.
    public void SetSources(PlayerMovement movement, ObjectiveSystem objectives, Transform destination = null)
    {
        sourcesAssigned = true;
        if (playerMovement != movement) poseReady = false;
        playerMovement = movement;
        objectiveSystem = objectives;
        returnPoint = destination;
        nearestMarker = null;
        nearestRefill = null;
    }
    private Vector2 PlotPosition(Vector3 destination)
    {
        Vector3 offset = destination - mapPosition;
        float yaw = mapHeading * Mathf.Deg2Rad;
        // Player faces up. World markers turn the opposite way as the player turns.
        return new Vector2(offset.x * Mathf.Cos(yaw) - offset.z * Mathf.Sin(yaw),
            offset.x * Mathf.Sin(yaw) + offset.z * Mathf.Cos(yaw)) * (PlotRadius / Mathf.Max(1, viewRadius));
    }
    private void UpdateNavigation()
    {
        float heading = mapHeading;
        for (int i = 0; i < bearingTicks.Count; i++)
        {
            float angle = i * 22.5f - heading;
            bearingTicks[i].anchoredPosition = PlotCenter + Bearing(angle) * (i % 4 == 0 ? 194 : 186);
            bearingTicks[i].localRotation = Quaternion.Euler(0, 0, -angle);
        }
        for (int i = 0; i < compassLabels.Count; i++)
        {
            float angle = (i * 90 - heading) * Mathf.Deg2Rad;
            compassLabels[i].rectTransform.anchoredPosition = PlotCenter + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * 178;
        }
        // Lay out the nearest objective first, then the refill and other markers.
        // They keep their actual positions; a short line connects any separated symbols.
        occupiedAreas.Clear();
        occupiedAreas.Add(new Rect(PlotCenter - Vector2.one * 16, Vector2.one * 32));
        foreach (var compass in compassLabels)
            occupiedAreas.Add(new Rect(compass.rectTransform.anchoredPosition - new Vector2(14, 16), new Vector2(28, 32)));
        MapMarker nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (var marker in objectiveIcons)
        {
            bool available = marker.objective.gameObject.activeInHierarchy &&
                (marker.objective.IsRunning || marker.objective.IsComplete);
            marker.point = PlotPosition(marker.objective.transform.position);
            bool outside = marker.point.magnitude > MarkerRadius;
            ShowMarker(marker, available && (clampToEdge || !outside));
            marker.kind = MarkerKind.Objective;
            foreach (var style in markerStyles)
                if (style != null && style.objective == marker.objective) { marker.kind = style.kind; break; }
            float distance = Vector3.Distance(playerMovement.transform.position, marker.objective.transform.position);
            if (available && !marker.objective.IsComplete && (int)marker.kind == (int)SelectedTracker && distance < nearestDistance)
            { nearest = marker; nearestDistance = distance; }
        }
        // Don't switch between almost equally close objectives with every small step.
        if (nearest != null && nearestMarker != null && nearestMarker.objective != null && nearestMarker.kind == nearest.kind &&
            nearestMarker.objective.gameObject.activeInHierarchy && nearestMarker.objective.IsRunning && !nearestMarker.objective.IsComplete)
        {
            float previousDistance = Vector3.Distance(playerMovement.transform.position, nearestMarker.objective.transform.position);
            if (previousDistance <= nearestDistance + Mathf.Max(1, nearestDistance * .1f))
            { nearest = nearestMarker; nearestDistance = previousDistance; }
        }
        nearestMarker = nearest;
        if (nearest != null && nearest.icon.gameObject.activeSelf) PlaceObjective(nearest);

        Transform destination = ReturnDestination;
        ShowMarker(returnMarker, destination != null);
        returnText.text = "";
        if (destination != null)
        {
            returnMarker.point = PlotPosition(destination.position);
            PlaceMarker(returnMarker, 22, Green);
            float distance = Vector3.Distance(playerMovement.transform.position, destination.position);
            returnText.text = $"REFILL {distance:0}M";
        }
        foreach (var marker in objectiveIcons)
            if (marker != nearest && marker.icon.gameObject.activeSelf) PlaceObjective(marker);

        // A refill tracker finds real, active refill triggers. It never changes the O2 return budget.
        Transform trackedRefill = SelectedTracker == TrackerMode.Refill ? FindNearestRefill() : null;
        ShowMarker(refillTarget, trackedRefill != null && trackedRefill != destination);
        if (refillTarget.icon.gameObject.activeSelf)
        {
            refillTarget.point = PlotPosition(trackedRefill.position);
            PlaceMarker(refillTarget, 22, Green);
        }

        // Identify nearby points directly. One label per kind avoids covering the map in text.
        if (nearest != null && nearest.icon.gameObject.activeSelf) PlaceLabel(nearest, KindName(nearest.kind));
        if (destination != null) PlaceLabel(returnMarker, returnText.text);
        if (refillTarget.icon.gameObject.activeSelf)
            PlaceLabel(refillTarget, $"REFILL {Vector3.Distance(playerMovement.transform.position, trackedRefill.position):0}M");
        foreach (MarkerKind kind in System.Enum.GetValues(typeof(MarkerKind)))
        {
            if (nearest != null && nearest.kind == kind) continue;
            var marker = objectiveIcons.Where(m => m.kind == kind && m.icon.gameObject.activeSelf && !m.objective.IsComplete)
                .OrderBy(m => m.point.sqrMagnitude).FirstOrDefault();
            if (marker != null) PlaceLabel(marker, KindName(kind));
        }

        Transform target = trackedRefill != null ? trackedRefill : nearest != null ? nearest.objective.transform : null;
        Color targetColor = TrackerColor(SelectedTracker);
        objectiveHeading.text = "NEAREST " + (SelectedTracker == TrackerMode.Refill ? "REFILL STATION" : SelectedTracker.ToString().ToUpperInvariant());
        targetText.text = trackedRefill != null ? "REFILL STATION" : nearest != null ? nearest.objective.Title.ToUpperInvariant() :
            SelectedTracker == TrackerMode.Objective ? "NO ACTIVE OBJECTIVES" : "NO " + SelectedTracker.ToString().ToUpperInvariant() + " FOUND";
        targetText.color = targetColor;
        targetDetail.color = targetColor;
        objectiveDirection.GetComponent<Image>().color = targetColor;
        if (target != null) nearestDistance = Vector3.Distance(playerMovement.transform.position, target.position);
        // Keep the arrow and distance beside the title, including when the title changes.
        float titleWidth = Mathf.Clamp(targetText.GetPreferredValues(targetText.text).x + 2, 80, 388);
        targetText.rectTransform.sizeDelta = new Vector2(titleWidth, 42);
        targetText.rectTransform.anchoredPosition = new Vector2(24 - 320 + titleWidth / 2, 183);
        objectiveDirection.anchoredPosition = new Vector2(24 - 320 + titleWidth + 24, 183);
        targetDetail.rectTransform.anchoredPosition = new Vector2(24 - 320 + titleWidth + 46 + 62, 183);
        targetDetail.text = target != null ? $"{nearestDistance:0} M" : "";
        float targetHeight = target != null ? target.position.y - playerMovement.transform.position.y : 0;
        bool differentLevel = Mathf.Abs(targetHeight) >= 3;
        targetDetail.fontSize = differentLevel ? 20 : 28;
        targetDetail.rectTransform.sizeDelta = new Vector2(124, 54);
        if (differentLevel) targetDetail.text += $"\n{Mathf.Abs(targetHeight):0}M {(targetHeight > 0 ? "ABOVE" : "BELOW")}";
        objectiveDirection.gameObject.SetActive(target != null);
        MapMarker tracked = trackedRefill != null ? (trackedRefill == destination ? returnMarker : refillTarget) : nearest;
        trackedPoint.gameObject.SetActive(tracked != null && tracked.icon.gameObject.activeSelf);
        if (target != null)
        {
            Vector2 direction = PlotPosition(target.position);
            objectiveDirection.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg);
            trackedPoint.rectTransform.anchoredPosition = tracked.icon.rectTransform.anchoredPosition;
            trackedPoint.color = targetColor;
            trackedPoint.transform.SetAsLastSibling();
        }
        playerImage.rectTransform.SetAsLastSibling();
    }

    private Transform FindNearestRefill()
    {
        // Recheck the scene occasionally so newly activated stations can be found too.
        if (Time.unscaledTime >= nextStationCheck)
        {
            refillStations = FindObjectsByType<ResetOxygen>(FindObjectsSortMode.None);
            nextStationCheck = Time.unscaledTime + .5f;
        }
        Transform best = null;
        float distance = float.MaxValue;
        foreach (var station in refillStations)
        {
            if (station == null || !station.isActiveAndEnabled) continue;
            var trigger = station.GetComponent<Collider>();
            if (trigger == null || !trigger.enabled || !trigger.isTrigger) continue;
            float candidate = Vector3.Distance(playerMovement.transform.position, station.transform.position);
            if (candidate < distance) { best = station.transform; distance = candidate; }
        }
        // An explicitly supplied ship/return point can also be used by the future tool.
        Transform known = ReturnDestination;
        if (known != null && known.gameObject.activeInHierarchy)
        {
            var station = known.GetComponent<ResetOxygen>();
            var trigger = known.GetComponent<Collider>();
            bool available = station == null || (station.isActiveAndEnabled && trigger != null && trigger.enabled && trigger.isTrigger);
            float candidate = Vector3.Distance(playerMovement.transform.position, known.position);
            if (available && candidate < distance) { best = known; distance = candidate; }
        }
        // Keep the same station while two choices are almost equally close.
        if (nearestRefill != null && nearestRefill.gameObject.activeInHierarchy)
        {
            var station = nearestRefill.GetComponent<ResetOxygen>();
            var trigger = nearestRefill.GetComponent<Collider>();
            bool available = station != null ? station.isActiveAndEnabled && trigger != null && trigger.enabled && trigger.isTrigger : nearestRefill == known;
            if (available && best != null && Vector3.Distance(playerMovement.transform.position, nearestRefill.position) <= distance + Mathf.Max(1, distance * .1f))
                best = nearestRefill;
        }
        nearestRefill = best;
        return best;
    }
    private static string KindName(MarkerKind kind)
    {
        return kind == MarkerKind.Anomaly ? "ANOMALY" : kind == MarkerKind.Signal ? "SIGNAL" : "OBJECTIVE";
    }
    private void PlaceObjective(MapMarker marker)
    {
        bool done = marker.objective.IsComplete;
        marker.icon.sprite = marker.kind == MarkerKind.Anomaly ? diamond : marker.kind == MarkerKind.Signal ? signal : star;
        // Keep symbols upright and the same shape, even when clamped to the map edge.
        marker.icon.rectTransform.localRotation = Quaternion.identity;
        Color color = marker.kind == MarkerKind.Anomaly ? Red : marker.kind == MarkerKind.Signal ? Ink : Amber;
        PlaceMarker(marker, (done ? 14 : 22) * Mathf.Clamp(objectiveIconScale, .5f, 1.5f),
            done ? new Color(Muted.r, Muted.g, Muted.b, .65f) : color);
        float height = marker.objective.transform.position.y - playerMovement.transform.position.y;
        marker.elevation.gameObject.SetActive(!done && Mathf.Abs(height) >= 3);
        marker.elevation.rectTransform.anchoredPosition = marker.icon.rectTransform.anchoredPosition + Vector2.right * 17;
        marker.elevation.rectTransform.localRotation = Quaternion.Euler(0, 0, height > 0 ? 0 : 180);
    }
    private MapMarker CreateMarker(string name)
    {
        var marker = new MapMarker();
        string decoration = name.Replace("ObjectiveIcon_", "Marker ");
        marker.leader = Box(decoration + " position line", navigationRect, Muted, PlotCenter, Vector2.one);
        marker.backing = Draw(decoration + " backing", navigationRect, disc, backgroundColor, PlotCenter, 30);
        marker.icon = Draw(name, navigationRect, star, Amber, PlotCenter, 22);
        marker.elevation = Draw(decoration + " height", navigationRect, arrow, Ink, PlotCenter, 8);
        marker.labelBacking = Box(decoration + " label backing", navigationRect, backgroundColor, PlotCenter, Vector2.one);
        marker.label = Label(decoration + " label", navigationRect, "", 0, 128, 20, Ink);
        marker.label.gameObject.SetActive(false);
        marker.labelBacking.gameObject.SetActive(false);
        return marker;
    }
    private static void ShowMarker(MapMarker marker, bool show)
    {
        marker.icon.gameObject.SetActive(show);
        marker.backing.gameObject.SetActive(show);
        marker.leader.gameObject.SetActive(false);
        marker.elevation.gameObject.SetActive(false);
        marker.wantsLabel = false;
    }
    private bool SpaceFree(Rect area)
    {
        return !occupiedAreas.Exists(other => other.Overlaps(area));
    }
    private void PlaceMarker(MapMarker marker, float size, Color color)
    {
        Vector2 actual = PlotCenter + Vector2.ClampMagnitude(marker.point, MarkerRadius);
        Vector2 position = actual;
        Vector2 footprint = Vector2.one * (size + 10);
        bool ClearAt(Vector2 candidate) => Vector2.Distance(candidate, PlotCenter) <= MarkerRadius &&
            SpaceFree(new Rect(candidate - footprint / 2, footprint));
        marker.clearTime = ClearAt(actual) ? marker.clearTime + Time.deltaTime : 0;
        bool keepOffset = marker.placed && marker.clearTime < .35f && ClearAt(actual + marker.layoutOffset);
        if (keepOffset) position = actual + marker.layoutOffset;
        // Separate overlapping markers instead of drawing a star, refill and player on top of each other.
        for (int attempt = 0; !keepOffset && attempt < 33; attempt++)
        {
            Vector2 candidate = actual;
            if (attempt > 0) candidate += Bearing((attempt - 1) % 8 * 45) * (1 + (attempt - 1) / 8) * 30;
            if (!ClearAt(candidate)) continue;
            position = candidate;
            break;
        }
        marker.layoutOffset = position - actual;
        marker.drawnOffset = marker.placed ? Vector2.Lerp(marker.drawnOffset, marker.layoutOffset,
            1 - Mathf.Exp(-Time.deltaTime / .08f)) : marker.layoutOffset;
        marker.placed = true;
        Vector2 target = position;
        position = PlotCenter + Vector2.ClampMagnitude(actual + marker.drawnOffset - PlotCenter, MarkerRadius);
        marker.icon.rectTransform.anchoredPosition = position;
        marker.icon.rectTransform.sizeDelta = Vector2.one * size;
        marker.icon.color = color;
        marker.backing.rectTransform.anchoredPosition = position;
        marker.backing.rectTransform.sizeDelta = Vector2.one * (size + 8);
        occupiedAreas.Add(new Rect(position - footprint / 2, footprint));
        occupiedAreas.Add(new Rect(target - footprint / 2, footprint));
        Vector2 line = position - actual;
        marker.leader.gameObject.SetActive(line.magnitude > 2);
        marker.leader.rectTransform.anchoredPosition = (position + actual) / 2;
        marker.leader.rectTransform.sizeDelta = new Vector2(line.magnitude, 1.5f);
        marker.leader.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(line.y, line.x) * Mathf.Rad2Deg);
        marker.leader.color = new Color(color.r, color.g, color.b, .65f);
        marker.label.color = color;
    }
    private void PlaceLabel(MapMarker marker, string text)
    {
        marker.label.text = text;
        float width = marker.label.GetPreferredValues(text).x + 8;
        Vector2 size = new Vector2(width, 26);
        Vector2 point = marker.icon.rectTransform.anchoredPosition;
        // Keep the same side while it fits. If it needs to move, fade it out first.
        for (int attempt = -1; attempt < 16; attempt++)
        {
            int i = attempt < 0 ? marker.labelSlot : attempt;
            if (i < 0) continue;
            Vector2 offset = i < 4 ? new Vector2((i % 2 == 0 ? 1 : -1) * (width / 2 + 18), i < 2 ? 0 : 28)
                : new Vector2(i % 4 < 2 ? 0 : (i % 2 == 0 ? 36 : -36), (i % 2 == 0 ? 32 : -32) * (i / 4));
            Vector2 position = point + offset;
            if (i == marker.labelSlot)
            {
                marker.label.rectTransform.anchoredPosition = position;
                marker.labelBacking.rectTransform.anchoredPosition = position;
            }
            var area = new Rect(position - size / 2, size);
            // All four label corners must fit inside the plot, away from the compass letters.
            Vector2 farthest = new Vector2(Mathf.Abs(position.x - PlotCenter.x), Mathf.Abs(position.y - PlotCenter.y)) + size / 2;
            if (farthest.magnitude > MarkerRadius || !SpaceFree(area)) continue;
            if (marker.labelSlot != i && marker.labelAlpha > 0) return;
            marker.labelSlot = i;
            marker.wantsLabel = true;
            marker.label.rectTransform.anchoredPosition = position;
            marker.label.rectTransform.sizeDelta = size;
            marker.labelBacking.rectTransform.anchoredPosition = position;
            marker.labelBacking.rectTransform.sizeDelta = size;
            marker.labelBacking.transform.SetAsLastSibling();
            marker.label.transform.SetAsLastSibling();
            occupiedAreas.Add(area);
            break;
        }
    }
    private static void UpdateLabelFade(MapMarker marker)
    {
        marker.labelAlpha = Mathf.MoveTowards(marker.labelAlpha, marker.wantsLabel ? 1 : 0, Time.deltaTime / .16f);
        marker.label.alpha = marker.labelAlpha;
        Color color = marker.labelBacking.color;
        color.a = marker.labelAlpha;
        marker.labelBacking.color = color;
        marker.label.gameObject.SetActive(marker.labelAlpha > 0);
        marker.labelBacking.gameObject.SetActive(marker.labelAlpha > 0);
    }
    private void UpdateOxygen()
    {
        // PlayerMovement already converts the shared supply into its distance budget.
        float fraction = playerMovement.OxygenFraction;
        oxygenNeedle.localRotation = Quaternion.Euler(0, 0, 135 - fraction * 270);
        oxygenText.text = $"{Mathf.Clamp(Mathf.RoundToInt(fraction * 100), fraction > 0 ? 1 : 0, 100)}%";
        Color color = fraction <= .15f ? Red : fraction <= .3f ? Amber : Ink;
        oxygenText.color = fraction <= .3f ? color : Ink;
        Transform destination = ReturnDestination;
        returnOxygenTick.gameObject.SetActive(destination != null);
        // A straight-line estimate, using the same distance cost as walking.
        // The landing system can pass the ship in SetSources. Until then use the last refill station.
        float needed = destination != null ? Vector3.Distance(playerMovement.transform.position, destination.position) / playerMovement.OxygenRangeAtFull : 0;
        // Head back at 30%, or earlier if the trip needs more. Leave a 5% buffer for that trip.
        // The green tick and the status below the dial show the same warning threshold.
        float returnAt = Mathf.Max(.3f, needed + .05f);
        if (destination != null)
        {
            float angle = -135 + Mathf.Clamp01(returnAt) * 270;
            returnOxygenTick.anchoredPosition = Bearing(angle) * 77;
            returnOxygenTick.localRotation = Quaternion.Euler(0, 0, -angle);
        }

        // Only one message at a time. Critical supply takes priority over the return estimate.
        if (fraction <= .15f)
        {
            oxygenState.text = "O2 CRITICAL\nFIND REFILL";
            oxygenState.color = Red;
        }
        else if (destination != null && fraction < needed)
        {
            oxygenState.text = "RETURN TOO FAR\nFIND REFILL";
            oxygenState.color = Red;
        }
        else if (fraction <= returnAt)
        {
            oxygenState.text = destination != null ? "RETURN NOW" : "LOW O2\nFIND REFILL";
            oxygenState.color = Amber;
        }
        else
        {
            oxygenState.text = destination != null ? $"RETURN WHEN O2\nREACHES {Mathf.CeilToInt(returnAt * 100 - .001f)}%" : "O2 OK";
            oxygenState.color = destination != null ? Green : Ink;
        }
    }
    private Transform ReturnDestination => returnPoint != null ? returnPoint : playerMovement.CurrentOxygenStation;
    private void SyncIcons()
    {
        for (int i = objectiveIcons.Count - 1; i >= 0; i--)
        {
            var pair = objectiveIcons[i];
            if (pair.objective != null && objectiveSystem != null && objectiveSystem.Objectives.Contains(pair.objective)) continue;
            if (nearestMarker == pair) nearestMarker = null;
            foreach (var graphic in new Graphic[] { pair.icon, pair.elevation, pair.leader, pair.backing, pair.label, pair.labelBacking })
                if (graphic != null) Destroy(graphic.gameObject);
            objectiveIcons.RemoveAt(i);
        }
        if (objectiveSystem == null) return;
        foreach (var objective in objectiveSystem.Objectives)
        {
            if (objective == null || objectiveIcons.Exists(pair => pair.objective == objective)) continue;
            var marker = CreateMarker("ObjectiveIcon_" + objective.Title);
            marker.objective = objective;
            marker.elevation.name = "Objective height " + objective.Title;
            objectiveIcons.Add(marker);
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
        text.rectTransform.sizeDelta = new Vector2(width, Mathf.Max(26, size * 1.4f));
        text.font = navigationFont != null ? navigationFont : TMP_Settings.defaultFontAsset;
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
        int size = square ? 32 : 384;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 point = new Vector2(x + .5f, y + .5f) - Vector2.one * size / 2;
            // A narrow, pointed diamond stays distinct from square building footprints.
            float distance = square ? Mathf.Abs(point.x) * 1.5f + Mathf.Abs(point.y) : point.magnitude;
            float alpha = square ? (distance <= 15 ? 1 : 0) : Mathf.Clamp01(size / 2f - distance);
            if (outline) alpha *= Mathf.Clamp01(distance - (size / 2f - 2.5f));
            texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
        }
        texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f);
        artwork.Add(sprite);
        return sprite;
    }
    private Sprite MakeGuides()
    {
        const int size = 384;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 point = new Vector2(x + .5f, y + .5f) - Vector2.one * size / 2;
            float angle = Mathf.Atan2(point.y, point.x) * Mathf.Rad2Deg + 180;
            bool circle = Mathf.Abs(point.magnitude - 158) < .8f && angle % 8 < 4;
            texture.SetPixel(x, y, circle ? Color.white : Color.clear);
        }
        texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f);
        artwork.Add(sprite);
        return sprite;
    }
    private Sprite MakeSymbol(bool station)
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
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
    private Sprite MakeStar()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var corners = new Vector2[10];
        // Alternate the five tips and five inner corners, starting with the top tip.
        for (int i = 0; i < corners.Length; i++)
            corners[i] = Vector2.one * 32 + Bearing(i * 36) * (i % 2 == 0 ? 30 : 13);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 point = new Vector2(x + .5f, y + .5f);
            bool inside = false;
            // Each edge crossing switches between the inside and outside of the star.
            for (int i = 0, j = corners.Length - 1; i < corners.Length; j = i++)
            {
                Vector2 a = corners[i], b = corners[j];
                if ((a.y > point.y) != (b.y > point.y) &&
                    point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            texture.SetPixel(x, y, inside ? Color.white : Color.clear);
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
        CloseTracker();
        if (canvas != null && anchorToScreen) canvas.enabled = false;
        if (mapImage != null) mapImage.gameObject.SetActive(false);
    }
    private void OnDestroy()
    {
        topography?.Dispose();
        if (displayRoot != null) Destroy(displayRoot);
        foreach (var sprite in artwork)
        {
            if (sprite == null) continue;
            Destroy(sprite.texture);
            Destroy(sprite);
        }
    }
}

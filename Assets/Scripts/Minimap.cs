using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SilentSuns.Menu;

public class Minimap : MonoBehaviour
{
    [Header("Minimap Settings")]
    [SerializeField] private RawImage mapImage;
    [SerializeField] private string playerName = "Player";
    [SerializeField] private float viewRadius = 40f;
    [SerializeField] private LayerMask terrainLayers;
    [SerializeField] private int textureWidth = 512;
    [SerializeField] private Color backgroundColor = Color.black;
    [Header("Icons")]
    [SerializeField] private Sprite playerIcon;
    [SerializeField] private Color playerIconColor = Color.white;
    [SerializeField, Min(.1f)] private float playerIconScale = 1f;
    [SerializeField, Min(.1f)] private float objectiveIconScale = 1f;
    [SerializeField] private bool clampToEdge = true;
    [Header("Oxygen Circle")]
    [SerializeField] private Sprite circleSprite;
    [SerializeField] private Color circleColor = new Color(.2f, .8f, 1f, 1f);

    private Transform player;
    private PlayerMovement playerMovement;
    private ObjectiveSystem objectiveSystem;
    private RectTransform mapRect;
    private Canvas canvas;
    private Image playerImage;
    private Image oxygenCircle;
    private Camera mapCamera;
    private RenderTexture mapTexture;
    private Sprite generatedCircle;
    private Texture2D generatedCircleTexture;
    private readonly List<(Objective objective, Image icon)> objectiveIcons = new List<(Objective, Image)>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }
    private static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!TerminalGameSession.IsSurveyScene(scene.name) || FindFirstObjectByType<Minimap>() != null) return;
        // The ship player and surface scenes use the same map prefab.
        var prefab = Resources.Load<GameObject>("SurveyMinimap");
        if (prefab != null) Instantiate(prefab);
    }
    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        if (canvas != null) canvas.enabled = false;
        if (mapImage == null) { enabled = false; return; }
        mapRect = mapImage.rectTransform;
        mapImage.raycastTarget = false;
        playerImage = CreateIcon("PlayerIcon", playerIcon, playerIconColor);
        if (circleSprite == null)
        {
            generatedCircle = CreateCircleSprite();
            generatedCircleTexture = generatedCircle.texture;
        }
        oxygenCircle = CreateIcon("OxygenCircle", circleSprite != null ? circleSprite : generatedCircle, circleColor);
        oxygenCircle.rectTransform.SetAsFirstSibling();
        Canvas.ForceUpdateCanvases();
        Vector2 size = mapRect.rect.size;
        int width = Mathf.Clamp(textureWidth, 64, 1024);
        mapTexture = new RenderTexture(width, Mathf.Max(1, Mathf.RoundToInt(width * size.y / Mathf.Max(1, size.x))), 16);
        mapTexture.name = "Survey minimap texture";
        mapImage.texture = mapTexture;
        mapCamera = new GameObject("MinimapCamera").AddComponent<Camera>();
        mapCamera.transform.SetParent(transform, false);
        mapCamera.enabled = false;
        mapCamera.orthographic = true;
        mapCamera.cullingMask = terrainLayers;
        mapCamera.clearFlags = CameraClearFlags.SolidColor;
        mapCamera.backgroundColor = backgroundColor;
        mapCamera.targetTexture = mapTexture;
    }
    private void LateUpdate()
    {
        if (mapImage == null || mapCamera == null) return;
        if (playerMovement == null)
        {
            var namedPlayer = GameObject.Find(playerName);
            playerMovement = namedPlayer != null ? namedPlayer.GetComponent<PlayerMovement>() : null;
            if (playerMovement == null) playerMovement = FindFirstObjectByType<PlayerMovement>();
            player = playerMovement != null ? playerMovement.transform : null;
        }
        bool show = playerMovement != null && playerMovement.CanUseSuitOxygen;
        if (canvas != null) canvas.enabled = show;
        mapCamera.enabled = show;
        mapImage.gameObject.SetActive(show);
        if (!show) return;
        if (objectiveSystem == null) objectiveSystem = FindFirstObjectByType<ObjectiveSystem>();
        SyncIcons();
        // Respect the screen-boundary setting without moving any menu panels.
        mapRect.anchorMin = mapRect.anchorMax = new Vector2(GamePreferences.SafeArea, 1 - GamePreferences.SafeArea);
        Vector2 size = mapRect.rect.size;
        Vector3 position = player.position;
        float yaw = player.eulerAngles.y * Mathf.Deg2Rad;
        float yawX = Mathf.Sin(yaw);
        float yawY = Mathf.Cos(yaw);
        float scale = size.x / (Mathf.Max(1, viewRadius) * 2);
        mapCamera.transform.SetPositionAndRotation(position + Vector3.up * 100, Quaternion.Euler(90, player.eulerAngles.y, 0));
        mapCamera.orthographicSize = size.y / (2 * scale);
        mapCamera.farClipPlane = 200;
        playerImage.rectTransform.sizeDelta = Vector2.one * (size.x * .08f * playerIconScale);
        UpdateOxygenCircle(position, yawX, yawY, scale);
        float iconSize = size.x * .08f * objectiveIconScale;
        Vector2 limit = (size - Vector2.one * iconSize) * .5f;
        foreach (var pair in objectiveIcons)
        {
            Vector3 offset = pair.objective.transform.position - position;
            Vector2 iconPosition = MapPosition(offset, yawX, yawY) * scale;
            Vector2 clamped = new Vector2(Mathf.Clamp(iconPosition.x, -limit.x, limit.x), Mathf.Clamp(iconPosition.y, -limit.y, limit.y));
            pair.icon.gameObject.SetActive(pair.objective.IsRunning && pair.objective.gameObject.activeInHierarchy &&
                (clampToEdge || clamped == iconPosition));
            pair.icon.rectTransform.sizeDelta = Vector2.one * iconSize;
            pair.icon.rectTransform.anchoredPosition = clamped;
        }
    }
    private static Vector2 MapPosition(Vector3 offset, float yawX, float yawY)
    {
        return new Vector2(offset.x * yawY - offset.z * yawX, offset.x * yawX + offset.z * yawY);
    }
    private void SyncIcons()
    {
        // A removed objective can be replaced without changing the list's count.
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
            Color color = objective.objectiveIconColor.a > 0 ? objective.objectiveIconColor : Color.white;
            var icon = CreateIcon("ObjectiveIcon_" + objective.Title, objective.objectiveIcon != null ? objective.objectiveIcon : playerIcon, color);
            objectiveIcons.Add((objective, icon));
        }
        playerImage.rectTransform.SetAsLastSibling();
    }
    private Image CreateIcon(string name, Sprite sprite, Color color)
    {
        var icon = new GameObject(name, typeof(Image)).GetComponent<Image>();
        icon.rectTransform.SetParent(mapRect, false);
        icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = Vector2.one * .5f;
        icon.sprite = sprite;
        icon.color = color;
        icon.raycastTarget = false;
        return icon;
    }
    public void SetPlayerIcon(Sprite sprite, Color color)
    {
        if (playerImage == null) return;
        playerImage.sprite = sprite;
        playerImage.color = color;
    }
    private void UpdateOxygenCircle(Vector3 position, float yawX, float yawY, float scale)
    {
        Transform station = playerMovement.CurrentOxygenStation;
        oxygenCircle.gameObject.SetActive(station != null);
        if (station == null) return;
        oxygenCircle.rectTransform.anchoredPosition = MapPosition(station.position - position, yawX, yawY) * scale;
        oxygenCircle.rectTransform.sizeDelta = Vector2.one * (playerMovement.SafeRadius * 2 * scale);
    }
    private void OnDisable()
    {
        if (canvas != null) canvas.enabled = false;
        if (mapCamera != null) mapCamera.enabled = false;
    }
    private void OnDestroy()
    {
        // These belong to this map instance. Imported icon assets stay in the project.
        if (mapCamera != null) { mapCamera.targetTexture = null; Destroy(mapCamera.gameObject); }
        if (mapTexture != null) { mapTexture.Release(); Destroy(mapTexture); }
        if (generatedCircle != null) Destroy(generatedCircle);
        if (generatedCircleTexture != null) Destroy(generatedCircleTexture);
        if (playerImage != null) Destroy(playerImage.gameObject);
        if (oxygenCircle != null) Destroy(oxygenCircle.gameObject);
        foreach (var pair in objectiveIcons)
            if (pair.icon != null) Destroy(pair.icon.gameObject);
    }
    private static Sprite CreateCircleSprite(int size = 256, float ringThickness = 4f, float fillAlpha = .15f)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float radius = size * .5f;
        Vector2 center = new Vector2(radius, radius);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), center);
            float edge = Mathf.Clamp01(radius - distance);
            float alpha = (distance > radius - ringThickness ? 1 : fillAlpha) * edge;
            tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * .5f);
    }
}

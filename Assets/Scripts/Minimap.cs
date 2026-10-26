using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{
    //References
    [Header("Minimap Settings")]
    [SerializeField] private RawImage mapImage;
    [SerializeField] private string playerName = "player";
    private Transform player;
    private ObjectiveSystem objectiveSystem;
    [SerializeField] private float viewRadius = 40f; //distance between icons and player
    [SerializeField] private LayerMask terrainLayers; //terrain cull layer
    [SerializeField] private int textureWidth = 512; //texture resolution
    [SerializeField] private Color backgroundColor = Color.black;

    [Header("Icons")]
    [SerializeField] private Sprite playerIcon;
    [SerializeField] private Color playerIconColor = Color.white;
    [SerializeField, Min(0.1f)] private float playerIconScale = 1f;
    [SerializeField, Min(0.1f)] private float objectiveIconScale = 1f;
    [SerializeField] private bool clampToEdge = true;

    private RectTransform mapRect;
    private Image playerImage;
    private Camera mapCamera;
    private readonly List<(Objective objective, Image icon)> objectiveIcons = new List<(Objective, Image)>();

    [Header("Oxygen Circle")]
    [SerializeField] private Sprite circleSprite;
    [SerializeField] private Color circleColor = new Color(0.2f, 0.8f, 1f, 1f);

    private PlayerMovement playerMovement;
    private Image oxygenCircle;

    private void Awake()
    {
        //Create icons and find references
        mapRect = mapImage.rectTransform;
        playerImage = CreateIcon("PlayerIcon", playerIcon, playerIconColor);
        objectiveSystem = FindAnyObjectByType<ObjectiveSystem>();
        player = GameObject.Find(playerName)?.transform;
        playerMovement = player.GetComponent<PlayerMovement>();
        oxygenCircle = CreateIcon("OxygenCircle", circleSprite != null ? circleSprite : CreateCircleSprite(), circleColor);
        oxygenCircle.rectTransform.SetAsFirstSibling();

        //Render texture matches the map's shape so nothing stretches
        Canvas.ForceUpdateCanvases();
        Vector2 mapSize = mapRect.rect.size;
        RenderTexture renderTexture = new RenderTexture(textureWidth, Mathf.RoundToInt(textureWidth * mapSize.y / mapSize.x), 16);
        mapImage.texture = renderTexture;

        //Minimap camera settings
        mapCamera = new GameObject("MinimapCamera").AddComponent<Camera>();
        mapCamera.transform.SetParent(transform);
        mapCamera.orthographic = true;
        mapCamera.cullingMask = terrainLayers;
        mapCamera.clearFlags = CameraClearFlags.SolidColor;
        mapCamera.backgroundColor = backgroundColor;
        mapCamera.targetTexture = renderTexture;
    }

    private void OnDestroy(){Destroy(mapCamera.targetTexture);}

    private void LateUpdate()
    {
        if (objectiveIcons.Count != objectiveSystem.Objectives.Count) SyncIcons();
        Vector2 mapSize = mapRect.rect.size;
        Vector3 playerPosition = player.position;
        float yaw = player.eulerAngles.y * Mathf.Deg2Rad;
        float yawX = Mathf.Sin(yaw);
        float yawY = Mathf.Cos(yaw);
        float mapScale = mapSize.x / (viewRadius * 2f);

        //Top down view of terrain, turning with the player
        mapCamera.transform.SetPositionAndRotation(playerPosition + Vector3.up * 100, Quaternion.Euler(90f, player.eulerAngles.y, 0f));
        mapCamera.orthographicSize = mapSize.y / (2f * mapScale);
        mapCamera.farClipPlane = 100 * 2f;

        //Player stays in the center pointing up
        playerImage.rectTransform.sizeDelta = Vector2.one * (mapSize.x * 0.08f * playerIconScale);

        UpdateOxygenCircle(playerPosition, yawX, yawY, mapScale);

        //Objectives
        float objectiveIconPixels = mapSize.x * 0.08f * objectiveIconScale;
        Vector2 objectiveLimit = (mapSize - Vector2.one * objectiveIconPixels) * 0.5f;

        foreach ((Objective objective, Image icon) in objectiveIcons)
        {
            //Offset from the player, rotated so the player's forward points up on the map
            Vector3 worldOffset = objective.transform.position - playerPosition;
            Vector2 iconPosition = new Vector2(worldOffset.x * yawY - worldOffset.z * yawX, worldOffset.x * yawX + worldOffset.z * yawY) * mapScale;
            Vector2 clampedPosition = new Vector2(Mathf.Clamp(iconPosition.x, -objectiveLimit.x, objectiveLimit.x), Mathf.Clamp(iconPosition.y, -objectiveLimit.y, objectiveLimit.y));

            icon.gameObject.SetActive(objective.IsActive && (clampToEdge || clampedPosition == iconPosition));
            icon.rectTransform.sizeDelta = Vector2.one * objectiveIconPixels;
            icon.rectTransform.anchoredPosition = clampedPosition;
        }
    }
    //Creates icons for objectives that are new
    private void SyncIcons()
    {
        for (int index = objectiveIcons.Count; index < objectiveSystem.Objectives.Count; index++)
        {
            Objective newObjective = objectiveSystem.Objectives[index];
            Image newIcon = CreateIcon($"ObjectiveIcon_{newObjective.Title}", newObjective.objectiveIcon, newObjective.objectiveIconColor);
            objectiveIcons.Add((newObjective, newIcon));
        }

        playerImage.rectTransform.SetAsLastSibling();
    }
    //Create icon
    private Image CreateIcon(string iconName, Sprite sprite, Color color)
    {
        Image iconImage = new GameObject(iconName, typeof(Image)).GetComponent<Image>();
        iconImage.rectTransform.SetParent(mapRect, false);
        iconImage.sprite = sprite;
        iconImage.color = color;
        iconImage.raycastTarget = false;
        return iconImage;
    }
    //Change player icon/color
    public void SetPlayerIcon(Sprite sprite, Color color)
    {
        playerImage.sprite = sprite;
        playerImage.color = color;
    }

    //Updates size of curcle
    private void UpdateOxygenCircle(Vector3 playerPosition, float yawX, float yawY, float mapScale)
    {
        Transform station = playerMovement.CurrentOxygenStation;
        oxygenCircle.gameObject.SetActive(station != null);
        if (station == null) return;

        //Use the same objective rotation math
        Vector3 offset = station.position - playerPosition;
        Vector2 position = new Vector2(offset.x * yawY - offset.z * yawX, offset.x * yawX + offset.z * yawY) * mapScale;

        oxygenCircle.rectTransform.anchoredPosition = position;
        oxygenCircle.rectTransform.sizeDelta = Vector2.one * (playerMovement.SafeRadius * 2f * mapScale);
    }

    //Temp ring delete later
    private static Sprite CreateCircleSprite(int size = 256, float ringThickness = 4f, float fillAlpha = 0.15f)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float radius = size * 0.5f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float edge = Mathf.Clamp01(radius - distance);
                float alpha = (distance > radius - ringThickness ? 1f : fillAlpha) * edge;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
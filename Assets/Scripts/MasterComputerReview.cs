using System.Collections;
using SilentSuns.Menu;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Used only by the review scene. It loads the real ship systems and player on an isolated test bench.
public class MasterComputerReview : MonoBehaviour
{
    [SerializeField] private GameObject stationPrefab;
    public MasterComputerStation Station { get; private set; }
    public static bool Ready { get; private set; }

    private void Awake() { Ready = false; }
    private void OnDestroy() { Ready = false; }

    private IEnumerator Start()
    {
        yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Additive);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName("SampleScene"));
        yield return null;
        var player = FindFirstObjectByType<PlayerMovement>();
        var camera = Camera.main;
        if (player == null || camera == null) yield break;
        player.transform.SetParent(null, true);
        player.transform.position = new Vector3(1000, 1000, 1002.9f);
        player.transform.rotation = Quaternion.Euler(0, 180, 0);
        camera.transform.localRotation = Quaternion.identity;
        var body = player.GetComponent<Rigidbody>();
        body.position = player.transform.position;
        body.rotation = player.transform.rotation;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        player.SetGravity(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.015f, .02f, .035f);
        var station = Instantiate(stationPrefab, new Vector3(1000, camera.transform.position.y - .63f, 1000), Quaternion.identity);
        Station = station.GetComponentInChildren<MasterComputerStation>();
        var light = new GameObject("Review bench light", typeof(Light)).GetComponent<Light>();
        light.transform.position = camera.transform.position + Vector3.up;
        light.type = LightType.Point;
        light.range = 8;
        light.intensity = 4;
        light.color = new Color(.65f, .75f, 1);
        Physics.SyncTransforms();
        Ready = true;
        // Only the automated review build uses this flag; normal play keeps control with the player.
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-masterReviewSmoke") >= 0)
            yield return SmokeCheck();
    }

    private IEnumerator SmokeCheck()
    {
        float deadline = Time.realtimeSinceStartup + 12;
        while ((Station.UI == null || !Station.UI.Initialized || Station.UI.Data == null) && Time.realtimeSinceStartup < deadline) yield return null;
        if (Station.UI == null || !Station.UI.Initialized || Station.UI.Data == null)
        {
            Debug.LogError("Master computer did not finish startup or connect to the ship.");
            Application.Quit(1);
            yield break;
        }
        Station.Interact(Camera.main.gameObject);
        while (!Station.UI.Ready && Time.realtimeSinceStartup < deadline) yield return null;
        if (!Station.UI.InUse || !Station.UI.Ready) { Debug.LogError("Master computer interaction did not open."); Application.Quit(1); yield break; }
        Debug.Log("MASTER COMPUTER REVIEW: startup, ship data and screen interaction ready.");
        string capture = System.Environment.GetEnvironmentVariable("SILENT_SUNS_MASTER_CAPTURE");
        if (!string.IsNullOrEmpty(capture)) CaptureFrame(capture);
        Application.Quit(0);
    }

    // A hidden build has no window frame to copy. Render its real cameras into an image instead.
    private void CaptureFrame(string path)
    {
        Canvas.ForceUpdateCanvases();
        var display = Station.GetComponentInParent<MasterComputerDisplay>();
        RenderPipeline.SubmitRenderRequest(display.SourceCamera, new UniversalRenderPipeline.SingleCameraRequest { destination = display.Texture });
        var target = new RenderTexture(1600, 1000, 24);
        target.Create();
        RenderPipeline.SubmitRenderRequest(Camera.main, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        image.Apply();
        System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
        RenderTexture.active = previous;
        Destroy(image);
        target.Release();
        Destroy(target);
    }
}

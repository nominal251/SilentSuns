using SilentSuns.Menu;
using UnityEngine;
using UnityEngine.InputSystem;

// Borrows the player's camera while reading the screen, then puts it back exactly where it was.
[DefaultExecutionOrder(500)]
public class MasterComputerView : MonoBehaviour
{
    private Camera camera;
    private Rigidbody body;
    private RigidbodyConstraints previousConstraints;
    private Vector3 savedPosition, startPosition, focusPosition;
    private Quaternion savedRotation, startRotation, focusRotation;
    private Vector2 look, smoothLook, initialPointer;
    private float elapsed;
    private bool returning, pointerMoved;
    private System.Action finished;
    public bool Active => camera != null;
    public bool Settled => Active && !returning && elapsed >= .55f;

    public void Begin(Renderer screen)
    {
        camera = Camera.main;
        if (camera == null) return;
        savedPosition = camera.transform.localPosition;
        savedRotation = camera.transform.localRotation;
        startPosition = camera.transform.position;
        startRotation = camera.transform.rotation;
        body = camera.GetComponentInParent<Rigidbody>();
        if (body != null)
        {
            previousConstraints = body.constraints;
            body.constraints = RigidbodyConstraints.FreezeAll;
        }
        // Fit the real glass without cropping it on wide or narrow windows.
        float tangent = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * .5f);
        float width = screen.transform.TransformVector(Vector3.right).magnitude;
        float height = screen.transform.TransformVector(Vector3.up).magnitude;
        float distance = Mathf.Max(height / (2 * tangent * .72f), width / (2 * tangent * camera.aspect * .86f));
        focusPosition = screen.transform.position - screen.transform.forward * distance;
        focusRotation = Quaternion.LookRotation(screen.transform.forward, screen.transform.up);
        look = smoothLook = Vector2.zero;
        elapsed = 0; returning = pointerMoved = false;
        initialPointer = Mouse.current == null ? Vector2.zero : Mouse.current.position.ReadValue();
    }

    private void LateUpdate()
    {
        if (camera == null) return;
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.SmoothStep(0, 1, elapsed / .55f);
        if (returning)
        {
            var parent = camera.transform.parent;
            Vector3 position = parent == null ? savedPosition : parent.TransformPoint(savedPosition);
            Quaternion rotation = parent == null ? savedRotation : parent.rotation * savedRotation;
            camera.transform.SetPositionAndRotation(Vector3.Lerp(startPosition, position, t), Quaternion.Slerp(startRotation, rotation, t));
            if (t >= 1) Restore();
            return;
        }
        if (Settled)
        {
            Vector2 input = Vector2.zero;
            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 pointer = mouse.position.ReadValue();
                if ((pointer - initialPointer).sqrMagnitude > 9) pointerMoved = true;
                // Only pan at the edges, and never while dragging a log or scrollbar.
                if (pointerMoved && !mouse.leftButton.isPressed && !mouse.rightButton.isPressed)
                    input = new Vector2(Edge(pointer.x / Screen.width), -Edge(pointer.y / Screen.height));
            }
            if (Gamepad.current != null)
            {
                Vector2 stick = Gamepad.current.rightStick.ReadValue();
                if (stick.magnitude > GamePreferences.DeadZone)
                    input = new Vector2(stick.x, stick.y * (GamePreferences.InvertLook ? 1 : -1)) * GamePreferences.ControllerSensitivity;
            }
            look += input * (38 * Time.unscaledDeltaTime);
            look = new Vector2(Mathf.Clamp(look.x, -8, 8), Mathf.Clamp(look.y, -5, 34));
        }
        smoothLook = Vector2.Lerp(smoothLook, look, 1 - Mathf.Exp(-12 * Time.unscaledDeltaTime));
        camera.transform.SetPositionAndRotation(Vector3.Lerp(startPosition, focusPosition, t),
            Quaternion.Slerp(startRotation, focusRotation * Quaternion.Euler(smoothLook.y, smoothLook.x, 0), t));
    }

    private static float Edge(float value) => value < .14f ? -Mathf.Clamp01((.14f - value) / .14f) : value > .86f ? Mathf.Clamp01((value - .86f) / .14f) : 0;

    public void Return(System.Action onFinished)
    {
        if (camera == null) { onFinished(); return; }
        if (returning) return;
        finished = onFinished;
        returning = true; elapsed = 0;
        startPosition = camera.transform.position;
        startRotation = camera.transform.rotation;
    }

    // Pause, death, and scene changes release immediately so they can own the camera and pointer.
    public void Restore()
    {
        if (camera != null)
        {
            camera.transform.localPosition = savedPosition;
            camera.transform.localRotation = savedRotation;
        }
        if (body != null) body.constraints = previousConstraints;
        camera = null; body = null;
        var callback = finished; finished = null;
        callback?.Invoke();
    }
    private void OnDisable() => Restore();
}

using UnityEngine;
using UnityEngine.Events;

public class TransformTool : MonoBehaviour
{
    public enum StartAction { Nothing, MoveToA, MoveToB, FlipFlop }

    [Header("Object Start Position")]
    public bool useLocalPosition = true;
    public Transform baseTransform;
    public Vector3 basePosition;

    [Header("Rotation")]
    // Off by default so existing objects keep their current rotation.
    public bool useRotation = false;
    // Starting orientation (Euler), in the same space as the position (local, world, or relative to baseTransform).
    public Vector3 baseRotation;

    [Header("Path (offsets and rotate from base)")]
    public Vector3 pointApos = Vector3.zero;
    public Vector3 pointArot = Vector3.zero;
    public Vector3 pointB = new Vector3(3f, 0f, 0f);
    public Vector3 pointBrot = new Vector3(0f, 0f, 0f);

    [Header("Motion")]
    [Min(0.01f)] public float duration = 2f;
    public AnimationCurve easing = AnimationCurve.Linear(0, 0, 1, 1);
    //flip flop when reaching other point with pause time
    public bool autoFlipFlop = false;
    [Min(0f)] public float endPause = 0f;

    [Header("On Start")]
    //Snap to first point at start?
    public bool snapToAOnStart = true;
    public StartAction startAction = StartAction.Nothing;

    [Header("Events")]
    public UnityEvent onReachedA;
    public UnityEvent onReachedB;

    // 0 = A, 1 = B how far along the path we are, 0-1
    float progress;      
    // 0 = heading to A, 1 = heading to B
    float target = 1f;   
    bool moving;
    float pause;

    Transform Base => baseTransform && !baseTransform.IsChildOf(transform) ? baseTransform : null;

    public Vector3 PointAWorld => ToWorld(pointApos);
    public Vector3 PointBWorld => ToWorld(pointB);

    public void MoveToA() => MoveTo(0f);
    public void MoveToB() => MoveTo(1f);

    //Move to the other point, or if already moving, move to the opposite point
    public void FlipFlop()
    {
        float heading = moving ? target : (progress >= 0.5f ? 1f : 0f);
        MoveTo(heading > 0.5f ? 0f : 1f);
    }

    public void Pause() => moving = false;
    public void Resume() { if (!Mathf.Approximately(progress, target)) moving = true; }
    public void SnapToA() => Snap(0f);
    public void SnapToB() => Snap(1f);

    bool UseParentSpace => useLocalPosition && transform.parent;
    Vector3 ToSpace(Vector3 w) => UseParentSpace ? transform.parent.InverseTransformPoint(w) : w;
    Vector3 FromSpace(Vector3 p) => UseParentSpace ? transform.parent.TransformPoint(p) : p;

    public Quaternion HandleRotation =>
        Base ? Base.rotation : UseParentSpace ? transform.parent.rotation : Quaternion.identity;
    public Vector3 BasePosition => ToWorld(Vector3.zero);
    public Vector3 ToWorld(Vector3 offset) =>
        Base ? Base.TransformPoint(offset) : FromSpace(basePosition + offset);
    public Vector3 ToOffset(Vector3 world) =>
        Base ? Base.InverseTransformPoint(world) : ToSpace(world) - basePosition;
    public void SetBaseFromWorld(Vector3 world) => basePosition = ToSpace(world);

    //Rotation relative to the base
    Quaternion LocalRotation(float e) =>
        Quaternion.Euler(baseRotation) * Quaternion.Euler(Vector3.LerpUnclamped(pointArot, pointBrot, e));

    Quaternion WorldRotation(float e)
    {
        Quaternion local = LocalRotation(e);
        if (Base) return Base.rotation * local;
        if (UseParentSpace) return transform.parent.rotation * local;
        return local;
    }

    //Move to a target position
    void MoveTo(float time)
    {
        target = time;
        pause = 0f;
        moving = !Mathf.Approximately(progress, time);
    }

    //Snap to an area when not moving, and apply the position immediately
    void Snap(float time)
    {
        progress = target = time;
        moving = false;
        Apply();
    }

    //Apply the current progress to the transform position (and rotation, if enabled)
    void Apply()
    {
        float ease = easing.Evaluate(progress);
        if (useLocalPosition && !Base)
            transform.localPosition = Vector3.LerpUnclamped(basePosition + pointApos, basePosition + pointB, ease);
        else
            transform.position = Vector3.LerpUnclamped(PointAWorld, PointBWorld, ease);

        if (!useRotation) return;

        if (useLocalPosition && !Base)
            transform.localRotation = LocalRotation(ease);
        else
            transform.rotation = WorldRotation(ease);
    }

    void Reset() => CaptureBaseFromCurrent();

    [ContextMenu("Set Base From Current Position")]
    void CaptureBaseFromCurrent()
    {
        basePosition = useLocalPosition ? transform.localPosition : transform.position;
        baseRotation = useLocalPosition ? transform.localEulerAngles : transform.eulerAngles;
    }

    //Start action on start
    void Start()
    {
        if (snapToAOnStart) Snap(0f);
        if (startAction == StartAction.MoveToA) MoveToA();
        else if (startAction == StartAction.MoveToB) MoveToB();
        else if (startAction == StartAction.FlipFlop) FlipFlop();
    }

    //Update is called once per frame
    void Update()
    {
        if (!moving) return;
        if (pause > 0f) { pause -= Time.deltaTime; return; }

        progress = Mathf.MoveTowards(progress, target, Time.deltaTime / duration);
        Apply();
        if (!Mathf.Approximately(progress, target)) return;

        // Arrived
        progress = target;
        moving = false;
        (target > 0.5f ? onReachedB : onReachedA)?.Invoke();

        if (autoFlipFlop) { FlipFlop(); pause = endPause; }
    }

    //Draw position A line to position B in the scene view
    void OnDrawGizmos()
    {
        Vector3 a = PointAWorld, b = PointBWorld;

        Gizmos.color = new Color(1f, 0.85f, 0.2f);
        Gizmos.DrawLine(a, b);
        Gizmos.color = new Color(0.2f, 0.9f, 0.3f);
        Gizmos.DrawSphere(a, .2f);
        Gizmos.color = new Color(0.95f, 0.3f, 0.25f);
        Gizmos.DrawSphere(b, .2f);
    }
}
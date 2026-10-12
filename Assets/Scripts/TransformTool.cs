using UnityEngine;
using UnityEngine.Events;
using UnityEditor;

public class TransformTool : MonoBehaviour
{
    public enum StartAction { Nothing, MoveToA, MoveToB, FlipFlop }

    [Header("Object Start Position")]
    public bool useLocalPosition = true;
    public Transform baseTransform;
    public Vector3 basePosition;

    [Header("Path (offsets from base)")]
    public Vector3 pointA = Vector3.zero;
    public Vector3 pointB = new Vector3(3f, 0f, 0f);

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

    public Vector3 PointAWorld => ToWorld(pointA);
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
        baseTransform ? baseTransform.rotation : UseParentSpace ? transform.parent.rotation : Quaternion.identity;
    public Vector3 BasePosition => ToWorld(Vector3.zero);
    public Vector3 ToWorld(Vector3 offset) =>
        baseTransform ? baseTransform.TransformPoint(offset) : FromSpace(basePosition + offset);
    public Vector3 ToOffset(Vector3 world) =>
        baseTransform ? baseTransform.InverseTransformPoint(world) : ToSpace(world) - basePosition;
    public void SetBaseFromWorld(Vector3 world) => basePosition = ToSpace(world);

    //Move to a target position
    void MoveTo(float t)
    {
        target = t;
        pause = 0f;
        moving = !Mathf.Approximately(progress, t);
    }

    //Snap to an area when not moving, and apply the position immediately
    void Snap(float t)
    {
        progress = target = t;
        moving = false;
        Apply();
    }

    //Apply the current progress to the transform position
    void Apply()
    {
        float e = easing.Evaluate(progress);
        if (useLocalPosition && !baseTransform)
            transform.localPosition = Vector3.LerpUnclamped(basePosition + pointA, basePosition + pointB, e);
        else
            transform.position = Vector3.LerpUnclamped(PointAWorld, PointBWorld, e);
    }

    void Reset() => basePosition = useLocalPosition ? transform.localPosition : transform.position;

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
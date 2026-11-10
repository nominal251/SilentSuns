using UnityEngine;

public class LadderClimber : MonoBehaviour
{
    //Climb settings
    public float climbSpeed = 2.5f;
    public float snapSpeed = 8f;
    public float dismountPush = 3f;
    public Ladder CurrentLadder { get; private set; }
    public bool IsClimbing => CurrentLadder != null;

    private Rigidbody rb;
    private PlayerMovement movement;
    private InputHandler input;
    private bool rbWasKinematic;
    private bool rbHadGravity;
    private float distance; //distance along the ladder from bottomPoint
    private int mountFrame = -1;
    private int dismountFrame = -1;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<PlayerMovement>();
    }

    void Start()
    {
        input = InputHandler.instance;
    }

    //attack player to ladder
    public void Mount(Ladder ladder)
    {
        if (IsClimbing || ladder == null) return;
        if (Time.frameCount == dismountFrame) return;
        if (movement != null && !movement.IsOnFoot) return;

        CurrentLadder = ladder;
        mountFrame = Time.frameCount;

        Vector3 a = ladder.bottomPoint.position;
        Vector3 ab = ladder.topPoint.position - a;
        distance = Mathf.Clamp(Vector3.Dot(transform.position - a, ab.normalized), 0f, ab.magnitude);

        if (movement != null) movement.movementLocked = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rbWasKinematic = rb.isKinematic;
        rbHadGravity = rb.useGravity;
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    //push player out upon dismounting
    public void Dismount(bool pushAway)
    {
        if (!IsClimbing) return;
        Ladder ladder = CurrentLadder;
        CurrentLadder = null;
        dismountFrame = Time.frameCount;

        rb.isKinematic = rbWasKinematic;
        rb.useGravity = rbHadGravity;
        if (movement != null) movement.movementLocked = false;

        if (pushAway)
            rb.linearVelocity = ladder.transform.forward * dismountPush;
    }

    //On update check for climbing and if player jumped off
    void Update()
    {
        if (!IsClimbing) return;

        if (SilentSuns.Menu.TerminalPause.BlocksGameplayInput ||
            SilentSuns.Menu.TerminalToolInput.BlocksInput)
            return;

        if (movement != null && !movement.IsOnFoot)
        {
            Dismount(false);
            return;
        }

        if (input == null) input = InputHandler.instance;
        if (input == null) return;

        if (input.interactPressed && Time.frameCount != mountFrame)
        {
            Dismount(false);
            return;
        }

        if (input.jumpPressed)
        {
            Dismount(true);
            return;
        }

        Vector3 a = CurrentLadder.bottomPoint.position;
        Vector3 ab = CurrentLadder.topPoint.position - a;
        float length = ab.magnitude;
        Vector3 dir = ab / length;

        float vertical = input.moveInput.z;
        if (vertical > 0.1f && distance >= length - 0.01f && CurrentLadder.topExitPoint != null)
        {
            SetPosition(CurrentLadder.topExitPoint.position);
            Dismount(false);
            return;
        }

        if (vertical < -0.1f && distance <= 0.01f)
        {
            Dismount(false);
            return;
        }

        distance = Mathf.Clamp(distance + vertical * climbSpeed * Time.deltaTime, 0f, length);

        Vector3 target = a + dir * distance + CurrentLadder.transform.forward * CurrentLadder.standOffset;
        SetPosition(Vector3.MoveTowards(transform.position, target, snapSpeed * Time.deltaTime));
    }
    //Set position on ladder
    void SetPosition(Vector3 pos)
    {
        transform.position = pos;
        rb.position = pos;
    }
}
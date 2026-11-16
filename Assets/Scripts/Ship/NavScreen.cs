using UnityEngine;
using UnityEngine.InputSystem;

public class NavScreen : MonoBehaviour
{
    public Transform horizontalLine;
    public Transform verticalLine;
    public Transform reticleCenter;
    public Transform shipPos;

    private bool isMoving = false;

    public Button leftButton;
    public Button rightButton;
    public Button upButton;
    public Button downButton;

    public Button startButton;
    public Button stopButton;

    public float reticleMoveSpeed = 5f;
    public float shipMoveSpeed = 1f;

    public float movingOpacity = 0.25f;
    public float normalOpacity = 1f;

    private Renderer horizontalRenderer;
    private Renderer verticalRenderer;

    void Awake()
    {
        horizontalRenderer = horizontalLine.GetComponent<Renderer>();
        verticalRenderer = verticalLine.GetComponent<Renderer>();
    }

    void Update()
    {
        if (Keyboard.current == null)
            return;

        // horizontal line up/down
        if (upButton.activated && !isMoving)
            horizontalLine.position += Vector3.up * reticleMoveSpeed * Time.deltaTime;

        if (downButton.activated && !isMoving)
            horizontalLine.position += Vector3.down * reticleMoveSpeed * Time.deltaTime;

        // vertical line left/right
        if (leftButton.activated && !isMoving)
            verticalLine.position += Vector3.left * reticleMoveSpeed * Time.deltaTime;

        if (rightButton.activated && !isMoving)
            verticalLine.position += Vector3.right * reticleMoveSpeed * Time.deltaTime;

        // central dot position
        reticleCenter.position = new Vector3(
            verticalLine.position.x,
            horizontalLine.position.y,
            reticleCenter.position.z
        );

        if (startButton.activated)
        {
            isMoving = true;
        }

        if (stopButton.activated)
        {
            isMoving = false;
        }

        // change line opacity
        Color horizontalColor = horizontalRenderer.material.color;
        horizontalColor.a = isMoving ? movingOpacity : normalOpacity;
        horizontalRenderer.material.color = horizontalColor;

        Color verticalColor = verticalRenderer.material.color;
        verticalColor.a = isMoving ? movingOpacity : normalOpacity;
        verticalRenderer.material.color = verticalColor;

        if (isMoving)
        {
            Vector3 targetPosition = new Vector3(
                reticleCenter.position.x,
                reticleCenter.position.y,
                shipPos.position.z
            );

            shipPos.position = Vector3.MoveTowards(
                shipPos.position,
                targetPosition,
                shipMoveSpeed * Time.deltaTime
            );

            if (Mathf.Approximately(shipPos.position.x, targetPosition.x) &&
                Mathf.Approximately(shipPos.position.y, targetPosition.y))
            {
                isMoving = false;
            }
        }
    }
}
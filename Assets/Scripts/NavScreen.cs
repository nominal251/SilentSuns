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

    public float moveSpeed = 5f;

    void Update()
    {
        if (Keyboard.current == null)
            return;

        // horizontal line up/down
        if (upButton.activated)
            horizontalLine.position += Vector3.up * moveSpeed * Time.deltaTime;

        if (downButton.activated)
            horizontalLine.position += Vector3.down * moveSpeed * Time.deltaTime;

        // vertical line left/right
        if (leftButton.activated)
            verticalLine.position += Vector3.left * moveSpeed * Time.deltaTime;

        if (rightButton.activated)
            verticalLine.position += Vector3.right * moveSpeed * Time.deltaTime;

        // central dot position
        reticleCenter.position = new Vector3(
            verticalLine.position.x,
            horizontalLine.position.y,
            reticleCenter.position.z
        );
    }
}
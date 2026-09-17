using UnityEngine;
using UnityEngine.InputSystem;

public class NavScreen : MonoBehaviour
{
    public Transform horizontalLine;
    public Transform verticalLine;
    //public Transform dot;

    public float moveSpeed = 5f;

    void Update()
    {
        if (Keyboard.current == null)
            return;

        // horizontal line up/down
        if (Keyboard.current.upArrowKey.isPressed)
            horizontalLine.position += Vector3.up * moveSpeed * Time.deltaTime;

        if (Keyboard.current.downArrowKey.isPressed)
            horizontalLine.position += Vector3.down * moveSpeed * Time.deltaTime;

        // vertical line left/right
        if (Keyboard.current.leftArrowKey.isPressed)
            verticalLine.position += Vector3.left * moveSpeed * Time.deltaTime;

        if (Keyboard.current.rightArrowKey.isPressed)
            verticalLine.position += Vector3.right * moveSpeed * Time.deltaTime;

        // central dot position (unused for now)
        /*dot.position = new Vector3(
            verticalLine.position.x,
            horizontalLine.position.y,
            dot.position.z
        );*/
    }
}
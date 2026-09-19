using UnityEngine;

public class Pursuer : MonoBehaviour
{
    public Transform shipPos;
    public float moveSpeed;

    // Update is called once per frame
    void Update()
    {
        Vector3 targetPosition = new Vector3(
                shipPos.position.x,
                shipPos.position.y,
                shipPos.position.z
            );

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );
    }
}

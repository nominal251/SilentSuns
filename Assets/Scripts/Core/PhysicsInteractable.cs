using UnityEngine;

public class PhysicsInteractable : MonoBehaviour, Interactable
{
    private Rigidbody rBody;

    private void Awake()
    {
        rBody = gameObject.GetComponent<Rigidbody>();
    }

    public void Interact(GameObject interactionSource)
    {
        rBody.isKinematic = true;
        rBody.detectCollisions = false;
        transform.SetParent(interactionSource.transform);
        transform.localPosition = new Vector3();
    }

    public void StopInteract()
    {
        rBody.isKinematic = false;
        rBody.detectCollisions = true;
        transform.SetParent(null);
    }
}

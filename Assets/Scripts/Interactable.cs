using UnityEngine;

public interface Interactable
{
    void Interact(GameObject interactionSource); //runs when pressing interact on an interactable
    void StopInteract(); //runs when the interact key is released on an interactable
}
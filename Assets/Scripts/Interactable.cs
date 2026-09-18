using UnityEngine;

public interface Interactable
{
    void Interact(); //runs when pressing interact on an interactable
    void StopInteract(); //runs when the interact key is released on an interactable
}
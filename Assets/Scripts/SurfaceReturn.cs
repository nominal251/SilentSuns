using UnityEngine;

public class SurfaceReturn : MonoBehaviour
{
    [SerializeField, Min(0f)] private float delay = 0f;

    public void ReturnToShip()
    {
        if (delay <= 0f) SceneSwap.ReturnToShip();
        else Invoke(nameof(Return), delay);
    }

    private void Return() => SceneSwap.ReturnToShip();
}

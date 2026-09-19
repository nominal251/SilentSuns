using UnityEngine;

public class Planet : MonoBehaviour
{
    public string name;

    public float Radius
    {
        get
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                return spriteRenderer.bounds.extents.x;
            }

            return 0f;
        }
    }
}
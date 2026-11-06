using UnityEngine;

public class TPiece : MonoBehaviour
{
    public int[,] Shape { get; private set; }

    public int Width => Shape.GetLength(0);
    public int Height => Shape.GetLength(1);

    private void Awake()
    {
        Shape = new int[,]
        {
            {0,1,0 },
            {1,1,1 }
        };
    }
}

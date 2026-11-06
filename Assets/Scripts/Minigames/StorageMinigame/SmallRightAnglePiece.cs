using UnityEngine;

public class SmallRightAnglePiece : MonoBehaviour
{
    public int[,] Shape { get; private set; }

    public int Width => Shape.GetLength(0);
    public int Height => Shape.GetLength(1);

    private void Awake()
    {
        Shape = new int[,]
        {
            {1,1 },
            {0,1 }
        };
    }
}

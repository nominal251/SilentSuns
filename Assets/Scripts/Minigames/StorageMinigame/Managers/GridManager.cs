using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [Tooltip("Just in case we want to make it bigger")]
    [SerializeField] private int gridWidth = 4;
    [SerializeField] private int gridHeight = 4;

    [Header("Grid References")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private Canvas canvas;

    private bool[,] occupiedCells;
    //private GridCell[,] gridCells;

    private void Start()
    {
        CreateGrid();
    }

    private void CreateGrid()
    {
        occupiedCells = new bool[gridWidth, gridHeight];
        //gridCells = new GridCell[gridWidtch, gridHeight];
    }
}

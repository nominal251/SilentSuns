
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private RectTransform gridPanel;
    [SerializeField] private int gridWidth = 4;
    [SerializeField] private int gridHeight = 4;

    [SerializeField] private float cellSize = 80f;
    [SerializeField] private float spacing = 5f;

    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private StoragePiece[] pieces;
    [SerializeField] private SurfaceObjectiveTracker surfaceObjectiveTracker; // SURFACE OBJECTIVE TRACKER CODE

    [SerializeField] private StorageMinigame storageMinigame;

    [Header("Grid Appearance")]
    [SerializeField] private Color gridColor = Color.gray;

    private bool success = false;

    // Tracks which piece occupies each grid cell.
    private StoragePiece[,] occupiedCells;

    private bool puzzleSolved;

    private float CellStep => cellSize + spacing;

    private void Start()
    {
        gridPanel = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        occupiedCells = new StoragePiece[gridWidth, gridHeight];

        CreateGrid();

        // Initialize all pieces.
        foreach (StoragePiece piece in pieces)
        {
            piece.BuildVisuals(cellSize, spacing);

            PieceDragHandler dragHandler =
                piece.GetComponent<PieceDragHandler>();

            dragHandler.Initialize(
                this,
                canvas.GetComponent<RectTransform>()
            );
        }

        if (surfaceObjectiveTracker == null) // SURFACE OBJECTIVE TRACKER CODE
        {
            GameObject tmp = GameObject.Find("SurfaceObjectiveTracker"); // SURFACE OBJECTIVE TRACKER CODE 
            surfaceObjectiveTracker = tmp.GetComponent<SurfaceObjectiveTracker>(); // SURFACE OBJECTIVE TRACKER CODE
        }

        if (storageMinigame == null)
        {
            storageMinigame = GetComponentInParent<StorageMinigame>();
        }
    }

    private void CreateGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GameObject cell = new GameObject(
                    "GridCell",
                    typeof(RectTransform),
                    typeof(Image)
                );

                cell.transform.SetParent(gridPanel, false);

                RectTransform rect =
                    cell.GetComponent<RectTransform>();

                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                rect.sizeDelta = new Vector2(cellSize, cellSize);

                rect.anchoredPosition = GetCellLocalPosition(
                    new Vector2Int(x, y)
                );

                Image image = cell.GetComponent<Image>();
                image.color = gridColor;

                // Grid backgrounds should not intercept mouse input.
                image.raycastTarget = false;
            }
        }
    }

    private Vector2 GetCellLocalPosition(Vector2Int position)
    {
        float startX = -gridPanel.rect.width / 2f + cellSize / 2f;
        float startY = -gridPanel.rect.height / 2f + cellSize / 2f;

        return new Vector2(
            startX + position.x * CellStep,
            startY + position.y * CellStep
        );
    }

    private Vector3 GetCellWorldPosition(Vector2Int position)
    {
        Vector2 localPosition = GetCellLocalPosition(position);

        return gridPanel.TransformPoint(localPosition);
    }

    // Converts a piece's world position into a grid coordinate.
    public bool TryGetGridOrigin(
        Vector3 worldPosition,
        out Vector2Int origin)
    {
        Vector3 localPosition =
            gridPanel.InverseTransformPoint(worldPosition);

        float startX = -gridPanel.rect.width / 2f + cellSize / 2f;
        float startY = -gridPanel.rect.height / 2f + cellSize / 2f;

        int x = Mathf.RoundToInt(
            (localPosition.x - startX) / CellStep
        );

        int y = Mathf.RoundToInt(
            (localPosition.y - startY) / CellStep
        );

        origin = new Vector2Int(x, y);

        return true;
    }

    // Checks whether a piece can occupy a particular position.
    public bool CanPlacePiece(
        StoragePiece piece,
        Vector2Int origin)
    {
        foreach (Vector2Int cell in piece.Shape)
        {
            Vector2Int position = origin + cell;

            // Check grid boundaries.
            if (position.x < 0 || position.x >= gridWidth ||
                position.y < 0 || position.y >= gridHeight)
            {
                return false;
            }

            // Check whether another piece occupies this cell.
            if (occupiedCells[position.x, position.y] != null)
            {
                return false;
            }
        }

        return true;
    }

    // Places a piece into the grid.
    public bool TryPlacePiece(
        StoragePiece piece,
        Vector2Int origin)
    {
        if (!CanPlacePiece(piece, origin))
            return false;

        foreach (Vector2Int cell in piece.Shape)
        {
            Vector2Int position = origin + cell;

            occupiedCells[position.x, position.y] = piece;
        }

        piece.SetPlacement(origin, true);

        // Snap the piece to the grid.
        piece.transform.position = GetCellWorldPosition(origin);

        CheckPuzzleComplete();

        return true;
    }

    // Removes a piece from its current grid position.
    public void RemovePiece(StoragePiece piece)
    {
        if (!piece.isPlaced)
            return;

        foreach (Vector2Int cell in piece.Shape)
        {
            Vector2Int position = piece.GridOrigin + cell;

            if (position.x >= 0 && position.x < gridWidth &&
                position.y >= 0 && position.y < gridHeight)
            {
                if (occupiedCells[position.x, position.y] == piece)
                {
                    occupiedCells[position.x, position.y] = null;
                }
            }
        }

        piece.SetPlacement(piece.GridOrigin, false);
    }

    public bool IsInsideGrid(StoragePiece piece, Vector2Int origin)
    {
        foreach (Vector2Int cell in piece.Shape)
        {
            Vector2Int position = origin + cell;

            if (position.x < 0 || position.x >= gridWidth ||
                position.y < 0 || position.y >= gridHeight)
            {
                return false;
            }
        }

        return true;
    }

    // Checks whether every piece is in its correct position.
    public bool CheckPuzzleComplete()
    {
        if (puzzleSolved)
            return false;

        if (!IsGridFull())
            return false;

        puzzleSolved = true;
        Debug.Log("Puzzle completed!");
        storageMinigame.CompletePuzzle();
        return true;
    }

    private bool IsGridFull()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (occupiedCells[x, y] == null)
                {
                    return false;
                }
            }
        }

        return true;
    }
}

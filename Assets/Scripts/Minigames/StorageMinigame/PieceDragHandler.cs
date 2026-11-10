using UnityEngine;
using UnityEngine.EventSystems;

public class PieceDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private GridManager gridManager;

    private RectTransform rectTransform;
    private RectTransform canvasRect;

    private Vector3 homePosition;   // where the piece started in the scene
    private Vector3 dragOffset;

    private Vector2Int originalGridPosition;

    private bool wasPlaced;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(GridManager manager, RectTransform canvasTransform) //initialized in grid manager
    {
        gridManager = manager;
        canvasRect = canvasTransform;

        // Remember the starting position once, so it never gets overwritten by later drags
        homePosition = rectTransform.position;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        StoragePiece piece = GetComponent<StoragePiece>();

        wasPlaced = piece.isPlaced;
        originalGridPosition = piece.GridOrigin;

        //removes from grid if it is already placed
        if (wasPlaced)
        {
            gridManager.RemovePiece(piece);
        }

        //mouse offset calculations
        RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out Vector3 pointerWorldPosition);
        dragOffset = rectTransform.position - pointerWorldPosition;

        rectTransform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out Vector3 pointerWorldPosition);

        rectTransform.position = pointerWorldPosition + dragOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        StoragePiece piece = GetComponent<StoragePiece>();

        //find the nearest coordinates
        gridManager.TryGetGridOrigin(rectTransform.position, out Vector2Int newPosition);

        //attempt to place piece
        if (gridManager.TryPlacePiece(piece, newPosition))
            return;

        bool outsideGrid = !gridManager.IsInsideGrid(piece, newPosition);

        if (outsideGrid || !wasPlaced)
        {
            //dropped outside the grid (or was never placed): go back to the starting position
            rectTransform.position = homePosition;
        }
        else
        {
            //dropped on top of another piece: go back to the previous grid spot
            gridManager.TryPlacePiece(piece, originalGridPosition);
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;

public class PieceDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private GridManager gridManager;

    private RectTransform rectTransform;
    private RectTransform canvasRect;

    private Vector3 originalPosition;
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
    }

    public void OnBeginDrag(PointerEventData eventData)
    {

        //remember original position
        originalPosition = rectTransform.position;

        //if it was placed now we know
        wasPlaced = GetComponent<StoragePiece>().isPlaced;

        StoragePiece piece = GetComponent<StoragePiece>();

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
        bool placed = gridManager.TryPlacePiece(piece, newPosition);

        if (!placed)
        {
            //restore to previous position
            if (wasPlaced)
            {
                gridManager.TryPlacePiece(piece, originalGridPosition);
            }
            else
            {
                //else return to start pos outside of grid
                rectTransform.position = originalPosition;
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

public class StoragePiece : MonoBehaviour
{
    [Header("Piece Shape")]
    [SerializeField] private Vector2Int[] shape;

    [Header("Correct Placement")]
    [SerializeField] private Vector2Int correctPosition;

    [Header("Visuals")]
    [SerializeField] private Color pieceColor = Color.white;

    public Vector2Int[] Shape => shape;
    public Vector2Int CorrectPosition => correctPosition;

    public Vector2Int GridOrigin { get; private set; }
    public bool isPlaced { get; private set; }

    public void BuildVisuals(float cellSize, float spacing)
    {
        //calculates the distance between starting position and squares to build the pieces
        float step = cellSize + spacing;

        foreach (Vector2Int cell in shape)
        {
            GameObject square = new GameObject("PieceCell", typeof(RectTransform), typeof(Image)); //creates new gameobject with recttransform and image

            square.transform.SetParent(transform, false);

            RectTransform rect = square.GetComponent<RectTransform>();

            //sets anchor to center of parent
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            //sets height and width of square
            rect.sizeDelta = new Vector2(cellSize, cellSize);

            rect.anchoredPosition = new Vector2(cell.x * step, cell.y * step);

            Image image = square.GetComponent<Image>();
            image.color = pieceColor;

            // Allows the parent piece to receive drag events.
            image.raycastTarget = true;

            Debug.Log("Created raycastable square: " + square.name);
        }
    }

    public void SetPlacement(Vector2Int origin, bool placed)
    {
        GridOrigin = origin;
        isPlaced = placed;
    }
}

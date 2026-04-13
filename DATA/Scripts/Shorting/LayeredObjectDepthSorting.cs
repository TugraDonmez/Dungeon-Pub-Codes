using UnityEngine;

/// <summary>
/// Layered objeler için gelişmiş depth sorting (çok katmanlı objeler)
/// </summary>
public class LayeredObjectDepthSorting : MonoBehaviour
{
    [System.Serializable]
    public class SortingLayer
    {
        public SpriteRenderer renderer;
        public int orderOffset; // Temel order'a göre offset
        public bool affectedByCharacters = true; // Karakterlerden etkilenir mi
    }

    [Header("Layered Object Settings")]
    [SerializeField] private SortingLayer[] layers;
    [SerializeField] private Vector2 sortingPosition;
    [SerializeField] private bool useBottomOfObject = true;

    private int baseSortingOrder;

    private void Start()
    {
        CalculateBaseSortingOrder();
        UpdateLayerSorting();
    }

    private void CalculateBaseSortingOrder()
    {
        if (DepthSortingManager.Instance != null)
        {
            Vector2 sortPos = useBottomOfObject ? GetBottomPosition() : (Vector2)transform.position;
            baseSortingOrder = DepthSortingManager.Instance.CalculateSortingOrder(sortPos + sortingPosition);
        }
    }

    private Vector2 GetBottomPosition()
    {
        if (layers.Length > 0 && layers[0].renderer != null)
        {
            Bounds bounds = layers[0].renderer.bounds;
            return new Vector2(bounds.center.x, bounds.min.y);
        }
        return transform.position;
    }

    private void UpdateLayerSorting()
    {
        foreach (var layer in layers)
        {
            if (layer.renderer != null)
            {
                layer.renderer.sortingOrder = baseSortingOrder + layer.orderOffset;
            }
        }
    }

    /// <summary>
    /// Karakter etkileşimi için sorting order güncelle
    /// </summary>
    public void UpdateSortingForCharacterInteraction(Vector2 characterPosition)
    {
        Vector2 objectPosition = GetBottomPosition() + sortingPosition;

        foreach (var layer in layers)
        {
            if (layer.affectedByCharacters && layer.renderer != null)
            {
                if (characterPosition.y < objectPosition.y)
                {
                    // Karakter objenin önünde
                    layer.renderer.sortingOrder = baseSortingOrder + layer.orderOffset + 1;
                }
                else
                {
                    // Karakter objenin arkasında
                    layer.renderer.sortingOrder = baseSortingOrder + layer.orderOffset - 1;
                }
            }
        }
    }
}
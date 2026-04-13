using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Statik objeler (ağaçlar, binalar vs.) için depth sorting
/// </summary>
public class StaticObjectDepthSorting : MonoBehaviour, IDepthSortable
{
    [Header("Static Object Settings")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Vector2 sortingOffset = Vector2.zero;
    [SerializeField] private bool useBottomOfSprite = true; // Sprite'ın altını referans al
    [SerializeField] private Transform customSortingPoint;

    [Header("Character Interaction")]
    [SerializeField] private bool allowCharactersBehind = true; // Karakterler arkaya geçebilir
    [SerializeField] private bool allowCharactersInFront = true; // Karakterler öne çıkabilir
    [SerializeField] private int baseSortingOrder = 0; // Objenin temel sorting order'ı

    private void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Statik objeler için sorting order'ı pozisyona göre hesapla
        CalculateBaseSortingOrder();

        if (DepthSortingManager.Instance != null)
        {
            DepthSortingManager.Instance.RegisterSortableObject(this);
        }
    }

    private void CalculateBaseSortingOrder()
    {
        Vector2 sortingPos = GetSortingPosition();
        baseSortingOrder = DepthSortingManager.Instance.CalculateSortingOrder(sortingPos);
    }

    public Vector2 GetSortingPosition()
    {
        Vector2 position;

        if (customSortingPoint != null)
        {
            position = customSortingPoint.position;
        }
        else if (useBottomOfSprite && spriteRenderer != null)
        {
            // Sprite'ın alt kısmını referans al
            Bounds bounds = spriteRenderer.bounds;
            position = new Vector2(bounds.center.x, bounds.min.y);
        }
        else
        {
            position = transform.position;
        }

        return position + sortingOffset;
    }

    public void SetSortingOrder(int order)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = order;
        }
    }

    public bool IsActive()
    {
        return gameObject.activeInHierarchy;
    }
}


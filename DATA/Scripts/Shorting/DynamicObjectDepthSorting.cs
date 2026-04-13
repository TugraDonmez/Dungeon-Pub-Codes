using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dinamik objeler için depth sorting (hareketli objeler)
/// </summary>
public class DynamicObjectDepthSorting : MonoBehaviour, IDepthSortable
{
    [Header("Dynamic Object Settings")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Vector2 sortingOffset = Vector2.zero;
    [SerializeField] private bool useRigidbodyPosition = false;

    private Rigidbody2D rb;
    private Vector2 lastPosition;

    private void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (useRigidbodyPosition)
            rb = GetComponent<Rigidbody2D>();

        if (DepthSortingManager.Instance != null)
        {
            DepthSortingManager.Instance.RegisterSortableObject(this);
        }

        lastPosition = transform.position;
    }

    public Vector2 GetSortingPosition()
    {
        Vector2 position = useRigidbodyPosition && rb != null ?
            rb.position : (Vector2)transform.position;
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

    private void OnDisable()
    {
        if (DepthSortingManager.Instance != null)
        {
            DepthSortingManager.Instance.UnregisterSortableObject(this);
        }
    }
}
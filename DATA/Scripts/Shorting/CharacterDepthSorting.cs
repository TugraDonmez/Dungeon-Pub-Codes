using UnityEngine;
using System;

/// <summary>
/// Karakter ve NPC'ler için depth sorting component'i
/// </summary>
public class CharacterDepthSorting : MonoBehaviour, IDepthSortable
{
    [Header("Character Sorting Settings")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Vector2 sortingOffset = Vector2.zero; // Ayar için offset
    [SerializeField] private bool useCustomSortingPoint = false;
    [SerializeField] private Transform customSortingPoint; // Özel sorting noktası

    [Header("Layer Override Settings")]
    [SerializeField] private bool canGoUnderObjects = true; // Objelerin altına geçebilir mi
    [SerializeField] private bool canGoOverObjects = true; // Objelerin üstüne çıkabilir mi
    [SerializeField] private int minSortingOrder = -100; // Minimum sorting order
    [SerializeField] private int maxSortingOrder = 100; // Maksimum sorting order

    private int currentSortingOrder;
    private bool isRegistered = false;

    private void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        RegisterToManager();
    }

    private void RegisterToManager()
    {
        if (DepthSortingManager.Instance != null && !isRegistered)
        {
            DepthSortingManager.Instance.RegisterSortableObject(this);
            isRegistered = true;
        }
    }

    private void OnEnable()
    {
        RegisterToManager();
    }

    private void OnDisable()
    {
        if (DepthSortingManager.Instance != null && isRegistered)
        {
            DepthSortingManager.Instance.UnregisterSortableObject(this);
            isRegistered = false;
        }
    }

    public Vector2 GetSortingPosition()
    {
        Vector2 position;

        if (useCustomSortingPoint && customSortingPoint != null)
        {
            position = customSortingPoint.position;
        }
        else
        {
            position = transform.position;
        }

        return position + sortingOffset;
    }

    public void SetSortingOrder(int order)
    {
        // Minimum ve maksimum değerleri kontrol et
        order = Mathf.Clamp(order, minSortingOrder, maxSortingOrder);

        currentSortingOrder = order;

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = order;
        }
    }

    public bool IsActive()
    {
        return gameObject.activeInHierarchy;
    }

    /// <summary>
    /// Sorting order'ı manuel olarak ayarla (geçici override için)
    /// </summary>
    public void SetTemporarySortingOrder(int order, float duration)
    {
        StartCoroutine(TemporarySortingOrderCoroutine(order, duration));
    }

    private System.Collections.IEnumerator TemporarySortingOrderCoroutine(int tempOrder, float duration)
    {
        int originalOrder = currentSortingOrder;
        SetSortingOrder(tempOrder);

        yield return new WaitForSeconds(duration);

        SetSortingOrder(originalOrder);
    }
}
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Ana depth sorting yöneticisi - oyundaki tüm depth sorting işlemlerini koordine eder
/// </summary>
public class DepthSortingManager : MonoBehaviour
{
    [Header("Sorting Settings")]
    [SerializeField] private float updateInterval = 0.1f; // Ne sıklıkla güncelleneceği
    [SerializeField] private int baseSortingOrder = 0; // Temel sorting order
    [SerializeField] private int sortingRange = 1000; // Kullanılabilir sorting order aralığı

    private List<IDepthSortable> sortableObjects = new List<IDepthSortable>();
    private float lastUpdateTime;

    public static DepthSortingManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Depth sorting yapılacak objeyi sisteme kaydet
    /// </summary>
    public void RegisterSortableObject(IDepthSortable sortable)
    {
        if (!sortableObjects.Contains(sortable))
        {
            sortableObjects.Add(sortable);
        }
    }

    /// <summary>
    /// Objeyi sistemden çıkar
    /// </summary>
    public void UnregisterSortableObject(IDepthSortable sortable)
    {
        sortableObjects.Remove(sortable);
    }

    private void Update()
    {
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateDepthSorting();
            lastUpdateTime = Time.time;
        }
    }

    /// <summary>
    /// Tüm kayıtlı objelerin depth sorting işlemini gerçekleştir
    /// </summary>
    private void UpdateDepthSorting()
    {
        // Y pozisyonuna göre sırala (büyükten küçüğe)
        sortableObjects.Sort((a, b) => b.GetSortingPosition().y.CompareTo(a.GetSortingPosition().y));

        // Sorting order atama
        for (int i = 0; i < sortableObjects.Count; i++)
        {
            int sortingOrder = baseSortingOrder + (sortingRange - i);
            sortableObjects[i].SetSortingOrder(sortingOrder);
        }
    }

    /// <summary>
    /// Belirli bir pozisyon için sorting order hesapla
    /// </summary>
    public int CalculateSortingOrder(Vector2 position)
    {
        return baseSortingOrder + Mathf.RoundToInt(-position.y * 100);
    }
}

/// <summary>
/// Depth sorting yapılabilir objeler için interface
/// </summary>
public interface IDepthSortable
{
    Vector2 GetSortingPosition();
    void SetSortingOrder(int order);
    bool IsActive();
}
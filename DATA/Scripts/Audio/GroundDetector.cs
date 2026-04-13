using UnityEngine;
using System.Collections.Generic;

public class GroundDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private Vector2 detectionBoxSize = new Vector2(0.8f, 0.2f);
    [SerializeField] private Vector2 detectionOffset = new Vector2(0f, -0.5f);
    [SerializeField] private LayerMask groundLayers = -1;

    [Header("Fallback Ground Type")]
    [SerializeField] private GroundType defaultGroundType;

    [Header("Performance")]
    [SerializeField] private float detectionInterval = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool enableDebugLogs = false;

    // Private variables
    private GroundType currentGroundType;
    private float lastDetectionTime = 0f;
    private Vector2 worldDetectionPoint;

    // Caching for performance
    private ContactFilter2D contactFilter;
    private Collider2D[] colliderBuffer = new Collider2D[5]; // Daha küçük buffer

    void Start()
    {
        InitializeGroundDetector();
    }

    private void InitializeGroundDetector()
    {
        SetupContactFilter();

        // İlk zemin tipini algıla
        currentGroundType = DetectGroundType();

        if (enableDebugLogs)
        {
            Debug.Log($"GroundDetector initialized. Current ground: {(currentGroundType?.name ?? "Default")}");
        }
    }

    private void SetupContactFilter()
    {
        contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(groundLayers);
        contactFilter.useLayerMask = true;
        contactFilter.useTriggers = false; // Sadece solid collider'ları algıla
    }

    void Update()
    {
        UpdateGroundDetection();
    }

    private void UpdateGroundDetection()
    {
        // Performance optimization: Belirli aralıklarla kontrol et
        if (Time.time - lastDetectionTime < detectionInterval)
            return;

        GroundType detectedGroundType = DetectGroundType();

        if (detectedGroundType != currentGroundType)
        {
            GroundType previousGround = currentGroundType;
            currentGroundType = detectedGroundType;

            if (enableDebugLogs)
            {
                Debug.Log($"Ground changed from {(previousGround?.name ?? "None")} to {(currentGroundType?.name ?? "None")}");
            }
        }

        lastDetectionTime = Time.time;
    }

    private GroundType DetectGroundType()
    {
        Vector2 detectionCenter = (Vector2)transform.position + detectionOffset;
        worldDetectionPoint = detectionCenter;

        // OverlapBox kullanarak çevredeki collider'ları bul
        int hitCount = Physics2D.OverlapBox(detectionCenter, detectionBoxSize, 0f, contactFilter, colliderBuffer);

        if (hitCount > 0)
        {
            // En yakın collider'ı bul ve component kontrolü yap
            for (int i = 0; i < hitCount; i++)
            {
                if (colliderBuffer[i] != null)
                {
                    GroundType groundType = GetGroundTypeFromCollider(colliderBuffer[i]);
                    if (groundType != null)
                    {
                        if (enableDebugLogs)
                            Debug.Log($"Found ground type: {groundType.name} on object: {colliderBuffer[i].name}");
                        return groundType;
                    }
                }
            }
        }

        // Hiçbir özel zemin tipi bulunamadıysa default'u döndür
        if (enableDebugLogs && currentGroundType != defaultGroundType)
        {
            Debug.Log("Using default ground type");
        }

        return defaultGroundType;
    }

    private GroundType GetGroundTypeFromCollider(Collider2D collider)
    {
        if (collider == null) return null;

        // Öncelikle GroundTypeComponent kontrol et
        GroundTypeComponent groundComponent = collider.GetComponent<GroundTypeComponent>();
        if (groundComponent != null && groundComponent.GroundType != null)
        {
            if (enableDebugLogs)
                Debug.Log($"Found GroundTypeComponent on {collider.name}: {groundComponent.GroundType.name}");
            return groundComponent.GroundType;
        }

        // Parent'larda da kontrol et (bazen collider child'da olabilir)
        groundComponent = collider.GetComponentInParent<GroundTypeComponent>();
        if (groundComponent != null && groundComponent.GroundType != null)
        {
            if (enableDebugLogs)
                Debug.Log($"Found GroundTypeComponent in parent of {collider.name}: {groundComponent.GroundType.name}");
            return groundComponent.GroundType;
        }

        return null;
    }

    // Public methods
    public GroundType GetCurrentGroundType()
    {
        return currentGroundType ?? defaultGroundType;
    }

    public bool IsOnGround()
    {
        return currentGroundType != null;
    }

    public bool IsOnSpecificGroundType(GroundType groundType)
    {
        return currentGroundType == groundType;
    }

    public void ForceDetection()
    {
        currentGroundType = DetectGroundType();
        if (enableDebugLogs)
            Debug.Log($"Forced detection result: {(currentGroundType?.name ?? "Default")}");
    }

    public void SetDetectionInterval(float interval)
    {
        detectionInterval = Mathf.Max(0.01f, interval);
    }

    // Debug methods
    public void DrawDebugGizmos()
    {
        if (!showDebugGizmos) return;

        Vector2 center = (Vector2)transform.position + detectionOffset;

        // Algılama kutusu
        if (currentGroundType != null)
        {
            Gizmos.color = currentGroundType.DebugColor;
        }
        else
        {
            Gizmos.color = Color.red;
        }

        Gizmos.DrawWireCube(center, detectionBoxSize);

        // Merkez noktası
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, 0.05f);
    }

    private void OnDrawGizmosSelected()
    {
        DrawDebugGizmos();

        // Debug info text
#if UNITY_EDITOR
        if (currentGroundType != null)
        {
            Vector3 labelPos = transform.position + Vector3.up * 1f;
            UnityEditor.Handles.Label(labelPos, $"Ground: {currentGroundType.name}");
        }
#endif
    }

    // Validation
    private void OnValidate()
    {
        detectionBoxSize = new Vector2(
            Mathf.Max(0.1f, detectionBoxSize.x),
            Mathf.Max(0.1f, detectionBoxSize.y)
        );

        detectionInterval = Mathf.Max(0.01f, detectionInterval);
    }
}
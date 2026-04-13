using UnityEngine;

/// <summary>
/// GameObject'lere özel zemin tipi atamak için kullanılan component
/// Layer-based sistemin alternatifi veya ek desteği olarak kullanılabilir
/// </summary>
public class GroundTypeComponent : MonoBehaviour
{
    [Header("Ground Type Assignment")]
    [SerializeField] private GroundType groundType;
    [SerializeField] private bool overrideLayerMapping = false;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    public GroundType GroundType => groundType;
    public bool OverrideLayerMapping => overrideLayerMapping;

    private void Start()
    {
        if (showDebugInfo && groundType != null)
        {
            Debug.Log($"GroundTypeComponent on '{gameObject.name}' set to: {groundType.name}");
        }
    }

    /// <summary>
    /// Runtime'da zemin tipini değiştir
    /// </summary>
    public void SetGroundType(GroundType newGroundType)
    {
        GroundType previousType = groundType;
        groundType = newGroundType;

        if (showDebugInfo)
        {
            Debug.Log($"GroundTypeComponent on '{gameObject.name}' changed from {(previousType?.name ?? "None")} to {(newGroundType?.name ?? "None")}");
        }
    }

    /// <summary>
    /// Bu component'in geçerli bir zemin tipi olup olmadığını kontrol eder
    /// </summary>
    public bool HasValidGroundType()
    {
        return groundType != null;
    }

    private void OnValidate()
    {
        if (groundType == null && showDebugInfo)
        {
            Debug.LogWarning($"GroundTypeComponent on '{gameObject.name}' has no ground type assigned!");
        }
    }

    // Debug için Gizmo çizimi
    private void OnDrawGizmosSelected()
    {
        if (groundType != null)
        {
            Gizmos.color = groundType.DebugColor;
            Gizmos.DrawWireCube(transform.position, Vector3.one);

            // Component adını göster
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"Ground: {groundType.name}");
#endif
        }
    }
}
// InteractableHighlight.cs - Modüler vurgulama sistemi
using UnityEngine;

[System.Serializable]
public class HighlightState
{
    [Header("State Info")]
    public string stateName;
    [Space]
    [Header("Sprites")]
    public Sprite normalSprite;
    public Sprite highlightedSprite;

    public HighlightState(string name, Sprite normal, Sprite highlighted)
    {
        stateName = name;
        normalSprite = normal;
        highlightedSprite = highlighted;
    }
}

public class InteractableHighlight : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Highlight States")]
    [SerializeField] private HighlightState[] highlightStates;

    [Header("Settings")]
    [SerializeField] private string defaultStateName = "default";

    private string currentStateName;
    private bool isHighlighted = false;

    private void Awake()
    {
        // SpriteRenderer otomatik bul
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Varsayılan state'i ayarla
        SetState(defaultStateName);
    }

    /// <summary>
    /// Mevcut durumu değiştirir (örn: "default", "cooking", "burning" vs.)
    /// </summary>
    public void SetState(string stateName)
    {
        currentStateName = stateName;
        UpdateSprite();
    }

    /// <summary>
    /// Vurgulama modunu açar/kapatır
    /// </summary>
    public void SetHighlighted(bool highlighted)
    {
        if (isHighlighted != highlighted)
        {
            isHighlighted = highlighted;
            UpdateSprite();
        }
    }

    /// <summary>
    /// Sprite'ı mevcut duruma göre günceller
    /// </summary>
    private void UpdateSprite()
    {
        HighlightState currentState = GetCurrentState();
        if (currentState != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = isHighlighted ?
                currentState.highlightedSprite :
                currentState.normalSprite;
        }
    }

    /// <summary>
    /// Mevcut durumu döndürür
    /// </summary>
    private HighlightState GetCurrentState()
    {
        foreach (var state in highlightStates)
        {
            if (state.stateName == currentStateName)
                return state;
        }

        // Eğer istenen state bulunamazsa, ilk state'i döndür
        if (highlightStates.Length > 0)
            return highlightStates[0];

        return null;
    }

    /// <summary>
    /// Yeni bir highlight state ekler
    /// </summary>
    public void AddHighlightState(string stateName, Sprite normalSprite, Sprite highlightedSprite)
    {
        // Önce mevcut state'i kontrol et
        for (int i = 0; i < highlightStates.Length; i++)
        {
            if (highlightStates[i].stateName == stateName)
            {
                // Güncelle
                highlightStates[i].normalSprite = normalSprite;
                highlightStates[i].highlightedSprite = highlightedSprite;
                UpdateSprite();
                return;
            }
        }

        // Yeni state ekle
        System.Array.Resize(ref highlightStates, highlightStates.Length + 1);
        highlightStates[highlightStates.Length - 1] = new HighlightState(stateName, normalSprite, highlightedSprite);

        UpdateSprite();
    }

    /// <summary>
    /// Mevcut durumun adını döndürür
    /// </summary>
    public string GetCurrentStateName()
    {
        return currentStateName;
    }

    /// <summary>
    /// Vurgulanma durumunu döndürür
    /// </summary>
    public bool IsHighlighted()
    {
        return isHighlighted;
    }

#if UNITY_EDITOR
    // Inspector'da kolay test için
    [Header("Editor Testing")]
    [SerializeField] private bool testHighlight = false;
    [SerializeField] private string testStateName = "default";

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            SetHighlighted(testHighlight);
            if (!string.IsNullOrEmpty(testStateName))
                SetState(testStateName);
        }
    }
#endif
}
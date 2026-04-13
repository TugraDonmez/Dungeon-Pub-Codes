using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableHighlightsec : MonoBehaviour
{
    [Header("Highlight Settings")]
    [SerializeField] private GameObject highlightEffect;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float highlightIntensity = 1.2f;

    private Color originalColor;
    private bool isHighlighted = false;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    public void SetHighlighted(bool highlighted)
    {
        if (isHighlighted == highlighted) return;

        isHighlighted = highlighted;

        if (highlightEffect != null)
        {
            highlightEffect.SetActive(highlighted);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = highlighted ?
                highlightColor * highlightIntensity :
                originalColor;
        }
    }
}

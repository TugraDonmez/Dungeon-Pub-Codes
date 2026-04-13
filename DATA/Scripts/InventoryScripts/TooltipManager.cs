using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    [Header("Tooltip Components")]
    public GameObject dragBox;
    public TMP_Text tooltipText;
    public Canvas canvas;

    [Header("Settings")]
    public Vector2 offset = new Vector2(10, 10); // Mouse'dan ne kadar uzakta olacak

    private RectTransform dragBoxRect;
    private bool isTooltipActive = false;
    private string currentTooltipText = "";

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Component referanslarını al
        if (dragBox != null)
            dragBoxRect = dragBox.GetComponent<RectTransform>();

        // Başlangıçta tooltip'i gizle
        HideTooltip();
    }

    public void ShowTooltip(string text)
    {
        if (string.IsNullOrEmpty(text) || dragBox == null || tooltipText == null)
            return;

        currentTooltipText = text;
        tooltipText.text = text;
        dragBox.SetActive(true);
        isTooltipActive = true;

        // Pozisyonu güncelle
        UpdateTooltipPosition();
    }

    public void HideTooltip()
    {
        if (dragBox != null)
            dragBox.SetActive(false);

        isTooltipActive = false;
        currentTooltipText = "";
    }

    public void UpdateTooltipPosition()
    {
        if (!isTooltipActive || dragBoxRect == null || canvas == null)
            return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            Input.mousePosition,
            canvas.worldCamera,
            out localPoint
        );

        // Offset ekle
        localPoint += offset;

        // Ekran sınırları içinde kalmasını sağla
        Vector2 canvasSize = (canvas.transform as RectTransform).sizeDelta;
        Vector2 tooltipSize = dragBoxRect.sizeDelta;

        // Sağ kenardan taşarsa sol tarafa al
        if (localPoint.x + tooltipSize.x > canvasSize.x / 2)
            localPoint.x -= (tooltipSize.x + offset.x * 2);

        // Üst kenardan taşarsa alt tarafa al
        if (localPoint.y + tooltipSize.y > canvasSize.y / 2)
            localPoint.y -= (tooltipSize.y + offset.y * 2);

        dragBoxRect.localPosition = localPoint;
    }

    private void Update()
    {
        // Tooltip aktifse pozisyonu sürekli güncelle
        if (isTooltipActive)
        {
            UpdateTooltipPosition();
        }
    }

    public bool IsTooltipActive()
    {
        return isTooltipActive;
    }

    public string GetCurrentTooltipText()
    {
        return currentTooltipText;
    }
}
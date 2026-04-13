using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingDragHandler : MonoBehaviour
{
    public static CraftingDragHandler Instance;

    [Header("Drag UI Components")]
    public CraftingSlotUI draggedSlot;
    public RectTransform dragIcon;
    public Canvas canvas;
    public TMP_Text dragAmountText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);
        else
            Instance = this;

        dragIcon.gameObject.SetActive(false);

        // Eğer dragAmountText yoksa oluştur
        if (dragAmountText == null)
        {
            var textObj = new GameObject("CraftingDragAmountText");
            textObj.transform.SetParent(dragIcon);
            textObj.transform.localPosition = new Vector3(15, -15, 0);
            textObj.transform.localScale = Vector3.one;

            dragAmountText = textObj.AddComponent<TMP_Text>();
            dragAmountText.text = "";
            dragAmountText.fontSize = 12;
            dragAmountText.color = Color.white;
            dragAmountText.alignment = TextAlignmentOptions.Center;
        }
    }

    public bool IsDragging()
    {
        return draggedSlot != null;
    }

    public void StartDrag(CraftingSlotUI slot)
    {
        draggedSlot = slot;
        SetupDragIcon(slot);
        dragIcon.gameObject.SetActive(true);
    }

    public void Drag()
    {
        if (draggedSlot == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            Input.mousePosition,
            canvas.worldCamera,
            out localPoint
        );

        dragIcon.localPosition = localPoint;
    }

    public void EndDrag()
    {
        dragIcon.gameObject.SetActive(false);
        draggedSlot = null;
    }

    public void SetupDragIcon(CraftingSlotUI slot)
    {
        var slotData = slot.craftingSlot;

        Image iconImage = dragIcon.GetComponent<Image>();
        iconImage.sprite = slotData.item.icon;

        // Miktar metnini güncelle
        if (slotData.item.stackable && slotData.amount > 1)
        {
            dragAmountText.text = slotData.amount.ToString();
        }
        else
        {
            dragAmountText.text = "";
        }
    }

    // YENİ METOD: Drag icon'u güncelle
    public void UpdateDragIcon()
    {
        if (draggedSlot == null) return;
        SetupDragIcon(draggedSlot);
    }
}
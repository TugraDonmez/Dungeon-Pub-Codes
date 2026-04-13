using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class InventoryDragHandler : MonoBehaviour
{
    public static InventoryDragHandler Instance;

    [Header("Drag UI Components")]
    public SlotUI draggedSlot;
    public RectTransform dragIcon;
    public Canvas canvas;
    public TMP_Text dragAmountText;

    // Sağ tık drag için özel veriler
    private InventorySlot rightClickDragData;
    private bool isRightClickDrag = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);
        else
            Instance = this;

        dragIcon.gameObject.SetActive(false);

        if (dragAmountText == null)
        {
            var textObj = new GameObject("DragAmountText");
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

    #region Public Methods
    public bool IsDragging()
    {
        return draggedSlot != null;
    }

    public bool IsRightClickDrag()
    {
        return isRightClickDrag;
    }

    public InventorySlot GetRightClickDragData()
    {
        return rightClickDragData;
    }

    public void StartDrag(SlotUI slot, bool isRightClick = false)
    {
        draggedSlot = slot;
        isRightClickDrag = isRightClick;
        rightClickDragData = null;

        SetupDragIcon(slot);
        dragIcon.gameObject.SetActive(true);
        slot.ShowTooltipIfNeeded();
    }

    public void StartRightClickDrag(SlotUI slot, InventorySlot tempData)
    {
        draggedSlot = slot;
        isRightClickDrag = true;
        rightClickDragData = tempData;

        SetupDragIconForRightClick(tempData);
        dragIcon.gameObject.SetActive(true);
        slot.ShowTooltipIfNeeded();

        slot.UpdateUI();
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
        draggedSlot.ShowTooltipIfNeeded();
    }

    public void EndDrag()
    {
        dragIcon.gameObject.SetActive(false);
        draggedSlot = null;
        rightClickDragData = null;
        isRightClickDrag = false;
    }

    public void CancelRightClickDrag()
    {
        if (isRightClickDrag && rightClickDragData != null && !rightClickDragData.IsEmpty && draggedSlot != null)
        {
            var originalSlot = draggedSlot.inventory.slots[draggedSlot.slotIndex];
            originalSlot.amount += rightClickDragData.amount;
            draggedSlot.UpdateUI();
        }
    }

    public void UpdateDragIcon()
    {
        if (draggedSlot == null) return;

        if (isRightClickDrag && rightClickDragData != null)
        {
            SetupDragIconForRightClick(rightClickDragData);
        }
        else
        {
            SetupDragIcon(draggedSlot);
        }
    }

    // YENİ: Crafting slotlarından drop'u handle et
    public bool HandleCraftingSlotDrop(CraftingSlotUI craftingSlot)
    {
        if (draggedSlot == null) return false;

        var inventoryData = draggedSlot.inventory.slots[draggedSlot.slotIndex];

        Debug.Log($"Inventory'den crafting'e drop: {inventoryData.item?.itemName}");

        // Bu işlemi CraftingSlotUI'da handle ediyoruz, burada sadece true döndür
        return true;
    }
    #endregion

    #region Private Methods
    private void SetupDragIcon(SlotUI slot)
    {
        var slotData = slot.inventory.slots[slot.slotIndex];

        Image iconImage = dragIcon.GetComponent<Image>();
        iconImage.sprite = slotData.item.icon;

        if (slotData.item.stackable && slotData.amount > 1)
        {
            dragAmountText.text = slotData.amount.ToString();
        }
        else
        {
            dragAmountText.text = "";
        }
    }

    private void SetupDragIconForRightClick(InventorySlot tempData)
    {
        Image iconImage = dragIcon.GetComponent<Image>();
        iconImage.sprite = tempData.item.icon;

        if (tempData.item.stackable && tempData.amount > 1)
        {
            dragAmountText.text = tempData.amount.ToString();
        }
        else
        {
            dragAmountText.text = "";
        }
    }
    #endregion
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class HandSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI Components")]
    public HandSlotUIComponent[] handSlotComponents = new HandSlotUIComponent[4];
    public Color activeSlotColor = Color.yellow;
    public Color inactiveSlotColor = Color.white;

    [System.Serializable]
    public class HandSlotUIComponent
    {
        public Image slotBackground;
        public Image icon;
        public TMP_Text amountText;
        public TMP_Text keyText; // 1, 2, 3, 4 göstergesi
        public int slotIndex;
    }

    public int draggedSlotIndex = -1;
    public bool isDragging = false;
    private bool wasDroppedSuccessfully = false;

    private void Start()
    {
        // Key textlerini ayarla
        for (int i = 0; i < handSlotComponents.Length; i++)
        {
            if (handSlotComponents[i].keyText != null)
            {
                handSlotComponents[i].keyText.text = (i + 1).ToString();
            }
            handSlotComponents[i].slotIndex = i;

            // BUG FIX 1: Başlangıçta icon'ları gizle
            if (handSlotComponents[i].icon != null)
            {
                handSlotComponents[i].icon.enabled = false;
            }
        }

        UpdateActiveSlotIndicator(0);
    }

    public void UpdateHandSlot(int slotIndex, HandSlot handSlot)
    {
        if (slotIndex >= 0 && slotIndex < handSlotComponents.Length)
        {
            var component = handSlotComponents[slotIndex];

            if (handSlot.IsEmpty)
            {
                // BUG FIX 1: Icon'u tamamen gizle
                component.icon.enabled = false;
                component.amountText.text = "";
            }
            else
            {
                // BUG FIX 1: Icon'u göster ve sprite'ı ata
                component.icon.enabled = true;
                component.icon.sprite = handSlot.item.icon;
                component.amountText.text = handSlot.item.stackable && handSlot.amount > 1 ? handSlot.amount.ToString() : "";
            }
        }
    }

    public void UpdateActiveSlotIndicator(int activeSlotIndex)
    {
        for (int i = 0; i < handSlotComponents.Length; i++)
        {
            if (handSlotComponents[i].slotBackground != null)
            {
                handSlotComponents[i].slotBackground.color = i == activeSlotIndex ? activeSlotColor : inactiveSlotColor;
            }
        }
    }

    // IPointerClickHandler
    public void OnPointerClick(PointerEventData eventData)
    {
        int clickedSlotIndex = GetSlotIndexFromPointer(eventData);
        if (clickedSlotIndex == -1) return;

        // Sol tık ile slot aktif et
        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 1)
        {
            HandSlotManager.Instance.SwitchToHandSlot(clickedSlotIndex);
        }
        // Çift tık ile inventory'e gönder
        else if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 2)
        {
            SendToInventory(clickedSlotIndex);
        }
    }

    // IBeginDragHandler
    public void OnBeginDrag(PointerEventData eventData)
    {
        draggedSlotIndex = GetSlotIndexFromPointer(eventData);
        if (draggedSlotIndex == -1) return;

        var handSlot = HandSlotManager.Instance.GetHandSlot(draggedSlotIndex);
        if (handSlot.IsEmpty) return;

        isDragging = true;
        wasDroppedSuccessfully = false;

        // BUG FIX 3: Hand slot drag'i başlat ve görselleştir
        StartHandSlotDragWithVisual(draggedSlotIndex);

        // Raycast'i engelle
        var canvasGroup = handSlotComponents[draggedSlotIndex].slotBackground.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = handSlotComponents[draggedSlotIndex].slotBackground.gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.blocksRaycasts = false;
    }

    // IDragHandler
    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging && InventoryDragHandler.Instance != null)
        {
            InventoryDragHandler.Instance.Drag();
        }
    }

    // IEndDragHandler
    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggedSlotIndex == -1) return;

        isDragging = false;

        // Raycast'i tekrar aktif et
        var canvasGroup = handSlotComponents[draggedSlotIndex].slotBackground.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        // Eğer başarıyla drop edilmediyse işlemi iptal et
        if (!wasDroppedSuccessfully)
        {
            Debug.Log("Hand slot drag iptal edildi");
        }

        if (InventoryDragHandler.Instance != null)
        {
            InventoryDragHandler.Instance.EndDrag();
        }

        draggedSlotIndex = -1;
    }

    // IDropHandler
    public void OnDrop(PointerEventData eventData)
    {
        int dropSlotIndex = GetSlotIndexFromPointer(eventData);
        if (dropSlotIndex == -1) return;

        wasDroppedSuccessfully = true;

        // Inventory'den hand slot'a drop
        var inventoryDragSlot = InventoryDragHandler.Instance?.draggedSlot;
        if (inventoryDragSlot != null)
        {
            HandleInventoryToHandSlotDrop(inventoryDragSlot, dropSlotIndex);
            return;
        }

        // Hand slot'tan hand slot'a drop
        if (draggedSlotIndex != -1 && draggedSlotIndex != dropSlotIndex)
        {
            HandleHandSlotToHandSlotDrop(draggedSlotIndex, dropSlotIndex);
        }

        // Crafting'den hand slot'a drop
        var craftingDragSlot = CraftingDragHandler.Instance?.draggedSlot;
        if (craftingDragSlot != null)
        {
            HandleCraftingToHandSlotDrop(craftingDragSlot, dropSlotIndex);
        }
    }

    // IPointerEnterHandler
    public void OnPointerEnter(PointerEventData eventData)
    {
        int slotIndex = GetSlotIndexFromPointer(eventData);
        if (slotIndex == -1) return;

        var handSlot = HandSlotManager.Instance.GetHandSlot(slotIndex);
        if (!handSlot.IsEmpty && TooltipManager.Instance != null)
        {
            TooltipManager.Instance.ShowTooltip(handSlot.item.itemName);
        }
    }

    // IPointerExitHandler
    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
        }
    }

    private int GetSlotIndexFromPointer(PointerEventData eventData)
    {
        for (int i = 0; i < handSlotComponents.Length; i++)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(
                handSlotComponents[i].slotBackground.rectTransform,
                eventData.position,
                eventData.pressEventCamera))
            {
                return i;
            }
        }
        return -1;
    }

    // BUG FIX 3: Hand slot drag görselleştirmesi
    private void StartHandSlotDragWithVisual(int slotIndex)
    {
        var handSlot = HandSlotManager.Instance.GetHandSlot(slotIndex);
        if (handSlot.IsEmpty) return;

        if (InventoryDragHandler.Instance != null)
        {
            // Drag icon'u ayarla
            var dragIcon = InventoryDragHandler.Instance.dragIcon;
            var dragIconImage = dragIcon.GetComponent<Image>();
            var dragAmountText = InventoryDragHandler.Instance.dragAmountText;

            if (dragIconImage != null)
            {
                dragIconImage.sprite = handSlot.item.icon;
                dragIconImage.enabled = true;
            }

            if (dragAmountText != null)
            {
                dragAmountText.text = handSlot.item.stackable && handSlot.amount > 1 ? handSlot.amount.ToString() : "";
            }

            dragIcon.gameObject.SetActive(true);

            Debug.Log($"Hand slot {slotIndex + 1} drag başladı: {handSlot.item.itemName} x{handSlot.amount}");
        }
    }

    private void HandleInventoryToHandSlotDrop(SlotUI inventorySlot, int handSlotIndex)
    {
        var inventoryData = inventorySlot.inventory.slots[inventorySlot.slotIndex];
        var handSlot = HandSlotManager.Instance.GetHandSlot(handSlotIndex);

        if (inventoryData.IsEmpty) return;

        if (handSlot.IsEmpty)
        {
            // Boş hand slot'a koy
            handSlot.SetItem(inventoryData.item, inventoryData.amount);
            inventoryData.Clear();
        }
        else if (handSlot.item == inventoryData.item && inventoryData.item.stackable)
        {
            // Stackle
            int spaceLeft = 99 - handSlot.amount;
            int amountToAdd = Mathf.Min(spaceLeft, inventoryData.amount);

            handSlot.amount += amountToAdd;
            inventoryData.amount -= amountToAdd;

            if (inventoryData.amount <= 0)
            {
                inventoryData.Clear();
            }
        }
        else
        {
            // Swap
            var tempItem = handSlot.item;
            var tempAmount = handSlot.amount;

            handSlot.SetItem(inventoryData.item, inventoryData.amount);
            inventoryData.item = tempItem;
            inventoryData.amount = tempAmount;
        }

        // UI güncelle
        HandSlotManager.Instance.UpdateHandSlotUI(handSlotIndex);
        inventorySlot.UpdateUI();

        Debug.Log($"Inventory'den hand slot {handSlotIndex + 1}'e taşındı");
    }

    private void HandleHandSlotToHandSlotDrop(int fromSlotIndex, int toSlotIndex)
    {
        HandSlotManager.Instance.SwapHandSlots(fromSlotIndex, toSlotIndex);
        Debug.Log($"Hand slot {fromSlotIndex + 1} ile {toSlotIndex + 1} yer değiştirdi");
    }

    private void HandleCraftingToHandSlotDrop(CraftingSlotUI craftingSlot, int handSlotIndex)
    {
        var craftingData = craftingSlot.craftingSlot;
        var handSlot = HandSlotManager.Instance.GetHandSlot(handSlotIndex);

        if (craftingData.IsEmpty) return;

        // Crafting manager'dan output'u temizle
        if (craftingSlot.craftingManager != null)
        {
            craftingSlot.craftingManager.ClearOutput();
        }

        if (handSlot.IsEmpty)
        {
            // Boş hand slot'a koy
            handSlot.SetItem(craftingData.item, craftingData.amount);
            craftingData.Clear();
        }
        else if (handSlot.item == craftingData.item && craftingData.item.stackable)
        {
            // Stackle
            int spaceLeft = 99 - handSlot.amount;
            int amountToAdd = Mathf.Min(spaceLeft, craftingData.amount);

            handSlot.amount += amountToAdd;
            craftingData.amount -= amountToAdd;

            if (craftingData.amount <= 0)
            {
                craftingData.Clear();
            }
        }
        else
        {
            // Swap
            var tempItem = handSlot.item;
            var tempAmount = handSlot.amount;

            handSlot.SetItem(craftingData.item, craftingData.amount);
            craftingData.item = tempItem;
            craftingData.amount = tempAmount;
        }

        // UI güncelle
        HandSlotManager.Instance.UpdateHandSlotUI(handSlotIndex);
        craftingSlot.UpdateUI();

        // Recipe kontrolü
        if (craftingSlot.craftingManager != null)
        {
            craftingSlot.craftingManager.OnSlotChanged();
        }

        Debug.Log($"Crafting'den hand slot {handSlotIndex + 1}'e taşındı");
    }

    private void SendToInventory(int handSlotIndex)
    {
        var handSlot = HandSlotManager.Instance.GetHandSlot(handSlotIndex);
        if (handSlot.IsEmpty) return;

        // PlayerInventoryManager üzerinden inventory'e ekle
        var playerInvManager = FindObjectOfType<PlayerInventoryManager>();
        if (playerInvManager != null)
        {
            bool success = playerInvManager.TryAddItem(handSlot.item, handSlot.amount);
            if (success)
            {
                HandSlotManager.Instance.ClearHandSlot(handSlotIndex);
                Debug.Log($"Hand slot {handSlotIndex + 1}'den inventory'e gönderildi");
            }
        }
    }
}
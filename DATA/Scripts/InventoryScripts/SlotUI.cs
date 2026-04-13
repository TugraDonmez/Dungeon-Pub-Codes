using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class SlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public int slotIndex;
    public Inventory inventory;
    public InventoryUI inventoryUI;

    public Image icon;
    public TMP_Text amountText;

    private bool isDragging = false;
    private bool isRightClickDrag = false;
    private bool wasDroppedSuccessfully = false; // YENİ: Drop başarısını takip et

    public void Setup(int index, Inventory inv, InventoryUI ui)
    {
        slotIndex = index;
        inventory = inv;
        inventoryUI = ui;
        UpdateUI();
    }

    public void UpdateUI()
    {
        var slot = inventory.slots[slotIndex];
        if (slot.IsEmpty)
        {
            icon.enabled = false;
            amountText.text = "";
        }
        else
        {
            icon.enabled = true;
            icon.sprite = slot.item.icon;
            amountText.text = slot.item.stackable ? slot.amount.ToString() : "";
        }
    }

    #region Click Operations
    public void OnPointerClick(PointerEventData eventData)
    {
        // Crafting drag sırasında sağ tık ile 1 tane alma
        if (eventData.button == PointerEventData.InputButton.Right &&
            CraftingDragHandler.Instance.IsDragging())
        {
            HandleRightClickWhileCraftingDragging();
            return;
        }

        // Eğer inventory drag işlemi devam ediyorsa click'i işleme
        if (InventoryDragHandler.Instance.IsDragging())
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                HandleRightClickWhileDragging();
            }
            return;
        }

        // Normal durumdaki click işlemleri
        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 2)
        {
            HandleDoubleLeftClick();
        }
    }

    private void HandleRightClickWhileCraftingDragging()
    {
        var craftingSlot = CraftingDragHandler.Instance.draggedSlot;
        if (craftingSlot == null || craftingSlot.craftingSlot.IsEmpty) return;

        var craftingData = craftingSlot.craftingSlot;
        var inventoryData = inventory.slots[slotIndex];

        Debug.Log($"Crafting'den sağ tık ile 1 tane alınıyor: {craftingData.item.itemName}");

        // Temizle output
        if (craftingSlot.craftingManager != null)
        {
            craftingSlot.craftingManager.ClearOutput();
        }

        if (inventoryData.IsEmpty)
        {
            // Boş slota 1 tane koy
            inventoryData.AddItem(craftingData.item, 1);
            Debug.Log("Inventory boş slotuna 1 tane konuldu");
        }
        else if (inventoryData.item == craftingData.item && craftingData.item.stackable && inventoryData.amount < 99)
        {
            // Aynı eşyaysa 1 tane ekle
            inventoryData.amount++;
            Debug.Log($"Inventory slotuna 1 tane eklendi, yeni miktar: {inventoryData.amount}");
        }
        else
        {
            Debug.Log("Inventory slotu dolu veya farklı eşya var!");
            return;
        }

        // Crafting slotundan 1 tane azalt
        craftingData.amount--;
        if (craftingData.amount <= 0)
        {
            craftingData.Clear();
            CraftingDragHandler.Instance.EndDrag();
            Debug.Log("Crafting slot boşaldı, drag işlemi sonlandırıldı");
        }

        // UI'ları güncelle
        UpdateUI();
        craftingSlot.UpdateUI();
        CraftingDragHandler.Instance.UpdateDragIcon();

        // Recipe kontrolü
        if (craftingSlot.craftingManager != null)
        {
            craftingSlot.craftingManager.OnSlotChanged();
        }
    }

    private void HandleDoubleLeftClick()
    {
        var slot = inventory.slots[slotIndex];

        // Slot boş mu veya stacklenebilir değil mi kontrol et
        if (slot.IsEmpty || !slot.item.stackable) return;

        // Zaten max stack mi?
        int maxStack = 99;
        if (slot.amount >= maxStack) return;

        // Ne kadar yer var?
        int spaceLeft = maxStack - slot.amount;

        // Aynı eşyaları topla
        int collectedAmount = CollectSameItemsToSlot(slot.item, spaceLeft);

        // Eğer bir şey topladıysak UI'ı güncelle
        if (collectedAmount > 0)
        {
            slot.amount += collectedAmount;
            UpdateUI();
            inventoryUI.DrawInventory();

            // Debug için (opsiyonel)
            Debug.Log($"{collectedAmount} adet {slot.item.itemName} toplandı. Yeni toplam: {slot.amount}");
        }
    }

    private int CollectSameItemsToSlot(Item targetItem, int spaceLeft)
    {
        int totalCollected = 0;

        // Envanterdeki tüm slotları kontrol et
        for (int i = 0; i < inventory.slots.Count && spaceLeft > 0; i++)
        {
            // Kendi slot'unu atla
            if (i == slotIndex) continue;

            var otherSlot = inventory.slots[i];

            // Aynı eşya mı kontrol et
            if (!otherSlot.IsEmpty && otherSlot.item == targetItem)
            {
                // Ne kadar alabiliriz?
                int amountToTake = Mathf.Min(otherSlot.amount, spaceLeft);

                // Al
                otherSlot.amount -= amountToTake;
                totalCollected += amountToTake;
                spaceLeft -= amountToTake;

                // Eğer slot boşaldıysa temizle
                if (otherSlot.amount <= 0)
                {
                    otherSlot.Clear();
                }
            }
        }

        return totalCollected;
    }

    private void HandleRightClickWhileDragging()
    {
        var draggedSlot = InventoryDragHandler.Instance.draggedSlot;
        if (draggedSlot == null) return;

        var draggedData = draggedSlot.inventory.slots[draggedSlot.slotIndex];
        var targetData = inventory.slots[slotIndex];

        // Sadece boş slot veya aynı eşya varsa işlem yap
        if (targetData.IsEmpty || (targetData.item == draggedData.item && draggedData.item.stackable))
        {
            // Taşınan eşyadan 1 tane al
            if (draggedData.amount > 0)
            {
                if (targetData.IsEmpty)
                {
                    // Boş slota 1 tane koy
                    targetData.AddItem(draggedData.item, 1);
                }
                else if (targetData.item == draggedData.item && targetData.amount < 99)
                {
                    // Aynı eşya varsa 1 tane ekle
                    targetData.amount = Mathf.Min(99, targetData.amount + 1);
                }
                else
                {
                    return; // Stack full, işlem yapma
                }

                // Taşınan eşyayı azalt
                draggedData.amount--;
                if (draggedData.amount <= 0)
                {
                    draggedData.Clear();
                    InventoryDragHandler.Instance.EndDrag();
                }

                // UI'ları güncelle
                UpdateUI();
                draggedSlot.UpdateUI();
                InventoryDragHandler.Instance.UpdateDragIcon();
            }
        }
    }
    #endregion

    #region Drag Operations
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (inventory.slots[slotIndex].IsEmpty)
            return;

        // Drop başarı durumunu sıfırla
        wasDroppedSuccessfully = false;

        // Hangi buton ile drag başladığını kontrol et
        isRightClickDrag = eventData.button == PointerEventData.InputButton.Right;

        if (isRightClickDrag)
        {
            // Sağ tık ile drag: Yarı alma işlemi
            HandleRightClickDragStart();
        }
        else
        {
            // Sol tık ile drag: Normal drag
            HandleLeftClickDragStart();
        }

        isDragging = true;
        GetComponent<CanvasGroup>().blocksRaycasts = false;
        ShowTooltipIfNeeded();
    }

    private void HandleLeftClickDragStart()
    {
        // Normal sol tık drag - tüm stack'i al
        InventoryDragHandler.Instance.StartDrag(this, false);
    }

    private void HandleRightClickDragStart()
    {
        var slot = inventory.slots[slotIndex];

        // Eğer sadece 1 tane varsa tümünü al
        if (slot.amount == 1)
        {
            InventoryDragHandler.Instance.StartDrag(this, false);
            return;
        }

        // Yarısını al (yukarı yuvarlama)
        int halfAmount = Mathf.CeilToInt(slot.amount / 2f);

        // Yeni slot oluştur (taşınacak kısım için)
        var tempSlot = new InventorySlot(slot.item, halfAmount);

        // Orijinal slottan çıkar
        slot.amount -= halfAmount;

        // UI'ı hemen güncelle
        UpdateUI();

        // Drag handler'a özel bir taşıma başlat
        InventoryDragHandler.Instance.StartRightClickDrag(this, tempSlot);
    }

    public void OnDrag(PointerEventData eventData)
    {
        InventoryDragHandler.Instance.Drag();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        isRightClickDrag = false;

        // DÜZELTME: Eğer başarıyla drop edilmediyse sağ tık drag'i iptal et
        if (InventoryDragHandler.Instance.IsRightClickDrag() && !wasDroppedSuccessfully)
        {
            Debug.Log("Sağ tık drag başarısız, geri alınıyor");
            InventoryDragHandler.Instance.CancelRightClickDrag();
        }

        InventoryDragHandler.Instance.EndDrag();
        GetComponent<CanvasGroup>().blocksRaycasts = true;

        if (!IsMouseOverSlot())
        {
            HideTooltip();
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        // Drop başarılı olarak işaretle
        wasDroppedSuccessfully = true;

        // Normal inventory drag işlemi
        var fromSlotUI = InventoryDragHandler.Instance.draggedSlot;
        if (fromSlotUI != null && fromSlotUI != this)
        {
            // Sağ tık drag mı kontrol et
            if (InventoryDragHandler.Instance.IsRightClickDrag())
            {
                Debug.Log("Sağ tık drop işlemi başlıyor");
                HandleRightClickDrop();
            }
            else
            {
                Debug.Log("Normal drop işlemi başlıyor");
                HandleNormalDrop();
            }
            InventoryDragHandler.Instance.EndDrag();
            return;
        }

        // Crafting drag işlemi - CraftingDragHandler'dan gelen drop
        var craftingFromSlot = CraftingDragHandler.Instance.draggedSlot;
        if (craftingFromSlot != null)
        {
            HandleCraftingSlotDrop(craftingFromSlot);
            return;
        }

        // YENİ: Hand Slot'tan inventory'e drop işlemi
        if (HandSlotManager.Instance != null)
        {
            var handSlotUI = FindObjectOfType<HandSlotUI>();
            if (handSlotUI != null && handSlotUI.GetComponent<HandSlotUI>().isDragging)
            {
                var handSlotUIComponent = handSlotUI.GetComponent<HandSlotUI>();
                int draggedIndex = handSlotUIComponent.draggedSlotIndex; // Bu field'i public yapmanız gerekebilir
                HandleHandSlotToInventoryDrop(draggedIndex);
                return;
            }
        }

        InventoryDragHandler.Instance.EndDrag();
    }


    private void HandleHandSlotToInventoryDrop(int handSlotIndex)
    {
        if (handSlotIndex == -1) return;

        var handSlot = HandSlotManager.Instance.GetHandSlot(handSlotIndex);
        var inventorySlot = inventory.slots[slotIndex];

        if (handSlot.IsEmpty) return;

        if (inventorySlot.IsEmpty)
        {
            // Boş inventory slotuna koy
            inventorySlot.AddItem(handSlot.item, handSlot.amount);
            HandSlotManager.Instance.ClearHandSlot(handSlotIndex);

            Debug.Log($"Hand slot {handSlotIndex + 1}'den inventory slot {slotIndex}'e taşındı");
        }
        else if (inventorySlot.item == handSlot.item && handSlot.item.stackable)
        {
            // Stackle
            int spaceLeft = 99 - inventorySlot.amount;
            int amountToAdd = Mathf.Min(spaceLeft, handSlot.amount);

            inventorySlot.amount += amountToAdd;
            handSlot.amount -= amountToAdd;

            if (handSlot.amount <= 0)
            {
                HandSlotManager.Instance.ClearHandSlot(handSlotIndex);
            }
            else
            {
                HandSlotManager.Instance.UpdateHandSlotUI(handSlotIndex);
            }

            Debug.Log($"Hand slot {handSlotIndex + 1}'den inventory'e stacklendi");
        }
        else
        {
            // Swap işlemi
            var tempItem = handSlot.item;
            var tempAmount = handSlot.amount;

            handSlot.SetItem(inventorySlot.item, inventorySlot.amount);
            inventorySlot.item = tempItem;
            inventorySlot.amount = tempAmount;

            HandSlotManager.Instance.UpdateHandSlotUI(handSlotIndex);

            Debug.Log($"Hand slot {handSlotIndex + 1} ile inventory slot {slotIndex} swap yapıldı");
        }

        UpdateUI();
    }


    private void HandleCraftingSlotDrop(CraftingSlotUI craftingSlot)
    {
        if (craftingSlot == null || craftingSlot.craftingSlot.IsEmpty)
        {
            CraftingDragHandler.Instance.EndDrag();
            return;
        }

        var craftingData = craftingSlot.craftingSlot;
        var inventoryData = inventory.slots[slotIndex];

        Debug.Log($"Crafting'den inventory'e drop: {craftingData.item.itemName} x{craftingData.amount}");

        // Crafting manager'dan output'u temizle
        if (craftingSlot.craftingManager != null)
        {
            craftingSlot.craftingManager.ClearOutput();
        }

        if (inventoryData.IsEmpty)
        {
            // Boş slota koy
            inventoryData.AddItem(craftingData.item, craftingData.amount);
            craftingData.Clear();

            Debug.Log("Crafting slotundan inventory'e taşındı - boş slot");
        }
        else if (inventoryData.item == craftingData.item && craftingData.item.stackable)
        {
            // Stackle
            int spaceLeft = 99 - inventoryData.amount;
            int amountToAdd = Mathf.Min(spaceLeft, craftingData.amount);

            inventoryData.amount += amountToAdd;
            craftingData.amount -= amountToAdd;

            if (craftingData.amount <= 0)
            {
                craftingData.Clear();
            }

            Debug.Log($"Crafting slotundan inventory'e stacklendi - {amountToAdd} eklendi");
        }
        else
        {
            // Swap işlemi
            (craftingData.item, inventoryData.item) = (inventoryData.item, craftingData.item);
            (craftingData.amount, inventoryData.amount) = (inventoryData.amount, craftingData.amount);

            Debug.Log("Crafting slotundan inventory'e swap yapıldı");
        }

        // UI'ları güncelle
        UpdateUI();
        craftingSlot.UpdateUI();

        // Recipe kontrolü yap
        if (craftingSlot.craftingManager != null)
        {
            craftingSlot.craftingManager.OnSlotChanged();
        }

        CraftingDragHandler.Instance.EndDrag();
    }

    private void HandleNormalDrop()
    {
        var fromSlotUI = InventoryDragHandler.Instance.draggedSlot;
        var fromInventory = fromSlotUI.inventory;
        var toInventory = this.inventory;

        var fromData = fromInventory.slots[fromSlotUI.slotIndex];
        var toData = toInventory.slots[slotIndex];

        Debug.Log($"Normal drop: {fromData.item?.itemName} x{fromData.amount} -> Slot {slotIndex}");

        if (!fromData.IsEmpty)
        {
            if (!toData.IsEmpty && fromData.item == toData.item && fromData.item.stackable)
            {
                // Stackleme
                int total = fromData.amount + toData.amount;
                toData.amount = Mathf.Min(99, total);
                fromData.amount = total - toData.amount;

                if (fromData.amount == 0)
                    fromData.Clear();

                Debug.Log($"Stackleme yapıldı: Toplam {toData.amount}, kalan {fromData.amount}");
            }
            else
            {
                // Swap işlemi
                (fromData.item, toData.item) = (toData.item, fromData.item);
                (fromData.amount, toData.amount) = (toData.amount, fromData.amount);

                Debug.Log("Swap işlemi yapıldı");
            }

            // Her iki envanter UI'ını güncelle
            fromSlotUI.inventoryUI.DrawInventory();
            inventoryUI.DrawInventory();
        }
    }

    private void HandleRightClickDrop()
    {
        var tempData = InventoryDragHandler.Instance.GetRightClickDragData();
        var targetData = inventory.slots[slotIndex];

        Debug.Log($"Sağ tık drop: {tempData?.item?.itemName} x{tempData?.amount} -> Slot {slotIndex}");

        if (tempData != null && !tempData.IsEmpty)
        {
            if (targetData.IsEmpty)
            {
                // Boş slota koy - başarılı transfer
                targetData.AddItem(tempData.item, tempData.amount);
                Debug.Log($"Boş slota konuldu: {tempData.item.itemName} x{tempData.amount}");
                // tempData otomatik olarak temizlenecek, geri koymaya gerek yok
            }
            else if (targetData.item == tempData.item && targetData.item.stackable)
            {
                // Stackle
                int spaceLeft = 99 - targetData.amount;
                int amountToAdd = Mathf.Min(spaceLeft, tempData.amount);
                targetData.amount += amountToAdd;

                Debug.Log($"Stacklendi: {amountToAdd} eklendi, toplam {targetData.amount}");

                // Kalan miktarı hesapla
                int remaining = tempData.amount - amountToAdd;
                if (remaining > 0)
                {
                    // Tümü konulamadı, kalanı geri koy
                    var originalSlot = InventoryDragHandler.Instance.draggedSlot.inventory.slots[InventoryDragHandler.Instance.draggedSlot.slotIndex];
                    originalSlot.amount += remaining;
                    Debug.Log($"Kalan {remaining} geri konuldu");
                }
            }
            else
            {
                // Farklı eşya veya dolu slot, geri koy
                var originalSlot = InventoryDragHandler.Instance.draggedSlot.inventory.slots[InventoryDragHandler.Instance.draggedSlot.slotIndex];
                originalSlot.amount += tempData.amount;
                Debug.Log($"Uyumsuz hedef, {tempData.amount} geri konuldu");
            }

            // UI'ları güncelle
            UpdateUI();
            InventoryDragHandler.Instance.draggedSlot.UpdateUI();
        }
    }
    #endregion

    #region Tooltip Operations
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isDragging)
        {
            ShowTooltipIfNeeded();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isDragging)
        {
            HideTooltip();
        }
    }

    public void ShowTooltipIfNeeded()
    {
        var slot = inventory.slots[slotIndex];
        if (!slot.IsEmpty && TooltipManager.Instance != null)
        {
            TooltipManager.Instance.ShowTooltip(slot.item.itemName);
        }
    }

    private void HideTooltip()
    {
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
        }
    }

    private bool IsMouseOverSlot()
    {
        Vector2 localMousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform as RectTransform,
            Input.mousePosition,
            null,
            out localMousePosition
        );

        return (transform as RectTransform).rect.Contains(localMousePosition);
    }
    #endregion
}
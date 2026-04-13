using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CraftingSlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler,
    IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI Components")]
    public Image icon;
    public Image backgroundImage;
    public TMP_Text amountText;
    public Button takeButton;

    [Header("Slot Configuration")]
    [SerializeField] private CraftingSlotType slotType = CraftingSlotType.Ingredient;
    [SerializeField] private int gridX = -1;
    [SerializeField] private int gridY = -1;

    public CraftingSlot craftingSlot;
    public CraftingManager craftingManager;
    private bool isSetup = false;
    private bool isDragging = false;

    private void Awake()
    {
        if (craftingSlot == null)
        {
            craftingSlot = new CraftingSlot();
        }
    }

    private void Start()
    {
        if (!isSetup)
        {
            AutoSetup();
        }
        UpdateUI();
    }

    private void AutoSetup()
    {
        if (craftingManager == null)
        {
            craftingManager = GetComponentInParent<CraftingManager>();
            if (craftingManager == null)
            {
                craftingManager = FindObjectOfType<CraftingManager>();
            }
        }

        if (craftingManager != null)
        {
            if (gridX >= 0 && gridY >= 0 && gridX < 3 && gridY < 3)
            {
                if (craftingManager.craftingGrid[gridX, gridY] != null)
                {
                    Setup(craftingManager.craftingGrid[gridX, gridY], CraftingSlotType.Ingredient, craftingManager, gridX, gridY);
                }
            }
            else if (slotType == CraftingSlotType.Output)
            {
                Setup(craftingManager.outputSlot, CraftingSlotType.Output, craftingManager, -1, -1);
            }
        }
    }

    public void Setup(CraftingSlot slot, CraftingSlotType type, CraftingManager manager, int x, int y)
    {
        craftingSlot = slot ?? new CraftingSlot();
        slotType = type;
        craftingManager = manager;
        gridX = x;
        gridY = y;
        isSetup = true;

        if (takeButton != null)
        {
            takeButton.gameObject.SetActive(type == CraftingSlotType.Output);
            takeButton.onClick.RemoveAllListeners();
            takeButton.onClick.AddListener(TakeOutputItem);
        }

        UpdateUI();
        Debug.Log($"CraftingSlotUI setup completed for slot [{x},{y}], type: {type}");
    }

    public void UpdateUI()
    {
        if (craftingSlot == null)
        {
            Debug.LogWarning("CraftingSlot is null in UpdateUI!");
            return;
        }

        if (craftingSlot.IsEmpty)
        {
            if (icon != null) icon.enabled = false;
            if (amountText != null) amountText.text = "";

            if (backgroundImage != null)
            {
                backgroundImage.enabled = slotType == CraftingSlotType.Ingredient;
            }
        }
        else
        {
            if (icon != null)
            {
                icon.enabled = true;
                icon.sprite = craftingSlot.item.icon;
            }

            if (backgroundImage != null)
            {
                backgroundImage.enabled = false;
            }

            if (amountText != null)
            {
                amountText.text = craftingSlot.item.stackable && craftingSlot.amount > 1 ?
                    craftingSlot.amount.ToString() : "";
            }
        }

        if (takeButton != null && slotType == CraftingSlotType.Output)
        {
            takeButton.gameObject.SetActive(!craftingSlot.IsEmpty);
        }
    }

    #region Drag Operations
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Sadece ingredient slotlarında drag'e izin ver
        if (slotType != CraftingSlotType.Ingredient || craftingSlot.IsEmpty)
            return;

        isDragging = true;
        GetComponent<CanvasGroup>().blocksRaycasts = false;
        CraftingDragHandler.Instance.StartDrag(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            CraftingDragHandler.Instance.Drag();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            isDragging = false;
            GetComponent<CanvasGroup>().blocksRaycasts = true;

            // Envantere drop edilip edilmediğini kontrol et
            if (!WasDroppedOnValidTarget(eventData))
            {
                // Hiçbir yere drop edilmediyse envantere geri gönder
                Debug.Log("Crafting slotundan envantere geri gönderiliyor");
                ReturnItemToInventory();
            }

            CraftingDragHandler.Instance.EndDrag();
        }
    }

    private bool WasDroppedOnValidTarget(PointerEventData eventData)
    {
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            // SlotUI (inventory) veya CraftingSlotUI (crafting) üzerine drop edildi mi?
            if (result.gameObject.GetComponent<SlotUI>() != null ||
                result.gameObject.GetComponent<CraftingSlotUI>() != null)
            {
                return true;
            }
        }
        return false;
    }

    #endregion

    public void OnDrop(PointerEventData eventData)
    {
        // Inventory'den gelen drop
        var inventoryDraggedSlot = InventoryDragHandler.Instance.draggedSlot;
        if (inventoryDraggedSlot != null)
        {
            HandleInventoryDrop(eventData);
            return;
        }

        // Crafting'den gelen drop
        var craftingDraggedSlot = CraftingDragHandler.Instance.draggedSlot;
        if (craftingDraggedSlot != null)
        {
            HandleCraftingDrop();
        }
    }

    private void HandleInventoryDrop(PointerEventData eventData)
    {
        var draggedSlot = InventoryDragHandler.Instance.draggedSlot;
        if (draggedSlot == null) return;

        var draggedData = draggedSlot.inventory.slots[draggedSlot.slotIndex];

        if (slotType == CraftingSlotType.Output)
        {
            Debug.Log("Çıktı slotuna item bırakılamaz!");
            return;
        }

        if (draggedData.IsEmpty)
        {
            Debug.Log("Taşınan slot boş!");
            return;
        }

        if (!CraftingSystem.Instance.CanPlaceInCraftingSlot(draggedData.item.id))
        {
            Debug.Log($"Bu eşya ({draggedData.item.itemName}) crafting tablosuna konulamaz!");
            return;
        }

        if (InventoryDragHandler.Instance.IsRightClickDrag())
        {
            HandleRightClickInventoryDrop(draggedSlot);
        }
        else
        {
            HandleNormalInventoryDrop(draggedSlot);
            InventoryDragHandler.Instance.EndDrag();
        }
    }

    private void HandleRightClickInventoryDrop(SlotUI draggedSlot)
    {
        var rightClickData = InventoryDragHandler.Instance.GetRightClickDragData();
        if (rightClickData == null || rightClickData.IsEmpty)
        {
            Debug.Log("Sağ tık drag verisi boş!");
            return;
        }

        if (!CraftingSystem.Instance.CanPlaceInCraftingSlot(rightClickData.item.id))
        {
            Debug.Log($"Bu eşya ({rightClickData.item.itemName}) crafting tablosuna konulamaz!");
            // Geri koy
            var originalSlot = draggedSlot.inventory.slots[draggedSlot.slotIndex];
            originalSlot.amount += rightClickData.amount;
            draggedSlot.UpdateUI();
            return;
        }

        // Output'u temizle
        if (craftingManager != null)
        {
            craftingManager.ClearOutput();
        }

        // DÜZELTME: Sağ tık drag verilerini kullan, ana slot verilerini değil
        if (craftingSlot.IsEmpty)
        {
            // Boş slota koy - SADECE rightClickData miktarını kullan
            craftingSlot.SetItem(rightClickData.item, rightClickData.amount);
            Debug.Log($"Sağ tık ile crafting slotuna konuldu: {rightClickData.item.itemName} x{rightClickData.amount}");

            // BAŞARILI TRANSFER: Envanter slotundan çıkarma işlemi yapma
            // (Çünkü rightClickData zaten orijinal slottan çıkarılmış miktar)
        }
        else if (craftingSlot.item == rightClickData.item && rightClickData.item.stackable)
        {
            // Stackle
            int spaceLeft = 99 - craftingSlot.amount;
            int amountToAdd = Mathf.Min(spaceLeft, rightClickData.amount);

            craftingSlot.amount += amountToAdd;

            // SADECE kalan varsa geri koy
            int remaining = rightClickData.amount - amountToAdd;
            if (remaining > 0)
            {
                var originalSlot = draggedSlot.inventory.slots[draggedSlot.slotIndex];
                originalSlot.amount += remaining;
            }
            // Başarılı olan kısmı geri koymuyoruz çünkü crafting slotuna gitti

            Debug.Log($"Sağ tık ile crafting slotuna stacklendi: {amountToAdd} eklendi, {remaining} geri konuldu");
        }
        else
        {
            // Swap işlemi
            var tempItem = craftingSlot.item;
            var tempAmount = craftingSlot.amount;

            craftingSlot.SetItem(rightClickData.item, rightClickData.amount);

            // Eski eşyayı envantere geri ver - SWAP durumunda rightClickData'yı geri koyuyoruz
            var originalSlot = draggedSlot.inventory.slots[draggedSlot.slotIndex];
            originalSlot.amount += rightClickData.amount; // Önce rightClickData'yı geri koy
            originalSlot.AddItem(tempItem, tempAmount); // Sonra swap edilen eşyayı ekle

            Debug.Log("Sağ tık ile crafting slotunda swap yapıldı");
        }

        UpdateUI();
        draggedSlot.UpdateUI();

        if (craftingManager != null)
        {
            craftingManager.OnSlotChanged();
        }

        // DÜZELTME: Drag işlemini sonlandır, böylece InventoryDragHandler.EndDrag çağrılmaz
        // ve rightClickData geri konmaz
        InventoryDragHandler.Instance.EndDrag();
    }

    private void HandleNormalInventoryDrop(SlotUI draggedSlot)
    {
        var draggedData = draggedSlot.inventory.slots[draggedSlot.slotIndex];

        if (draggedData.IsEmpty)
        {
            Debug.Log("Taşınan slot boş!");
            return;
        }

        if (!CraftingSystem.Instance.CanPlaceInCraftingSlot(draggedData.item.id))
        {
            Debug.Log($"Bu eşya ({draggedData.item.itemName}) crafting tablosuna konulamaz!");
            return;
        }

        // Output'u temizle
        if (craftingManager != null)
        {
            craftingManager.ClearOutput();
        }

        if (!craftingSlot.IsEmpty)
        {
            if (craftingSlot.item == draggedData.item && draggedData.item.stackable)
            {
                int spaceLeft = 99 - craftingSlot.amount;
                int amountToAdd = Mathf.Min(spaceLeft, draggedData.amount);

                if (amountToAdd > 0)
                {
                    craftingSlot.amount += amountToAdd;
                    draggedData.amount -= amountToAdd;

                    if (draggedData.amount <= 0)
                        draggedData.Clear();
                }
            }
            else
            {
                Debug.Log("Bu slot zaten dolu ve farklı bir eşya var!");
                return;
            }
        }
        else
        {
            int amountToMove;

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                amountToMove = 1;
            }
            else
            {
                amountToMove = draggedData.amount;
            }

            craftingSlot.SetItem(draggedData.item, amountToMove);
            draggedData.amount -= amountToMove;
            if (draggedData.amount <= 0)
            {
                draggedData.Clear();
            }
        }

        UpdateUI();
        draggedSlot.UpdateUI();

        if (craftingManager != null)
        {
            craftingManager.OnSlotChanged();
        }
    }

    private void HandleCraftingDrop()
    {
        var fromSlotUI = CraftingDragHandler.Instance.draggedSlot;
        if (fromSlotUI == null || fromSlotUI == this)
        {
            CraftingDragHandler.Instance.EndDrag();
            return;
        }

        if (slotType != CraftingSlotType.Ingredient)
        {
            CraftingDragHandler.Instance.EndDrag();
            return;
        }

        // BUG FIX: Output'u temizle
        if (craftingManager != null)
        {
            craftingManager.ClearOutput();
        }

        var fromData = fromSlotUI.craftingSlot;
        var toData = this.craftingSlot;

        if (!fromData.IsEmpty)
        {
            if (!toData.IsEmpty && fromData.item == toData.item && fromData.item.stackable)
            {
                int total = fromData.amount + toData.amount;
                toData.amount = Mathf.Min(99, total);
                fromData.amount = total - toData.amount;

                if (fromData.amount == 0)
                    fromData.Clear();
            }
            else
            {
                (fromData.item, toData.item) = (toData.item, fromData.item);
                (fromData.amount, toData.amount) = (toData.amount, fromData.amount);
            }

            fromSlotUI.UpdateUI();
            UpdateUI();

            if (craftingManager != null)
            {
                craftingManager.OnSlotChanged();
            }
        }

        CraftingDragHandler.Instance.EndDrag();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Sağ tık ile inventory drag sırasında 1 tane bırakma
        if (eventData.button == PointerEventData.InputButton.Right &&
            InventoryDragHandler.Instance.IsDragging())
        {
            HandleRightClickWhileDragging();
            return;
        }

        // Sol tık ile çıktı slotlarından alma
        if (eventData.button == PointerEventData.InputButton.Left &&
            slotType == CraftingSlotType.Output && !craftingSlot.IsEmpty)
        {
            TakeOutputItem();
        }
        // Sağ tık ile normal slotlardan geri alma
        else if (eventData.button == PointerEventData.InputButton.Right &&
                 slotType != CraftingSlotType.Output && !craftingSlot.IsEmpty)
        {
            ReturnItemToInventory();
        }
    }

    private void HandleRightClickWhileDragging()
    {
        if (slotType != CraftingSlotType.Ingredient)
        {
            Debug.Log("Output slotuna sağ tık ile item bırakılamaz!");
            return;
        }

        var draggedSlot = InventoryDragHandler.Instance.draggedSlot;
        if (draggedSlot == null) return;

        var draggedData = draggedSlot.inventory.slots[draggedSlot.slotIndex];

        // Inventory'den sağ tık drag ise özel veriler kullan
        if (InventoryDragHandler.Instance.IsRightClickDrag())
        {
            var rightClickData = InventoryDragHandler.Instance.GetRightClickDragData();
            if (rightClickData == null || rightClickData.IsEmpty)
            {
                Debug.Log("Sağ tık drag verisi boş!");
                return;
            }

            // Crafting'e koyulabilir mi kontrol et
            if (!CraftingSystem.Instance.CanPlaceInCraftingSlot(rightClickData.item.id))
            {
                Debug.Log($"Bu eşya ({rightClickData.item.itemName}) crafting tablosuna konulamaz!");
                return;
            }

            // Output'u temizle
            if (craftingManager != null)
            {
                craftingManager.ClearOutput();
            }

            // 1 tane bırak
            PlaceOneItem(rightClickData.item, rightClickData, draggedSlot);
        }
        else
        {
            // Normal drag'den 1 tane bırak
            if (draggedData.IsEmpty) return;

            if (!CraftingSystem.Instance.CanPlaceInCraftingSlot(draggedData.item.id))
            {
                Debug.Log($"Bu eşya ({draggedData.item.itemName}) crafting tablosuna konulamaz!");
                return;
            }

            // Output'u temizle
            if (craftingManager != null)
            {
                craftingManager.ClearOutput();
            }

            // 1 tane bırak
            PlaceOneItem(draggedData.item, draggedData, draggedSlot);
        }
    }

    private void PlaceOneItem(Item item, InventorySlot sourceData, SlotUI sourceSlotUI)
    {
        if (craftingSlot.IsEmpty)
        {
            // Boş slota 1 tane koy
            craftingSlot.SetItem(item, 1);
            Debug.Log($"Crafting slotuna 1 tane konuldu: {item.itemName}");
        }
        else if (craftingSlot.item == item && item.stackable && craftingSlot.amount < 99)
        {
            // Aynı eşyaysa 1 tane ekle
            craftingSlot.amount++;
            Debug.Log($"Crafting slotuna 1 tane eklendi: {item.itemName}, Yeni miktar: {craftingSlot.amount}");
        }
        else
        {
            Debug.Log("Crafting slotu dolu veya farklı eşya var!");
            return;
        }

        // Kaynağı azalt
        sourceData.amount--;
        if (sourceData.amount <= 0)
        {
            sourceData.Clear();
            InventoryDragHandler.Instance.EndDrag();
            Debug.Log("Kaynak slot boşaldı, drag işlemi sonlandırıldı");
        }

        // UI'ları güncelle
        UpdateUI();
        sourceSlotUI.UpdateUI();
        InventoryDragHandler.Instance.UpdateDragIcon();

        // Recipe kontrolü
        if (craftingManager != null)
        {
            craftingManager.OnSlotChanged();
        }
    }

    private void TakeOutputItem()
    {
        if (craftingSlot == null || craftingSlot.IsEmpty)
        {
            Debug.Log("Output slot boş!");
            return;
        }

        var playerInventory = FindObjectOfType<PlayerInventoryManager>();
        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventoryManager bulunamadı!");
            return;
        }

        // Önce item'ı geçici olarak sakla
        Item itemToTake = craftingSlot.item;
        int amountToTake = craftingSlot.amount;

        Debug.Log($"Output alınmaya çalışılıyor: {itemToTake.itemName} x{amountToTake}");

        // Envantere eklenebilir mi kontrol et
        bool success = playerInventory.TryAddItem(itemToTake, amountToTake);

        if (success)
        {
            Debug.Log($"Output başarıyla alındı: {itemToTake.itemName} x{amountToTake}");

            // ÖNCE malzemeleri tüket (CraftItem çağır)
            if (craftingManager != null)
            {
                Debug.Log("Malzemeler tüketiliyor...");
                craftingManager.CraftItem();
            }

            // SONRA output'u temizle
            craftingSlot.Clear();
            UpdateUI();

            Debug.Log("Output item alma işlemi tamamlandı");
        }
        else
        {
            Debug.Log("Envanter dolu, eşya alınamadı!");
        }
    }

    private void ReturnItemToInventory()
    {
        if (craftingSlot == null || craftingSlot.IsEmpty) return;

        var playerInventory = FindObjectOfType<PlayerInventoryManager>();
        if (playerInventory != null)
        {
            bool success = playerInventory.TryAddItem(craftingSlot.item, craftingSlot.amount);
            if (success)
            {
                craftingSlot.Clear();
                UpdateUI();
                if (craftingManager != null)
                {
                    // BUG FIX: Output'u da temizle
                    craftingManager.ClearOutput();
                    craftingManager.OnSlotChanged();
                }
            }
            else
            {
                Debug.Log("Envanter dolu, eşya geri konulamadı!");
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isDragging && craftingSlot != null && !craftingSlot.IsEmpty && TooltipManager.Instance != null)
        {
            TooltipManager.Instance.ShowTooltip(craftingSlot.item.itemName);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isDragging && TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
        }
    }
}
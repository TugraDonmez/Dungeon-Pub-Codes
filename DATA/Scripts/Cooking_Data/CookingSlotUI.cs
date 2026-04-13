using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CookingSlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI Components")]
    public Image icon;
    public TMP_Text amountText;
    public Button takeButton; // Çıktı slotları için alma butonu

    private CookingSlot cookingSlot;
    private CookingSlotType slotType;
    private OvenCookingManager cookingManager;

    public void Setup(CookingSlot slot, CookingSlotType type, OvenCookingManager manager)
    {
        cookingSlot = slot;
        slotType = type;
        cookingManager = manager;

        // Çıktı slotları için alma butonunu aktif et
        if (takeButton != null)
        {
            takeButton.gameObject.SetActive(type == CookingSlotType.Output);
            takeButton.onClick.RemoveAllListeners();
            takeButton.onClick.AddListener(TakeOutputItem);
        }

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (cookingSlot.IsEmpty)
        {
            icon.enabled = false;
            if (amountText != null) amountText.text = "";
        }
        else
        {
            icon.enabled = true;
            icon.sprite = cookingSlot.item.icon;
            if (amountText != null)
            {
                amountText.text = cookingSlot.item.stackable && cookingSlot.amount > 1 ?
                    cookingSlot.amount.ToString() : "";
            }
        }

        // Alma butonunu sadece dolu çıktı slotlarında göster
        if (takeButton != null && slotType == CookingSlotType.Output)
        {
            takeButton.gameObject.SetActive(!cookingSlot.IsEmpty);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        var draggedSlot = InventoryDragHandler.Instance.draggedSlot;
        if (draggedSlot == null) return;

        var draggedData = draggedSlot.inventory.slots[draggedSlot.slotIndex];

        // Çıktı slotlarına drop edilemez
        if (slotType == CookingSlotType.Output)
            return;

        // Boş slot mu kontrol et
        if (draggedData.IsEmpty)
            return;

        // Bu slot türüne uygun mu kontrol et
        if (!cookingSlot.CanAcceptItem(draggedData.item, slotType))
        {
            Debug.Log($"Bu eşya ({draggedData.item.itemName}) bu slota konulamaz!");
            return;
        }

        // Slot zaten dolu mu
        if (!cookingSlot.IsEmpty)
        {
            Debug.Log("Bu slot zaten dolu!");
            return;
        }

        // Eşyayı taşı
        cookingSlot.SetItem(draggedData.item, 1); // Her seferinde 1 tane al

        // Envanter slotundan çıkar
        draggedData.amount--;
        if (draggedData.amount <= 0)
        {
            draggedData.Clear();
        }

        // UI'ları güncelle
        UpdateUI();
        draggedSlot.UpdateUI();

        // Pişirme kontrolü yap
        cookingManager.OnSlotChanged(slotType);

        InventoryDragHandler.Instance.EndDrag();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Sol tık ile çıktı slotlarından alma
        if (eventData.button == PointerEventData.InputButton.Left &&
            slotType == CookingSlotType.Output && !cookingSlot.IsEmpty)
        {
            TakeOutputItem();
        }
        // Sağ tık ile normal slotlardan geri alma
        else if (eventData.button == PointerEventData.InputButton.Right &&
                 slotType != CookingSlotType.Output && !cookingSlot.IsEmpty)
        {
            ReturnItemToInventory();
        }
    }

    private void TakeOutputItem()
    {
        if (cookingSlot.IsEmpty) return;

        var playerInventory = FindObjectOfType<PlayerInventoryManager>();
        if (playerInventory != null)
        {
            bool success = playerInventory.TryAddItem(cookingSlot.item, cookingSlot.amount);
            if (success)
            {
                cookingSlot.Clear();
                UpdateUI();
            }
            else
            {
                Debug.Log("Envanter dolu, eşya alınamadı!");
            }
        }
    }

    private void ReturnItemToInventory()
    {
        if (cookingSlot.IsEmpty) return;

        var playerInventory = FindObjectOfType<PlayerInventoryManager>();
        if (playerInventory != null)
        {
            bool success = playerInventory.TryAddItem(cookingSlot.item, cookingSlot.amount);
            if (success)
            {
                cookingSlot.Clear();
                UpdateUI();

                // BU SATIR ÖNEMLİ: Malzeme geri alındığında pişirme işlemini iptal et
                cookingManager.OnIngredientRemoved(slotType);
            }
            else
            {
                Debug.Log("Envanter dolu, eşya geri konulamadı!");
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!cookingSlot.IsEmpty && TooltipManager.Instance != null)
        {
            TooltipManager.Instance.ShowTooltip(cookingSlot.item.itemName);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
        }
    }
}
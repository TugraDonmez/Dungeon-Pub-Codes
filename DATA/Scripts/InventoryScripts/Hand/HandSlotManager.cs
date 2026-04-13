using System.Collections.Generic;
using UnityEngine;

public class HandSlotManager : MonoBehaviour
{
    public static HandSlotManager Instance;

    [Header("Hand Slots")]
    public HandSlot[] handSlots = new HandSlot[4];
    public int activeHandSlotIndex = 0;

    [Header("Visual")]
    public Transform handVisualParent; // Karakterin elindeki eşyayı gösterecek transform
    public List<string> hiddenItemIDs = new List<string>(); // Elimde gösterilmeyecek eşya ID'leri

    [Header("UI References")]
    public HandSlotUI handSlotUI;

    private GameObject currentHandVisual;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Hand slotları başlat
        for (int i = 0; i < handSlots.Length; i++)
        {
            handSlots[i] = new HandSlot();
        }
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // 1, 2, 3, 4 tuşları ile el slotu değiştir
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwitchToHandSlot(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwitchToHandSlot(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SwitchToHandSlot(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SwitchToHandSlot(3);
        }
    }

    public void SwitchToHandSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < handSlots.Length)
        {
            activeHandSlotIndex = slotIndex;
            UpdateHandVisual();

            if (handSlotUI != null)
            {
                handSlotUI.UpdateActiveSlotIndicator(activeHandSlotIndex);
            }

            Debug.Log($"Aktif el slotu: {activeHandSlotIndex + 1}");
        }
    }

    public HandSlot GetActiveHandSlot()
    {
        return handSlots[activeHandSlotIndex];
    }

    public HandSlot GetHandSlot(int index)
    {
        if (index >= 0 && index < handSlots.Length)
        {
            return handSlots[index];
        }
        return null;
    }

    public bool TryAddItemToHandSlot(int slotIndex, Item item, int amount)
    {
        if (slotIndex >= 0 && slotIndex < handSlots.Length)
        {
            var handSlot = handSlots[slotIndex];

            if (handSlot.IsEmpty)
            {
                handSlot.AddItem(item, amount);
                UpdateHandSlotUI(slotIndex);
                if (slotIndex == activeHandSlotIndex)
                {
                    UpdateHandVisual();
                }
                return true;
            }
            else if (handSlot.item == item && item.stackable && handSlot.amount < 99)
            {
                int spaceLeft = 99 - handSlot.amount;
                int amountToAdd = Mathf.Min(spaceLeft, amount);
                handSlot.amount += amountToAdd;
                UpdateHandSlotUI(slotIndex);
                if (slotIndex == activeHandSlotIndex)
                {
                    UpdateHandVisual();
                }
                return amountToAdd == amount; // Tüm miktar eklenebildi mi?
            }
        }
        return false;
    }

    public void SwapHandSlots(int slot1Index, int slot2Index)
    {
        if (slot1Index >= 0 && slot1Index < handSlots.Length &&
            slot2Index >= 0 && slot2Index < handSlots.Length)
        {
            var temp = new HandSlot(handSlots[slot1Index].item, handSlots[slot1Index].amount);
            handSlots[slot1Index].SetItem(handSlots[slot2Index].item, handSlots[slot2Index].amount);
            handSlots[slot2Index].SetItem(temp.item, temp.amount);

            UpdateHandSlotUI(slot1Index);
            UpdateHandSlotUI(slot2Index);

            if (slot1Index == activeHandSlotIndex || slot2Index == activeHandSlotIndex)
            {
                UpdateHandVisual();
            }
        }
    }

    public void ClearHandSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < handSlots.Length)
        {
            handSlots[slotIndex].Clear();
            UpdateHandSlotUI(slotIndex);

            if (slotIndex == activeHandSlotIndex)
            {
                UpdateHandVisual();
            }
        }
    }

    public void UpdateAllHandSlotsUI()
    {
        if (handSlotUI != null)
        {
            for (int i = 0; i < handSlots.Length; i++)
            {
                handSlotUI.UpdateHandSlot(i, handSlots[i]);
            }
        }
    }

    public void UpdateHandSlotUI(int slotIndex)
    {
        if (handSlotUI != null && slotIndex >= 0 && slotIndex < handSlots.Length)
        {
            handSlotUI.UpdateHandSlot(slotIndex, handSlots[slotIndex]);
        }
    }

    public void UpdateHandVisual()
    {
        // Önceki görseli temizle
        if (currentHandVisual != null)
        {
            Destroy(currentHandVisual);
            currentHandVisual = null;
        }

        var activeSlot = GetActiveHandSlot();

        // Aktif slotta eşya varsa ve gösterilmesi gerekiyorsa
        if (!activeSlot.IsEmpty && ShouldShowItemInHand(activeSlot.item))
        {
            CreateHandVisual(activeSlot.item);
        }
    }

    private bool ShouldShowItemInHand(Item item)
    {
        return !hiddenItemIDs.Contains(item.id);
    }

    private void CreateHandVisual(Item item)
    {
        if (handVisualParent == null || item == null || item.icon == null)
            return;

        // Yeni görsel oluştur
        currentHandVisual = new GameObject($"HandVisual_{item.itemName}");
        currentHandVisual.transform.SetParent(handVisualParent);
        currentHandVisual.transform.localPosition = Vector3.zero;
        currentHandVisual.transform.localRotation = Quaternion.identity;
        currentHandVisual.transform.localScale = Vector3.one;

        // SpriteRenderer ekle
        var spriteRenderer = currentHandVisual.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = item.icon;
        spriteRenderer.sortingOrder = 1; // Karakterin önünde görünsün

        Debug.Log($"El görseli oluşturuldu: {item.itemName}");
    }

    public void AddItemToHiddenList(string itemID)
    {
        if (!hiddenItemIDs.Contains(itemID))
        {
            hiddenItemIDs.Add(itemID);
        }
    }

    public void RemoveItemFromHiddenList(string itemID)
    {
        hiddenItemIDs.Remove(itemID);
    }

    public bool IsItemHidden(string itemID)
    {
        return hiddenItemIDs.Contains(itemID);
    }

    // Save/Load işlemleri için
    [System.Serializable]
    public class HandSlotData
    {
        public string[] itemIDs = new string[4];
        public int[] amounts = new int[4];
        public int activeSlotIndex;
    }

    public HandSlotData GetSaveData()
    {
        var data = new HandSlotData();
        data.activeSlotIndex = activeHandSlotIndex;

        for (int i = 0; i < handSlots.Length; i++)
        {
            if (!handSlots[i].IsEmpty)
            {
                data.itemIDs[i] = handSlots[i].item.id;
                data.amounts[i] = handSlots[i].amount;
            }
            else
            {
                data.itemIDs[i] = "";
                data.amounts[i] = 0;
            }
        }

        return data;
    }

    public void LoadFromData(HandSlotData data, ItemDatabase itemDatabase)
    {
        activeHandSlotIndex = data.activeSlotIndex;

        for (int i = 0; i < handSlots.Length; i++)
        {
            if (!string.IsNullOrEmpty(data.itemIDs[i]))
            {
                var item = itemDatabase.GetItemByID(data.itemIDs[i]);
                if (item != null)
                {
                    handSlots[i].SetItem(item, data.amounts[i]);
                }
            }
            else
            {
                handSlots[i].Clear();
            }
        }

        UpdateAllHandSlotsUI();
        UpdateHandVisual();
    }

    public bool TryTakeOneItemFromActiveSlot(out Item item)
    {
        var activeSlot = GetActiveHandSlot();

        if (activeSlot.IsEmpty)
        {
            item = null;
            return false;
        }

        item = activeSlot.item;
        activeSlot.amount--;

        if (activeSlot.amount <= 0)
        {
            activeSlot.Clear();
        }

        UpdateHandSlotUI(activeHandSlotIndex);
        UpdateHandVisual();

        Debug.Log($"Aktif hand slottan 1 {item.itemName} alındı. Kalan: {activeSlot.amount}");
        return true;
    }

    public bool TryTakeItemFromActiveSlot(int amountToTake, out Item item, out int takenAmount)
    {
        var activeSlot = GetActiveHandSlot();

        if (activeSlot.IsEmpty)
        {
            item = null;
            takenAmount = 0;
            return false;
        }

        item = activeSlot.item;
        takenAmount = Mathf.Min(amountToTake, activeSlot.amount);
        activeSlot.amount -= takenAmount;

        if (activeSlot.amount <= 0)
        {
            activeSlot.Clear();
        }

        UpdateHandSlotUI(activeHandSlotIndex);
        UpdateHandVisual();

        Debug.Log($"Aktif hand slottan {takenAmount} {item.itemName} alındı. Kalan: {activeSlot.amount}");
        return true;
    }

    public bool IsActiveHandSlotEmpty()
    {
        return GetActiveHandSlot().IsEmpty;
    }

}
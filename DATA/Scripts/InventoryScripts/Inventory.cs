using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/InventoryData")]
public class Inventory : ScriptableObject
{
    public int slotCount = 20;
    public List<InventorySlot> slots = new();
    public string saveFileName = "player_inventory";

    public void Save()
    {
        InventoryData data = new();

        // Her slot için pozisyonu da kaydet
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (!slot.IsEmpty)
            {
                data.slots.Add(new ItemSlotData
                {
                    itemID = slot.item.id,
                    amount = slot.amount,
                    slotIndex = i  // Slot pozisyonunu kaydet
                });
            }
        }

        SaveSystem.SaveInventory(data, saveFileName);
        Debug.Log($"Envanter kaydedildi: {saveFileName} - {data.slots.Count} item");
    }

    public void Load(ItemDatabase db)
    {
        var data = SaveSystem.LoadInventory(saveFileName);

        // Önce tüm slotları temizle
        slots.Clear();

        // SlotCount kadar boş slot oluştur
        for (int i = 0; i < slotCount; i++)
        {
            slots.Add(new InventorySlot());
        }

        // Kayıtlı verileri DOĞRU POZİSYONLARINA yükle
        foreach (var slotData in data.slots)
        {
            var item = db.GetItemByID(slotData.itemID);
            if (item != null && slotData.slotIndex >= 0 && slotData.slotIndex < slotCount)
            {
                // Eşyayı orijinal pozisyonuna yerleştir
                slots[slotData.slotIndex] = new InventorySlot(item, slotData.amount);
            }
        }

        Debug.Log($"Envanter yüklendi: {saveFileName} - {data.slots.Count} item");
    }

    private void OnEnable()
    {
        if (slots.Count != slotCount)
        {
            slots.Clear();
            for (int i = 0; i < slotCount; i++)
                slots.Add(new InventorySlot());
        }
    }

    public bool AddItem(Item item, int amount = 1)
    {
        int remaining = amount;

        // Önce mevcut stack'lere ekle
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty && slot.item == item && item.stackable && slot.amount < 99)
            {
                int space = 99 - slot.amount;
                int toAdd = Mathf.Min(space, remaining);
                slot.amount += toAdd;
                remaining -= toAdd;
                if (remaining <= 0) return true;
            }
        }

        // Sonra boş slotlara ekle
        foreach (var slot in slots)
        {
            if (slot.IsEmpty)
            {
                int toAdd = Mathf.Min(99, remaining);
                slot.AddItem(item, toAdd);
                remaining -= toAdd;
                if (remaining <= 0) return true;
            }
        }

        return remaining <= 0;
    }
}

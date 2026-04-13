using System.Collections.Generic;

[System.Serializable]
public class InventoryData
{
    public List<ItemSlotData> slots = new();
}

[System.Serializable]
public class ItemSlotData
{
    public string itemID; // Her item'ın benzersiz ID'si olmalı
    public int amount;
    public int slotIndex;
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CraftingSlot
{
    public Item item;
    public int amount;
    public bool IsEmpty => item == null || amount <= 0;

    public CraftingSlot()
    {
        Clear();
    }

    public void SetItem(Item newItem, int newAmount)
    {
        item = newItem;
        amount = newAmount;
    }

    public void Clear()
    {
        item = null;
        amount = 0;
    }

    public bool CanAcceptItem(Item newItem, CraftingSlotType slotType)
    {
        if (newItem == null) return false;

        switch (slotType)
        {
            case CraftingSlotType.Ingredient:
                return CraftingSystem.Instance.CanPlaceInCraftingSlot(newItem.id);
            case CraftingSlotType.Output:
                return false; // Çıktı slotuna manuel yerleştirme yapılamaz
            default:
                return false;
        }
    }
}

public enum CraftingSlotType
{
    Ingredient,
    Output
}
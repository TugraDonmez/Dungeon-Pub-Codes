using UnityEngine;

[System.Serializable]
public class CookingSlot
{
    public Item item;
    public int amount;
    public bool IsEmpty => item == null || amount <= 0;

    public CookingSlot()
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

    public bool CanAcceptItem(Item newItem, CookingSlotType slotType)
    {
        if (newItem == null) return false;

        switch (slotType)
        {
            case CookingSlotType.FryingIngredient:
                return CookingSystem.Instance.CanPlaceInFryingSlot(newItem.id);
            case CookingSlotType.Liquid:
                return CookingSystem.Instance.CanPlaceInLiquidSlot(newItem.id);
            case CookingSlotType.BakingIngredient:
                return CookingSystem.Instance.CanPlaceInBakingSlot(newItem.id);
            case CookingSlotType.Output:
                return false; // Çıktı slotuna manuel yerleştirme yapılamaz
            default:
                return false;
        }
    }
}

public enum CookingSlotType
{
    FryingIngredient,
    Liquid,
    BakingIngredient,
    Output
}

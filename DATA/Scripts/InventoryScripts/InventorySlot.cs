using UnityEngine;

[System.Serializable]
public class InventorySlot
{
    public Item item;
    public int amount;

    public bool IsEmpty => item == null;

    public void Clear()
    {
        item = null;
        amount = 0;
    }
    public InventorySlot() { }

    public InventorySlot(Item item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }
    public void AddItem(Item newItem, int amountToAdd)
    {
        if (item != null && item == newItem && item.stackable)
        {
            int spaceLeft = 99 - amount;
            int amountToActuallyAdd = Mathf.Min(spaceLeft, amountToAdd);
            amount += amountToActuallyAdd;

            // Eğer fazla eklenecek varsa... (taşanlar için ileride kullanılabilir)
            int remaining = amountToAdd - amountToActuallyAdd;
            if (remaining > 0)
            {
                Debug.Log($"Yalnızca {amountToActuallyAdd} eklendi, {remaining} stoklanamadı.");
            }
        }
        else
        {
            item = newItem;
            amount = Mathf.Min(amountToAdd, 99);
        }
    }
}

using UnityEngine;

[System.Serializable]
public class HandSlot
{
    public Item item;
    public int amount;

    public bool IsEmpty => item == null || amount <= 0;

    public HandSlot()
    {
        Clear();
    }

    public HandSlot(Item item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }

    public void Clear()
    {
        item = null;
        amount = 0;
    }

    public void AddItem(Item newItem, int amountToAdd)
    {
        if (item != null && item == newItem && item.stackable)
        {
            int spaceLeft = 99 - amount;
            int amountToActuallyAdd = Mathf.Min(spaceLeft, amountToAdd);
            amount += amountToActuallyAdd;
        }
        else
        {
            item = newItem;
            amount = Mathf.Min(amountToAdd, 99);
        }
    }

    public void SetItem(Item newItem, int newAmount)
    {
        item = newItem;
        amount = newAmount;
    }
}
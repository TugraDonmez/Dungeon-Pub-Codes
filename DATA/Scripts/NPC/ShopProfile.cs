using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Shop Profile")]
public class ShopProfile : ScriptableObject
{
    public string npcName;
    public List<ShopItemEntry> itemsForSale;
}

[System.Serializable]
public class ShopItemEntry
{
    public Item item;
    public int basePrice;
    public int stock;

    public int GetPriceByRelationship(int relationScore)
    {
        if (relationScore >= 8) return Mathf.RoundToInt(basePrice * 0.8f);
        if (relationScore <= -5) return Mathf.RoundToInt(basePrice * 1.5f);
        return basePrice;
    }
}

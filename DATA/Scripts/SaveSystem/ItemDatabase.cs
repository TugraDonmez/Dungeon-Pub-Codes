using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ItemDatabase : ScriptableObject
{
    public List<Item> items;

    public Item GetItemByID(string id)
    {
        foreach (var item in items) // items listenizin adı ne ise
        {
            if (item.id == id)
                return item;
        }
        return null;
    }
}

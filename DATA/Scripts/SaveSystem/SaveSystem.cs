using System.IO;
using UnityEngine;

public static class SaveSystem
{
    public static void SaveInventory(InventoryData data, string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName + ".json");
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(path, json);
    }

    public static InventoryData LoadInventory(string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName + ".json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<InventoryData>(json);
        }
        return new InventoryData(); // boş döner
    }

}

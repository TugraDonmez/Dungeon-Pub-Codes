using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public Inventory inventory;
    public Transform slotContainer;
    public GameObject slotPrefab;

    void Start()
    {
        DrawInventory();
    }

    public void DrawInventory()
    {
        foreach (Transform child in slotContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < inventory.slots.Count; i++)
        {
            var slotGO = Instantiate(slotPrefab, slotContainer);
            var slotUI = slotGO.GetComponent<SlotUI>();
            slotUI.Setup(i, inventory, this);
        }
    }
}

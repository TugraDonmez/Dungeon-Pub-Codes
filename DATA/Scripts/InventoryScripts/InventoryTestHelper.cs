using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class InventoryTestHelper : MonoBehaviour
{
    public Inventory inventory;     // Envanter SO
    public Item itemToAdd;          // Eklenecek eşya
    public Item itemToAdd2;          // Eklenecek eşya
    public int amount = 1;          // Miktar

    public InventoryUI inventoryUI; // UI'yi güncellemek için

    public GameObject Inventory;
    bool invOpen = false;


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            invOpen = !invOpen;
            
        }
        switch (invOpen)
        {
            case true: Inventory.SetActive(true); 
                gameObject.GetComponent<PlayerMovement>().enabled = false; 
                gameObject.GetComponent<PlayerInteraction>().enabled = false; 
                GetComponent<PlayerCombat>().enabled = false; 
                break;
            case false: Inventory.SetActive(false); gameObject.GetComponent<PlayerMovement>().enabled = true; gameObject.GetComponent<PlayerInteraction>().enabled = true; GetComponent<PlayerCombat>().enabled = true; TooltipManager.Instance.HideTooltip(); break;

        }
    }

    public void AddItemToInventory()
    {
        bool added = inventory.AddItem(itemToAdd, amount);
        bool added2 = inventory.AddItem(itemToAdd2, amount);

        if (added)
        {
            Debug.Log($"{itemToAdd.name} eklendi.");
            inventoryUI.DrawInventory(); // UI'yi güncelle
        }
        else
        {
            Debug.Log("Envanter dolu!");
        }


        if (added2)
        {
            Debug.Log($"{itemToAdd2.name} eklendi.");
            inventoryUI.DrawInventory(); // UI'yi güncelle
        }
        else
        {
            Debug.Log("Envanter dolu!");
        }
    }

}

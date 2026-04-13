using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Counter : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject counterUI;
    public GameObject Target;
    private Animator animator;

    [Header("Counter Settings")]
    public string counterID = "counter_01"; // Her tezgah için benzersiz ID

    [Header("Crafting Manager")]
    public CraftingManager craftingManager; // Crafting yöneticisi referansı

    private void Start()
    {
        // Eğer crafting manager atanmamışsa, UI içinde ara
        if (craftingManager == null && counterUI != null)
        {
            craftingManager = counterUI.GetComponentInChildren<CraftingManager>();
        }
    }

    public void Interact()
    {
        if (counterUI.activeInHierarchy)
        {
            CloseCounter();
        }
        else
        {
            OpenCounter();
        }
    }

    public void OpenCounter()
    {
        counterUI.SetActive(true);
        Camera.main.GetComponent<PlayerCamera>().target = Target.transform;

        // Crafting manager'ı başlat
        if (craftingManager != null)
        {
            craftingManager.enabled = true;
        }
    }

    public void CloseCounter()
    {
        counterUI.SetActive(false);
        Camera.main.GetComponent<PlayerCamera>().target = GameObject.FindGameObjectWithTag("Player").transform;

        // Tezgahı kapatırken tüm slotları temizle ve eşyaları envantere geri ver
        if (craftingManager != null)
        {
            craftingManager.ClearAllSlots();
        }
    }
}
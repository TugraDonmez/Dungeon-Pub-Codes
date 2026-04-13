using UnityEngine;

public class PlayerInventoryManager : MonoBehaviour
{
    [Header("Inventory References")]
    public Inventory inventory;
    public InventoryUI inventoryUI;

    [Header("Audio")]
    public AudioClip itemPickupSound;

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public bool TryAddItem(Item item, int amount)
    {
        if (inventory == null) return false;

        bool success = inventory.AddItem(item, amount);

        if (success)
        {
            // UI güncelle
            if (inventoryUI != null)
            {
                inventoryUI.DrawInventory();
            }

            // Ses çal
            PlayPickupSound();

            // Debug log
            Debug.Log($"{item.name} x{amount} envantere eklendi!");

            return true;
        }
        else
        {
            Debug.Log($"Envanter dolu! {item.name} eklenemedi.");
            return false;
        }
    }

    private void PlayPickupSound()
    {
        if (itemPickupSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(itemPickupSound);
        }
    }
}

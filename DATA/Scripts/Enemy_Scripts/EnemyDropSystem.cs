using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemDrop
{
    public Item item;
    public int minAmount = 1;
    public int maxAmount = 1;
    [Range(0f, 100f)]
    public float dropChance = 50f;
}

public class EnemyDropSystem : MonoBehaviour
{
    [Header("Drop Settings")]
    public List<ItemDrop> possibleDrops = new List<ItemDrop>();
    public GameObject itemPickupPrefab; // Düşecek item prefab'ı

    [Header("Drop Physics")]
    public float dropForce = 5f;
    public float dropRadius = 1f;
    public int maxDropCount = 3;

    private void Start()
    {
        // MEVCUt Enemy script'inizle çalışması için güncellenmiş
        var enemy = GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.OnDeath += HandleEnemyDeath;
        }
    }

    private void HandleEnemyDeath()
    {
        DropItems();
    }

    public void DropItems()
    {
        int droppedCount = 0;

        foreach (var itemDrop in possibleDrops)
        {
            if (droppedCount >= maxDropCount) break;

            if (Random.Range(0f, 100f) <= itemDrop.dropChance)
            {
                int amount = Random.Range(itemDrop.minAmount, itemDrop.maxAmount + 1);
                CreateItemPickup(itemDrop.item, amount);
                droppedCount++;
            }
        }
    }

    private void CreateItemPickup(Item item, int amount)
    {
        if (itemPickupPrefab == null)
        {
            Debug.LogError("Item pickup prefab atanmamış!");
            return;
        }

        Vector2 dropPosition = GetRandomDropPosition();
        GameObject droppedItem = Instantiate(itemPickupPrefab, dropPosition, Quaternion.identity);

        // Hemen Initialize et (Awake çalıştıktan sonra)
        var pickupComponent = droppedItem.GetComponent<ItemPickup>();
        if (pickupComponent != null)
        {
            Debug.Log($"Item drop ediliyor: {item.itemName} x{amount}");
            pickupComponent.Initialize(item, amount);

            // Fizik efekti ekle
            var rb = droppedItem.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 randomDirection = Random.insideUnitCircle.normalized;
                rb.AddForce(randomDirection * dropForce, ForceMode2D.Impulse);
            }
        }
        else
        {
            Debug.LogError("ItemPickup component prefab'da bulunamadı!");
        }
    }

    private Vector2 GetRandomDropPosition()
    {
        Vector2 randomOffset = Random.insideUnitCircle * dropRadius;
        return (Vector2)transform.position + randomOffset;
    }

    private void OnDestroy()
    {
        var enemy = GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.OnDeath -= HandleEnemyDeath;
        }
    }
}
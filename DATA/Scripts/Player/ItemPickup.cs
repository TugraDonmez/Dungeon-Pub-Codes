using System.Collections;
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Item Data")]
    public Item item;
    public int amount = 1;

    [Header("Pickup Settings")]
    public float detectionRadius = 2f;
    public float magnetRadius = 1f;
    public float magnetSpeed = 8f;
    public float pickupDelay = 0.1f;

    [Header("Visual Effects")]
    public GameObject pickupEffect;
    public AudioClip pickupSound;

    private Transform player;
    private bool isBeingPickedUp = false;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private Rigidbody2D rb;
    private bool isInitialized = false;

    private void Awake()
    {
        // Component referanslarını al
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();

        Debug.Log("ItemPickup Awake çalıştı");
    }

    private void Start()
    {
        // Player'ı bul
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Eğer Initialize henüz çağrılmamışsa ama item varsa, sprite'ı set et
        if (!isInitialized && item != null)
        {
            Debug.Log("Start'da sprite set ediliyor");
            SetSprite();
        }

        // Spawn animasyonunu başlat
        StartCoroutine(SpawnAnimation());
    }

    public void Initialize(Item newItem, int newAmount)
    {
        Debug.Log($"Initialize çağrıldı: {newItem?.itemName}, Amount: {newAmount}");

        item = newItem;
        amount = newAmount;
        isInitialized = true;

        // Sprite'ı set et
        SetSprite();
    }

    private void SetSprite()
    {
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer bulunamadı!");
            return;
        }

        if (item == null)
        {
            Debug.LogError("Item null!");
            return;
        }

        if (item.icon == null)
        {
            Debug.LogError($"Item {item.itemName} icon'u null!");
            return;
        }

        spriteRenderer.sprite = item.icon;
        Debug.Log($"Sprite başarıyla set edildi: {item.itemName} -> {item.icon.name}");
    }

    private void Update()
    {
        if (player == null || isBeingPickedUp) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Manyetik çekim mesafesi içindeyse
        if (distanceToPlayer <= magnetRadius)
        {
            MoveTowardsPlayer();
        }

        // Toplama mesafesi içindeyse
        if (distanceToPlayer <= detectionRadius * 0.3f)
        {
            TryPickup();
        }
    }

    private void MoveTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;

        if (rb != null)
        {
            rb.linearVelocity = direction * magnetSpeed;
        }
        else
        {
            transform.position = Vector2.MoveTowards(transform.position, player.position, magnetSpeed * Time.deltaTime);
        }

        // Manyetik çekim efekti
        if (spriteRenderer)
        {
            float pulseIntensity = Mathf.Sin(Time.time * 10f) * 0.3f + 0.7f;
            spriteRenderer.color = new Color(1f, 1f, 1f, pulseIntensity);
        }
    }

    private void TryPickup()
    {
        if (isBeingPickedUp) return;

        var playerInventory = player.GetComponent<PlayerInventoryManager>();
        if (playerInventory != null)
        {
            StartCoroutine(PickupSequence(playerInventory));
        }
    }

    private IEnumerator PickupSequence(PlayerInventoryManager inventoryManager)
    {
        isBeingPickedUp = true;

        yield return new WaitForSeconds(pickupDelay);

        bool success = inventoryManager.TryAddItem(item, amount);

        if (success)
        {
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }

            if (pickupEffect != null)
            {
                Instantiate(pickupEffect, transform.position, Quaternion.identity);
            }

            yield return StartCoroutine(PickupAnimation());
            Destroy(gameObject);
        }
        else
        {
            isBeingPickedUp = false;
            Debug.Log("Envanter dolu! Item toplanamadı.");
        }
    }

    private IEnumerator SpawnAnimation()
    {
        Vector3 originalScale = transform.localScale;
        transform.localScale = Vector3.zero;

        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float scale = Mathf.Lerp(0f, 1f, t);
            transform.localScale = originalScale * scale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    private IEnumerator PickupAnimation()
    {
        float elapsed = 0f;
        float duration = 0.2f;
        Vector3 startScale = transform.localScale;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * 0.5f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            if (spriteRenderer)
            {
                Color color = spriteRenderer.color;
                color.a = Mathf.Lerp(1f, 0f, t);
                spriteRenderer.color = color;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, magnetRadius);
    }
}
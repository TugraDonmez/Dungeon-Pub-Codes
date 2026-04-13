using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private float knockbackDuration = 0.4f;
    [SerializeField] private float stunDuration = 0.6f; // Saldırı yapamama süresi
    [SerializeField] private float invulnerabilityDuration = 1f; // Hasar alamama süresi

    // Components
    private Rigidbody2D rb;
    private PlayerCombat playerCombat;
    private PlayerMovement playerMovement;

    // Knockback state
    private bool isKnockedBack = false;
    private bool isStunned = false;
    private bool isInvulnerable = false;

    // Properties for other scripts to check
    public bool IsKnockedBack => isKnockedBack;
    public bool IsStunned => isStunned;
    public bool IsInvulnerable => isInvulnerable;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => currentHealth / maxHealth;

    Animator anim;

    void Start()
    {
        currentHealth = maxHealth;
        InitializeComponents();
        anim = GetComponent<Animator>();
    }

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCombat = GetComponent<PlayerCombat>();
        playerMovement = GetComponent<PlayerMovement>();

        if (rb == null)
        {
            Debug.LogError("PlayerHealth needs a Rigidbody2D component for knockback!");
        }
    }

    public void TakeDamage(float damage)
    {
        // Don't take damage if invulnerable
        if (isInvulnerable)
        {
            Debug.Log("Player is invulnerable, damage ignored!");
            return;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        Debug.Log($"Player took {damage} damage. Health: {currentHealth}/{maxHealth}");

        // Start knockback effect
        StartKnockback();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void TakeDamage(float damage, Vector2 damageSource)
    {
        if(playerMovement.isDashing)
        {
            return;
        }
        // Don't take damage if invulnerable
        if (isInvulnerable)
        {
            Debug.Log("Player is invulnerable, damage ignored!");
            return;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        Debug.Log($"Player took {damage} damage. Health: {currentHealth}/{maxHealth}");

        // Start knockback effect with direction
        StartKnockback(damageSource);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void StartKnockback()
    {
        // Default knockback direction (random)
        Vector2 randomDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        StartKnockback(transform.position + (Vector3)randomDirection);
    }

    private void StartKnockback(Vector2 damageSource)
    {
        if (rb == null) return;

        // Calculate knockback direction (away from damage source)
        Vector2 knockbackDirection = ((Vector2)transform.position - damageSource).normalized;

        // If direction is zero, use random direction
        if (knockbackDirection.magnitude < 0.1f)
        {
            knockbackDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        }

        StartCoroutine(KnockbackCoroutine(knockbackDirection));
    }

    private IEnumerator KnockbackCoroutine(Vector2 direction)
    {
        // Set knockback state
        isKnockedBack = true;
        isStunned = true;
        isInvulnerable = true;
        anim.SetTrigger("GetDamage");
        // Notify other components about knockback
        if (playerCombat != null)
        {
            playerCombat.OnKnockbackStarted();
        }

        if (playerMovement != null)
        {
            playerMovement.OnKnockbackStarted();
        }

        // Apply knockback force
        rb.linearVelocity = Vector2.zero; // Reset current velocity
        rb.AddForce(direction * knockbackForce, ForceMode2D.Impulse);

        // Wait for knockback duration
        yield return new WaitForSeconds(knockbackDuration);

        // Stop knockback movement
        rb.linearVelocity = Vector2.zero;
        isKnockedBack = false;

        // Notify components that knockback is finished
        if (playerCombat != null)
        {
            playerCombat.OnKnockbackFinished();
        }

        if (playerMovement != null)
        {
            playerMovement.OnKnockbackFinished();
        }

        // Wait additional time for stun (can move but can't attack)
        float remainingStunTime = stunDuration - knockbackDuration;
        if (remainingStunTime > 0)
        {
            yield return new WaitForSeconds(remainingStunTime);
        }

        // End stun
        isStunned = false;

        // Notify components that stun is finished
        if (playerCombat != null)
        {
            playerCombat.OnStunFinished();
        }

        // Wait for invulnerability to end
        float remainingInvulnerabilityTime = invulnerabilityDuration - stunDuration;
        if (remainingInvulnerabilityTime > 0)
        {
            yield return new WaitForSeconds(remainingInvulnerabilityTime);
        }

        // End invulnerability
        isInvulnerable = false;

        Debug.Log("Player recovered from knockback, stun, and invulnerability!");
    }

    public void Heal(float healAmount)
    {
        currentHealth += healAmount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        Debug.Log($"Player healed {healAmount}. Health: {currentHealth}/{maxHealth}");
    }

    private void Die()
    {
        // Stop any ongoing coroutines
        StopAllCoroutines();

        Debug.Log("Player died!");
        // Burada ölüm logic'ini ekleyebilirsin

        // Notify other components about death
        if (playerCombat != null)
        {
            playerCombat.OnPlayerDeath();
        }

        if (playerMovement != null)
        {
            playerMovement.OnPlayerDeath();
        }
    }

    // Method to check if player can attack
    public bool CanAttack()
    {
        return !isKnockedBack && !isStunned && currentHealth > 0;
    }

    // Method to check if player can move normally
    public bool CanMoveNormally()
    {
        return !isKnockedBack && currentHealth > 0;
    }

    // Visual feedback for invulnerability (optional)
    private void Update()
    {
        // You can add visual effects here for invulnerability
        // For example, make player blink or change color
        if (isInvulnerable)
        {
            // Example: Blinking effect
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                float alpha = Mathf.PingPong(Time.time * 10f, 1f);
                Color color = spriteRenderer.color;
                color.a = alpha * 0.5f + 0.5f; // Alpha between 0.5 and 1
                spriteRenderer.color = color;
            }
        }
        else
        {
            // Reset alpha when not invulnerable
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = 1f;
                spriteRenderer.color = color;
            }
        }
    }
}
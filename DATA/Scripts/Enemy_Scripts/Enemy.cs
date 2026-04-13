using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Health Settings")]
    public int health = 3;

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.5f;
    [SerializeField] private float stunDuration = 0.8f; // Saldırı yapamama süresi

    // Components
    private Rigidbody2D rb;
    private EnemyAI enemyAI;

    // Knockback state
    private bool isKnockedBack = false;
    private bool isStunned = false;

    // Properties for EnemyAI to check
    public bool IsKnockedBack => isKnockedBack;
    public bool IsStunned => isStunned;

    public event System.Action OnDeath;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyAI = GetComponent<EnemyAI>();

        if (rb == null)
        {
            Debug.LogError("Enemy needs a Rigidbody2D component for knockback!");
        }

        if (enemyAI == null)
        {
            Debug.LogError("Enemy needs an EnemyAI component!");
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log("Enemy took damage! Health now: " + health);

        // Knockback effect
        StartKnockback();

        if (health <= 0)
        {
            Die();
        }
    }

    private void Update()
    {
        if (IsStunned)
        {
            // Example: Blinking effect
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                float alpha = Mathf.PingPong(Time.time * 10f, 1f);
                Color color = spriteRenderer.color;
                color.a = alpha * 0.5f + 0.5f; // Alpha between 0.5 and 1
                spriteRenderer.color = Color.red;
                spriteRenderer.color = color;
                GetComponent<Animator>().SetTrigger("Hit");
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
                spriteRenderer.color = Color.white;
            }
        }
    }


    public void TakeDamage(int damage, Vector2 damageSource)
    {
        health -= damage;
        Debug.Log("Enemy took damage! Health now: " + health);

        // Knockback effect with direction
        StartKnockback(damageSource);

        if (health <= 0)
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

        // Notify EnemyAI about knockback state
        if (enemyAI != null)
        {
            enemyAI.OnKnockbackStarted();
        }

        // Apply knockback force
        rb.linearVelocity = Vector2.zero; // Reset current velocity
        rb.AddForce(direction * knockbackForce, ForceMode2D.Impulse);

        // Wait for knockback duration
        yield return new WaitForSeconds(knockbackDuration);

        // Stop knockback movement
        rb.linearVelocity = Vector2.zero;
        isKnockedBack = false;

        // Notify EnemyAI that knockback is finished
        if (enemyAI != null)
        {
            enemyAI.OnKnockbackFinished();
        }

        // Wait additional time for stun (can't attack but can move)
        float remainingStunTime = stunDuration - knockbackDuration;
        if (remainingStunTime > 0)
        {
            yield return new WaitForSeconds(remainingStunTime);
        }

        // End stun
        isStunned = false;

        // Notify EnemyAI that stun is finished
        if (enemyAI != null)
        {
            enemyAI.OnStunFinished();
        }

        Debug.Log($"{gameObject.name} recovered from knockback and stun!");
    }

    void Die()
    {
        // Stop any ongoing coroutines
        StopAllCoroutines();

        // Notify EnemyAI about death if needed
        OnDeath?.Invoke();
        if (enemyAI != null)
        {
            enemyAI.OnDeath();
        }

        Destroy(gameObject, 0.1f);
    }

    // Method to check if enemy can attack
    public bool CanAttack()
    {
        return !isKnockedBack && !isStunned && health > 0;
    }

    // Method to check if enemy can move normally
    public bool CanMoveNormally()
    {
        return !isKnockedBack && health > 0;
    }
}
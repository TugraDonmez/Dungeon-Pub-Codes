using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PlayerCombat : MonoBehaviour
{
    [Header("Combat Settings")]
    public float attackRange = 1f;
    public int damage = 1;
    public float attackCooldown = 0.3f;
    public LayerMask enemyLayer;

    [Header("Combo System")]
    [SerializeField] private float comboWindow = 0.8f; // Combo sıfırlama süresi (kısaltıldı)
    [SerializeField] private float comboBuffer = 0.3f; // Input buffer süresi
    [SerializeField] private int maxComboCount = 3; // Maksimum combo sayısı
    [SerializeField] private bool enableComboBuffer = true; // Buffer sistemini aç/kapat
    [SerializeField] private float[] animationDurations = { 0.4f, 0.45f, 0.5f }; // Her combo animasyon süreleri

    [Header("Attack Movement")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float[] lungeDistances = { 0.15f, 0.25f, 0.35f }; // Her combo için farklı hamle mesafesi
    [SerializeField] private float[] lungeDurations = { 0.05f, 0.06f, 0.08f }; // Her combo için farklı hamle süresi
    [SerializeField] private float[] attackRanges = { 1f, 1.2f, 1.5f }; // Her combo için farklı menzil
    [SerializeField] private int[] comboDamage = { 1, 1, 2 }; // Her combo için farklı hasar

    [Header("Visual Effects")]
    [SerializeField] private float[] screenShakeIntensity = { 0.1f, .1f, .1f }; // Her combo için farklı sarsıntı

    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip[] comboSounds; // Her combo için ses efekti

    // Components
    private PlayerMovement playerMovement;
    private PlayerHealth playerHealth;
    private Animator animator;
    private AudioSource audioSource;

    // Combat state
    private bool isAttacking = false;
    private bool isKnockedBack = false;
    private bool isStunned = false;

    // Combo system variables
    private int currentComboIndex = 0; // 0, 1, 2 (Blend Tree değerleri)
    private float lastAttackTime = 0f;
    private bool inputBuffered = false;
    private float bufferStartTime = 0f;
    private Coroutine comboResetCoroutine;
    private bool canAdvanceCombo = false; // Combo ilerleme kontrolü
    private float currentAttackStartTime = 0f; // Saldırı başlangıç zamanı

    void Start()
    {
        InitializeComponents();
        InitializeComboSystem();
    }

    private void InitializeComponents()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerHealth = GetComponent<PlayerHealth>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (playerHealth == null)
        {
            Debug.LogError("PlayerCombat requires a PlayerHealth component!");
        }

        if (playerMovement == null)
        {
            Debug.LogError("PlayerCombat requires a PlayerMovement component!");
        }

        // Array boyutları kontrol
        ValidateArraySizes();
    }

    private void InitializeComboSystem()
    {
        currentComboIndex = 0;
        lastAttackTime = 0f;
        inputBuffered = false;
    }

    private void ValidateArraySizes()
    {
        // Dizi kontrol falan aga
        if (lungeDistances.Length != maxComboCount) System.Array.Resize(ref lungeDistances, maxComboCount);
        if (lungeDurations.Length != maxComboCount) System.Array.Resize(ref lungeDurations, maxComboCount);
        if (attackRanges.Length != maxComboCount) System.Array.Resize(ref attackRanges, maxComboCount);
        if (comboDamage.Length != maxComboCount) System.Array.Resize(ref comboDamage, maxComboCount);
        if (screenShakeIntensity.Length != maxComboCount) System.Array.Resize(ref screenShakeIntensity, maxComboCount);
        if (animationDurations.Length != maxComboCount) System.Array.Resize(ref animationDurations, maxComboCount);
    }

    void Update()
    {
        HandleInput();
        HandleComboBuffer();
        CheckComboTimeout(); // Yeni: Combo timeout kontrolü
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (CanAttack())
            {
                StartCoroutine(Attack());
            }
            else if (enableComboBuffer && CanBuffer())
            {
                // Input buffer sistemi - saldırı bitince otomatik saldır - burası not geri dönülcek
                BufferInput();
            }
        }
    }

    private void CheckComboTimeout()
    {
        // Eğer saldırı yapıyoruz ve animasyon süresi geçtiyse combo ilerleme izni ver
        if (isAttacking && !canAdvanceCombo)
        {
            float currentAnimDuration = animationDurations[currentComboIndex];
            if (Time.time - currentAttackStartTime >= currentAnimDuration * 0.7f) // Animasyonun %70'i tamamlandığında
            {
                canAdvanceCombo = true;
            }
        }

        // Combo reset kontrolü - hacımsal animasyon bittikten sonra belirli süre geçerse sıfırla
        if (!isAttacking && currentComboIndex > 0)
        {
            float timeSinceLastAttack = Time.time - lastAttackTime;
            if (timeSinceLastAttack > comboWindow)
            {
                Debug.Log("Combo reset due to timeout");
                ResetCombo();
            }
        }
    }

    private void HandleComboBuffer()
    {
        // Buffer'lı input varsa ve artık saldırı yapabiliyorsak
        if (inputBuffered && Time.time - bufferStartTime <= comboBuffer)
        {
            if (CanAttack())
            {
                inputBuffered = false;
                StartCoroutine(Attack());
            }
        }
        else if (inputBuffered && Time.time - bufferStartTime > comboBuffer)
        {
            // Buffer süresi doldu
            inputBuffered = false;
        }
    }

    private bool CanAttack()
    {
        return !isAttacking &&
               !isKnockedBack &&
               !isStunned &&
               (playerHealth == null || playerHealth.CanAttack()) &&
               (playerMovement == null || playerMovement.CanMove());
    }

    private bool CanBuffer()
    {
        // Buffer yapabilir miyiz?
        return isAttacking && !inputBuffered;
    }

    private void BufferInput()
    {
        inputBuffered = true;
        bufferStartTime = Time.time;
        Debug.Log("Input buffered for combo!");
    }

    IEnumerator Attack()
    {
        if (!CanAttack()) yield break;

        isAttacking = true;
        Vector2 lookDir = playerMovement.GetLookDirection();

        // Combo zamanlaması kontrol et
        CheckComboTiming();

        // Görsel efektler
        ApplyScreenShake();

        // Ses efekti çal
        PlayComboSound();

        // Attack animasyonunu tetikle
        if (animator != null)
        {
            animator.SetTrigger("Attack");
            animator.SetFloat("AttackX", lookDir.x);
            animator.SetFloat("AttackY", lookDir.y);
            animator.SetFloat("ComboIndex", currentComboIndex); // Blend Tree için

            Debug.Log($"Playing combo {currentComboIndex + 1}/3");
        }

        // Hız ayarlaması (combo ile birlikte artabilir)
        float comboSpeedMultiplier = 1f + (currentComboIndex * 0.1f);
        playerMovement.moveSpeed = 2 * comboSpeedMultiplier;

        // Lunge forward
        if (!isKnockedBack && playerMovement != null && playerMovement.CanMove())
        {
            yield return StartCoroutine(PerformLunge(lookDir));
        }

        // Saldırı hasarını uygula
        PerformAttackDamage(lookDir);

        // Combo ilerlet
        AdvanceCombo();

        // Attack cooldown (combo ile azalabilir)
        float adjustedCooldown = attackCooldown * (1f - (currentComboIndex * 0.1f));
        yield return new WaitForSeconds(Mathf.Max(adjustedCooldown, 0.1f));

        // Hızı normal hale getir
        playerMovement.moveSpeed = 5;
        isAttacking = false;

        // Combo reset timer'ını başlat
        StartComboResetTimer();
    }

    private void CheckComboTiming()
    {
        float timeSinceLastAttack = Time.time - lastAttackTime;

        // Eğer combo penceresi geçtiyse combo'yu sıfırla
        if (timeSinceLastAttack > comboWindow && lastAttackTime > 0)
        {
            ResetCombo();
            Debug.Log("Combo reset due to timing");
        }

        lastAttackTime = Time.time;
    }

    private void AdvanceCombo()
    {
        currentComboIndex = (currentComboIndex + 1) % maxComboCount;

        // Eğer combo tamamlandıysa özel efekt
        if (currentComboIndex == 0)
        {
            Debug.Log("Combo completed! Starting over...");
            OnComboCompleted();
        }
    }

    private void OnComboCompleted()
    {
        // Combo tamamlandığında özel efektler
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.23f, 0.15f);
        }

        // Burada partikül efekti, özel ses vs
    }

    private void ApplyScreenShake()
    {
        if (CameraShake.Instance != null)
        {
           // float intensity = screenShakeIntensity[currentComboIndex];

            switch (currentComboIndex)
            {
                case 0:
                    CameraShake.Instance.Shake(0.1f, 0.1f);
                    break;
                case 1:
                    CameraShake.Instance.Shake(0.1f, 0.1f);
                    break;
                case 2:
                    CameraShake.Instance.Shake(0.15f, 0.1f);
                    break;
            }
        }
    }

    private void PlayComboSound()
    {
        if (audioSource != null && comboSounds != null && comboSounds.Length > currentComboIndex)
        {
            if (comboSounds[currentComboIndex] != null)
            {
                audioSource.PlayOneShot(comboSounds[currentComboIndex]);
            }
        }
    }

    private void StartComboResetTimer()
    {
        // Mevcut timer'ı durdur
        if (comboResetCoroutine != null)
        {
            StopCoroutine(comboResetCoroutine);
        }

        // Yeni timer başlat
        comboResetCoroutine = StartCoroutine(ComboResetTimer());
    }

    private IEnumerator ComboResetTimer()
    {
        yield return new WaitForSeconds(comboWindow);

        // Eğer bu süre içinde yeni saldırı yapılmadıysa combo'yu sıfırla
        if (Time.time - lastAttackTime >= comboWindow)
        {
            ResetCombo();
            Debug.Log("Combo reset due to inactivity");
        }
    }

    private void ResetCombo()
    {
        currentComboIndex = 0;
        inputBuffered = false;

        if (comboResetCoroutine != null)
        {
            StopCoroutine(comboResetCoroutine);
            comboResetCoroutine = null;
        }
    }

    private IEnumerator PerformLunge(Vector2 lookDir)
    {
        Vector2 startPos = transform.position;
        float currentLungeDistance = lungeDistances[currentComboIndex];
        float currentLungeDuration = lungeDurations[currentComboIndex];

        Vector2 targetPos = startPos + lookDir.normalized * currentLungeDistance;
        float elapsed = 0f;

        while (elapsed < currentLungeDuration)
        {
            if (isKnockedBack) yield break;

            transform.position = Vector2.Lerp(startPos, targetPos, elapsed / currentLungeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void PerformAttackDamage(Vector2 lookDir)
    {
        float currentAttackRange = attackRanges[currentComboIndex];
        int currentDamage = comboDamage[currentComboIndex];

        Vector2 attackPos = (Vector2)transform.position + lookDir.normalized * currentAttackRange * 0.5f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPos, currentAttackRange * 0.4f, enemyLayer);

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<Enemy>(out Enemy enemy))
            {
                enemy.TakeDamage(currentDamage, transform.position);
                Debug.Log($"Player combo {currentComboIndex + 1} hit {enemy.name} for {currentDamage} damage!");
            }
        }
    }

    // Knockback ve stun metodları (önceki gibi)
    public void OnKnockbackStarted()
    {
        Debug.Log("PlayerCombat: Knockback started - combo reset");
        isKnockedBack = true;
        isStunned = true;

        StopAllCoroutines();
        isAttacking = false;
        ResetCombo(); // Knockback olduğunda combo'yu sıfırla
    }

    public void OnKnockbackFinished()
    {
        Debug.Log("PlayerCombat: Knockback finished");
        isKnockedBack = false;
    }

    public void OnStunFinished()
    {
        Debug.Log("PlayerCombat: Stun finished");
        isStunned = false;
    }

    public void OnPlayerDeath()
    {
        Debug.Log("PlayerCombat: Player died");
        StopAllCoroutines();
        isAttacking = false;
        isKnockedBack = true;
        isStunned = true;
        ResetCombo();
    }

    // Public getter metodları
    public bool IsInCombat() => isAttacking;
    public bool IsKnockedBack() => isKnockedBack;
    public bool IsStunned() => isStunned;
    public int GetCurrentComboIndex() => currentComboIndex;
    public bool IsInputBuffered() => inputBuffered;

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null || playerMovement == null) return;

        Vector2 dir = playerMovement.GetLookDirection();
        float currentAttackRange = Application.isPlaying ? attackRanges[currentComboIndex] : attackRange;
        Vector2 center = (Vector2)attackPoint.position + dir.normalized * currentAttackRange * 0.5f;

        // Combo durumuna göre renk
        if (isKnockedBack)
        {
            Gizmos.color = Color.gray;
        }
        else if (isStunned)
        {
            Gizmos.color = Color.yellow;
        }
        else if (isAttacking)
        {
            Gizmos.color = Color.green;
        }
        else
        {
            // Combo index'e göre renk gradyanı
            Color[] comboColors = { Color.red, Color.blue, Color.magenta };
            Gizmos.color = Application.isPlaying ? comboColors[currentComboIndex] : Color.red;
        }

        Gizmos.DrawWireSphere(center, currentAttackRange * 0.4f);

        // Lunge mesafesini göster
        if (playerMovement != null)
        {
            Vector2 lookDir = playerMovement.GetLookDirection();
            float currentLungeDistance = Application.isPlaying ? lungeDistances[currentComboIndex] : lungeDistances[0];
            Vector2 lungeEnd = (Vector2)transform.position + lookDir.normalized * currentLungeDistance;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, lungeEnd);
        }

        // Combo bilgisini göster
        if (Application.isPlaying)
        {
         //   UnityEditor.Handles.Label(transform.position + Vector3.up * 2, $"Combo: {currentComboIndex + 1}/3");
        }
    }
}
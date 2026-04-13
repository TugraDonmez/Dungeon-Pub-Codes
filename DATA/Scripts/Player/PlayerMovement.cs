using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] public float moveSpeed = 5f;
    [SerializeField] public float runSpeedMultiplier = 1.5f; // Koşma hızı çarpanı

    [Header("Dash Settings")]
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private LayerMask dashIgnoreLayers = -1; // Dash sırasında hangi layer'ları ignore edeceği

    [Header("Audio Settings")]
    [SerializeField] private bool enableFootstepAudio = true;

    // Components
    private Rigidbody2D rb;
    private Animator animator;
    private PlayerHealth playerHealth;
    [SerializeField] private CapsuleCollider2D playerCollider;
    private FootstepAudioSystem footstepAudioSystem; // Yeni eklenen component

    // Movement state
    private Vector2 movement;
    private Vector2 lastDirection = Vector2.down; // Varsayılan olarak aşağıya baksın
    private bool isKnockedBack = false;
    private bool isRunning = false; // Koşma durumu

    // Dash state
    public bool isDashing = false;
    private bool canDash = true;
    private Vector2 dashDirection;
    private float dashTimer;
    private float dashCooldownTimer;
    private int originalLayer; // Orijinal layer'ı sakla

    // Speed tracking for audio system
    private Vector2 lastPosition;
    private float currentSpeed;

    void Start()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerHealth = GetComponent<PlayerHealth>();
        footstepAudioSystem = GetComponent<FootstepAudioSystem>(); // Adım sesi sistemini al

        if (rb == null)
        {
            Debug.LogError("PlayerMovement requires a Rigidbody2D component!");
        }

        if (animator == null)
        {
            Debug.LogError("PlayerMovement requires an Animator component!");
        }

        if (playerCollider == null)
        {
            Debug.LogError("PlayerMovement requires a Collider2D component!");
        }

        // Footstep audio system kontrolü (zorunlu değil)
        if (footstepAudioSystem == null && enableFootstepAudio)
        {
            Debug.LogWarning("PlayerMovement: FootstepAudioSystem component not found! Footstep audio will be disabled.");
            enableFootstepAudio = false;
        }

        // Orijinal layer'ı sakla
        originalLayer = gameObject.layer;
    }

    void Update()
    {
        HandleDashCooldown();
        HandleInput();
        HandleDashInput();
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            HandleDash();
        }
        else
        {
            HandleMovement();
        }

        // Hız takibi (adım sesi sistemi için)
        currentSpeed = ((Vector2)transform.position - lastPosition).magnitude / Time.fixedDeltaTime;
        lastPosition = transform.position;
    }

    private void HandleInput()
    {
        // Only handle input if not knocked back, not dashing, and can move
        if (!isKnockedBack && !isDashing && CanMove())
        {
            // Giriş al
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");
            movement = movement.normalized;

            // Koşma kontrolü (Shift tuşu)
            isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // Hareket varsa yönü hatırla
            if (movement != Vector2.zero)
            {
                lastDirection = movement;
            }
        }
        else if (!isDashing) // Dash sırasında movement'ı sıfırla ama lastDirection'ı koru
        {
            movement = Vector2.zero;
            isRunning = false;
        }
    }

    private void HandleDashInput()
    {
        // Dash input kontrolü
        if (Input.GetKeyDown(KeyCode.C) && canDash && !isKnockedBack && !isDashing && CanMove())
        {
            StartDash();
        }
    }

    private void HandleDashCooldown()
    {
        // Dash cooldown timer
        if (!canDash)
        {
            dashCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer <= 0f)
            {
                canDash = true;
            }
        }
    }

    private void UpdateAnimations()
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", currentSpeed);
            animator.SetBool("IsRunning", isRunning && !isDashing); // Koşma animasyonu

            if (isKnockedBack)
            {
                animator.SetBool("IsMoving", false);
            }
            else if (isDashing)
            {
                animator.SetBool("IsMoving", true);
                animator.SetBool("IsDash", true);
                animator.SetFloat("MoveX", dashDirection.x);
                animator.SetFloat("MoveY", dashDirection.y);
            }
            else
            {
                animator.SetBool("IsDash", false);

                if (movement != Vector2.zero && currentSpeed >= 0.1f)
                {
                    animator.SetBool("IsMoving", true);
                    animator.SetFloat("MoveX", movement.x);
                    animator.SetFloat("MoveY", movement.y);
                }
                else
                {
                    animator.SetBool("IsMoving", false);
                    animator.SetFloat("MoveX", lastDirection.x);
                    animator.SetFloat("MoveY", lastDirection.y);
                }
            }
        }
    }

    private void HandleMovement()
    {
        // Only move if not knocked back, not dashing, and can move normally
        if (!isKnockedBack && !isDashing && CanMove())
        {
            // Koşma hızı hesaplama
            float currentMoveSpeed = moveSpeed;
            if (isRunning && movement.magnitude > 0.1f)
            {
                currentMoveSpeed *= runSpeedMultiplier;
            }

            rb.MovePosition(rb.position + movement * currentMoveSpeed * Time.fixedDeltaTime);
        }
        // Savrulma sırasında fizik motorunun kontrolü bırak
    }

    private void StartDash()
    {
        isDashing = true;
        canDash = false;
        dashCooldownTimer = dashCooldown;
        dashTimer = dashDuration;

        // Dash yönünü belirle (hareket varsa hareket yönü, yoksa son bakılan yön)
        dashDirection = movement != Vector2.zero ? movement : lastDirection;
        dashDirection = dashDirection.normalized;

        // Collision layer'ını değiştir (düşmanlar ile çarpışmayı engelle)
        SetDashCollisionState(true);

        Debug.Log($"Dash started in direction: {dashDirection}");
    }

    private void HandleDash()
    {
        dashTimer -= Time.fixedDeltaTime;
        animator.SetBool("IsDash", true);

        if (dashTimer > 0f)
        {
            // Dash hareketi - daha yumuşak hareket için velocity kullan
            float dashSpeed = dashDistance / dashDuration;
            rb.linearVelocity = dashDirection * dashSpeed;
        }
        else
        {
            // Dash bitir
            EndDash();
        }
    }

    private void EndDash()
    {
        isDashing = false;
        rb.linearVelocity = Vector2.zero; // Dash sonrası hızı sıfırla

        // Collision layer'ını normale çevir
        SetDashCollisionState(false);
        animator.SetBool("IsDash", false);

        Debug.Log("Dash ended");
    }

    private void SetDashCollisionState(bool isDashingState)
    {
        if (playerCollider == null) return;

        if (isDashingState)
        {
            playerCollider.enabled = false;
        }
        else
        {
            playerCollider.enabled = true;
        }
    }

    public bool CanMove()
    {
        return playerHealth == null || playerHealth.CanMoveNormally();
    }

    public bool CanTakeDamage()
    {
        // Dash sırasında hasar alınamaz
        return !isDashing;
    }

    // Combat sistemi için dışa açık yön alma fonksiyonu
    public Vector2 GetLookDirection()
    {
        return lastDirection;
    }

    public Vector2 GetMoveInput()
    {
        return movement;
    }

    // Adım sesi sistemi için ek getter'lar
    public bool IsRunning()
    {
        return isRunning && movement.magnitude > 0.1f && !isDashing;
    }

    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    // Methods called by PlayerHealth during knockback
    public void OnKnockbackStarted()
    {
        Debug.Log("PlayerMovement: Knockback started - movement disabled");
        isKnockedBack = true;
        movement = Vector2.zero;
        isRunning = false;

        // Dash'i iptal et ve collision'ı normale çevir
        if (isDashing)
        {
            SetDashCollisionState(false);
            EndDash();
        }
    }

    public void OnKnockbackFinished()
    {
        Debug.Log("PlayerMovement: Knockback finished - movement restored");
        isKnockedBack = false;
    }

    public void OnPlayerDeath()
    {
        Debug.Log("PlayerMovement: Player died - movement disabled");
        isKnockedBack = true;
        movement = Vector2.zero;
        isRunning = false;
        rb.linearVelocity = Vector2.zero;

        // Dash'i iptal et ve collision'ı normale çevir
        if (isDashing)
        {
            SetDashCollisionState(false);
            EndDash();
        }

        // Footstep audio'yu durdur
        if (enableFootstepAudio && footstepAudioSystem != null)
        {
            footstepAudioSystem.EnableFootsteps(false);
        }

        // Ölüm animasyonu
        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsDash", false);
            // İsteğe bağlı: Ölüm animasyonu için özel trigger
            // animator.SetTrigger("Death");
        }
    }

    // Public method to check knockback state
    public bool IsKnockedBack()
    {
        return isKnockedBack;
    }

    // Public method to check dash state
    public bool IsDashing()
    {
        return isDashing;
    }

    // Public method to get dash cooldown progress (0-1)
    public float GetDashCooldownProgress()
    {
        if (canDash) return 1f;
        return 1f - (dashCooldownTimer / dashCooldown);
    }

    // Footstep audio system control methods
    public void EnableFootstepAudio(bool enable)
    {
        enableFootstepAudio = enable;
        if (footstepAudioSystem != null)
        {
            footstepAudioSystem.EnableFootsteps(enable);
        }
    }

    public void SetFootstepVolume(float volume)
    {
        if (enableFootstepAudio && footstepAudioSystem != null)
        {
            footstepAudioSystem.SetVolume(volume);
        }
    }
}
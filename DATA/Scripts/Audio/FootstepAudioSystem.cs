using System.Collections;
using UnityEngine;

[System.Serializable]
public class FootstepSettings
{
    [Header("Footstep Timing")]
    [SerializeField] private float walkStepInterval = 0.5f;
    [SerializeField] private float runStepInterval = 0.3f;
    [SerializeField] private float dashStepInterval = 0.1f;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float walkVolume = 0.7f;
    [Range(0f, 1f)]
    [SerializeField] private float runVolume = 0.85f;
    [Range(0f, 1f)]
    [SerializeField] private float dashVolume = 1f;

    [Header("Pitch Variation")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.9f, 1.1f);

    public float WalkStepInterval => walkStepInterval;
    public float RunStepInterval => runStepInterval;
    public float DashStepInterval => dashStepInterval;
    public float WalkVolume => walkVolume;
    public float RunVolume => runVolume;
    public float DashVolume => dashVolume;
    public Vector2 PitchRange => pitchRange;
}

public enum MovementType
{
    Walking,
    Running,
    Dashing
}

[RequireComponent(typeof(AudioSource))]
public class FootstepAudioSystem : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private GroundDetector groundDetector;
    [SerializeField] private AudioSource footstepAudioSource;

    [Header("Settings")]
    [SerializeField] private FootstepSettings footstepSettings;

    [Header("Default Sounds")]
    [SerializeField] private FootstepSoundData defaultFootstepData;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    // Private variables
    private float stepTimer = 0f;
    private bool wasMoving = false;
    private GroundType currentGroundType;
    private MovementType currentMovementType = MovementType.Walking;

    // Performance optimization
    private WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();

    void Start()
    {
        InitializeSystem();
    }

    private void InitializeSystem()
    {
        // Auto-find components if not assigned
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (groundDetector == null)
            groundDetector = GetComponent<GroundDetector>();

        if (footstepAudioSource == null)
            footstepAudioSource = GetComponent<AudioSource>();

        // Validate required components
        if (playerMovement == null)
        {
            Debug.LogError("FootstepAudioSystem: PlayerMovement component not found!");
            enabled = false;
            return;
        }

        if (groundDetector == null)
        {
            Debug.LogError("FootstepAudioSystem: GroundDetector component not found!");
            enabled = false;
            return;
        }

        if (footstepAudioSource == null)
        {
            Debug.LogError("FootstepAudioSystem: AudioSource component not found!");
            enabled = false;
            return;
        }

        // Configure audio source
        ConfigureAudioSource();

        // Get initial ground type
        currentGroundType = groundDetector.GetCurrentGroundType();

        if (enableDebugLogs)
            Debug.Log($"FootstepAudioSystem initialized. Initial ground type: {(currentGroundType?.name ?? "None")}");
    }

    private void ConfigureAudioSource()
    {
        footstepAudioSource.playOnAwake = false;
        footstepAudioSource.loop = false;
        footstepAudioSource.spatialBlend = 0f; // 2D sound
    }

    void Update()
    {
        UpdateFootstepSystem();
    }

    private void UpdateFootstepSystem()
    {
        // Check if player can move and is not knocked back
        if (!playerMovement.CanMove() || playerMovement.IsKnockedBack())
        {
            ResetFootstepTimer();
            return;
        }

        // Update ground type
        var newGroundType = groundDetector.GetCurrentGroundType();
        if (newGroundType != currentGroundType)
        {
            currentGroundType = newGroundType;
            if (enableDebugLogs)
                Debug.Log($"Ground type changed to: {(currentGroundType?.name ?? "None")}");
        }

        // Check if player is moving
        bool isMoving = IsPlayerMoving();

        // Determine movement type
        UpdateMovementType();

        if (isMoving)
        {
            HandleMovingFootsteps();
        }
        else
        {
            HandleStoppedMovement();
        }

        wasMoving = isMoving;
    }

    private bool IsPlayerMoving()
    {
        Vector2 moveInput = playerMovement.GetMoveInput();
        return moveInput.magnitude > 0.1f || playerMovement.IsDashing();
    }

    private void UpdateMovementType()
    {
        if (playerMovement.IsDashing())
        {
            currentMovementType = MovementType.Dashing;
        }
        else
        {
            Vector2 moveInput = playerMovement.GetMoveInput();
            // Koşma için shift tuşunu kontrol edebilirsiniz
            bool isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            currentMovementType = isRunning ? MovementType.Running : MovementType.Walking;
        }
    }

    private void HandleMovingFootsteps()
    {
        float currentStepInterval = GetCurrentStepInterval();

        stepTimer += Time.deltaTime;

        if (stepTimer >= currentStepInterval)
        {
            PlayFootstepSound();
            stepTimer = 0f;
        }
    }

    private void HandleStoppedMovement()
    {
        if (wasMoving)
        {
            // Player just stopped moving
            ResetFootstepTimer();
        }
    }

    private float GetCurrentStepInterval()
    {
        switch (currentMovementType)
        {
            case MovementType.Running:
                return footstepSettings.RunStepInterval;
            case MovementType.Dashing:
                return footstepSettings.DashStepInterval;
            default:
                return footstepSettings.WalkStepInterval;
        }
    }

    private float GetCurrentVolume()
    {
        switch (currentMovementType)
        {
            case MovementType.Running:
                return footstepSettings.RunVolume;
            case MovementType.Dashing:
                return footstepSettings.DashVolume;
            default:
                return footstepSettings.WalkVolume;
        }
    }

    private void PlayFootstepSound()
    {
        FootstepSoundData soundData = GetCurrentSoundData();
        if (soundData == null) return;

        AudioClip clipToPlay = soundData.GetRandomClip();
        if (clipToPlay == null) return;

        // Configure audio source
        footstepAudioSource.clip = clipToPlay;
        footstepAudioSource.volume = GetCurrentVolume() * soundData.VolumeMultiplier;
        footstepAudioSource.pitch = Random.Range(footstepSettings.PitchRange.x, footstepSettings.PitchRange.y);

        // Play the sound
        footstepAudioSource.Play();

        if (enableDebugLogs)
        {
            Debug.Log($"Playing footstep sound: {clipToPlay.name} | Ground: {(currentGroundType?.name ?? "Default")} | Movement: {currentMovementType}");
        }
    }

    private FootstepSoundData GetCurrentSoundData()
    {
        if (currentGroundType != null && currentGroundType.FootstepSoundData != null)
        {
            return currentGroundType.FootstepSoundData;
        }

        return defaultFootstepData;
    }

    private void ResetFootstepTimer()
    {
        stepTimer = 0f;
    }

    // Public methods for external control
    public void SetVolume(float volume)
    {
        if (footstepAudioSource != null)
        {
            footstepAudioSource.volume = Mathf.Clamp01(volume);
        }
    }

    public void EnableFootsteps(bool enable)
    {
        enabled = enable;
        if (!enable)
        {
            ResetFootstepTimer();
            if (footstepAudioSource != null && footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Stop();
            }
        }
    }

    public GroundType GetCurrentGroundType()
    {
        return currentGroundType;
    }

    public MovementType GetCurrentMovementType()
    {
        return currentMovementType;
    }

    // Debug methods
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void OnDrawGizmosSelected()
    {
        if (groundDetector != null)
        {
            groundDetector.DrawDebugGizmos();
        }
    }
}
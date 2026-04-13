using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class DoorController : MonoBehaviour, IInteractable
{
    [Header("Door Configuration")]
    [SerializeField] private DoorData doorData;
    [SerializeField] private Transform doorVisual;
    [SerializeField] private Collider2D doorCollider;

    [Header("Animation")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openAnimationTrigger = "Open";
    [SerializeField] private string closeAnimationTrigger = "Close";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    // Events
    public static event Action<DoorController> OnDoorStateChanged;
    public event Action<bool> OnDoorToggled;

    // State
    private DoorState currentState = DoorState.Closed;
    private bool isTransitioning = false;

    // Properties
    public bool IsOpen => currentState == DoorState.Open;
    public bool IsClosed => currentState == DoorState.Closed;
    public bool IsTransitioning => isTransitioning;
    public DoorData DoorData => doorData;

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponents();
        ValidateSetup();
    }

    private void Start()
    {
        InitializeDoor();
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        if (doorCollider == null)
            doorCollider = GetComponent<Collider2D>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (doorAnimator == null && doorVisual != null)
            doorAnimator = doorVisual.GetComponent<Animator>();
    }

    private void ValidateSetup()
    {
        if (doorData == null)
        {
            Debug.LogError($"[DoorController] Door data is missing on {gameObject.name}!");
            enabled = false;
            return;
        }

        if (doorVisual == null)
        {
            Debug.LogWarning($"[DoorController] Door visual is not assigned on {gameObject.name}!");
        }
    }

    private void InitializeDoor()
    {
        SetDoorState(DoorState.Closed, false);
        ApplyDoorData();
    }

    private void ApplyDoorData()
    {
        if (doorData.overrideAudioClips)
        {
            openSound = doorData.openSound;
            closeSound = doorData.closeSound;
        }

        if (doorData.overrideAnimationTriggers)
        {
            openAnimationTrigger = doorData.openTrigger;
            closeAnimationTrigger = doorData.closeTrigger;
        }
    }

    #endregion

    #region IInteractable Implementation

    public void Interact()
    {
        if (isTransitioning)
        {
            if (enableDebugLogs)
                Debug.Log($"[DoorController] Door {gameObject.name} is currently transitioning.");
            return;
        }

        ToggleDoor();
    }

    #endregion

    #region Door Control

    public void ToggleDoor()
    {
        if (IsOpen)
            CloseDoor();
        else
            OpenDoor();
    }

    public void OpenDoor()
    {
        if (currentState == DoorState.Open || isTransitioning)
            return;

        StartCoroutine(ChangeDoorState(DoorState.Open));
    }

    public void CloseDoor()
    {
        if (currentState == DoorState.Closed || isTransitioning)
            return;

        StartCoroutine(ChangeDoorState(DoorState.Closed));
    }

    private IEnumerator ChangeDoorState(DoorState newState)
    {
        isTransitioning = true;

        if (enableDebugLogs)
            Debug.Log($"[DoorController] Changing door {gameObject.name} to {newState}");

        // Pre-transition setup
        OnStateChangeStarted(newState);

        // Play animation
        if (doorAnimator != null)
        {
            string trigger = newState == DoorState.Open ? openAnimationTrigger : closeAnimationTrigger;
            doorAnimator.SetTrigger(trigger);

            // Wait for animation duration
            yield return new WaitForSeconds(doorData.transitionDuration);
        }
        else
        {
            // Fallback if no animator
            yield return new WaitForSeconds(doorData.transitionDuration);
        }

        // Apply final state
        SetDoorState(newState, true);

        // Post-transition cleanup
        OnStateChangeCompleted(newState);

        isTransitioning = false;
    }

    private void OnStateChangeStarted(DoorState newState)
    {
        // Play sound
        PlayDoorSound(newState == DoorState.Open);

        // Invoke events
        OnDoorToggled?.Invoke(newState == DoorState.Open);
    }

    private void OnStateChangeCompleted(DoorState newState)
    {
        // Global event for door manager
        OnDoorStateChanged?.Invoke(this);
    }

    #endregion

    #region State Management

    private void SetDoorState(DoorState newState, bool notify = true)
    {
        currentState = newState;

        // Update collider
        if (doorCollider != null)
            doorCollider.isTrigger = !(newState == DoorState.Closed);

        if (notify && enableDebugLogs)
            Debug.Log($"[DoorController] Door {gameObject.name} state set to {newState}");
    }

    #endregion

    #region Audio

    private void PlayDoorSound(bool isOpening)
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = isOpening ? openSound : closeSound;
        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay, doorData.audioVolume);
        }
    }

    #endregion

    #region Public Utility

    public void SetDoorData(DoorData newDoorData)
    {
        doorData = newDoorData;
        ApplyDoorData();
    }

    public void ForceState(DoorState state)
    {
        StopAllCoroutines();
        isTransitioning = false;
        SetDoorState(state, true);
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        if (doorData == null) return;

        // Draw interaction area if available
        Gizmos.color = IsOpen ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
    }

    #endregion
}

// DoorState.cs - Kapı durumu enum'u
public enum DoorState
{
    Closed,
    Open,
    Locked
}

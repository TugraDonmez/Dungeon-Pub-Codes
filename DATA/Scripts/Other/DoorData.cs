using UnityEngine;

[CreateAssetMenu(fileName = "New Door Data", menuName = "Game Systems/Door Data")]
public class DoorData : ScriptableObject
{
    [Header("Timing")]
    public float transitionDuration = 0.5f;

    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float audioVolume = 1f;

    [Header("Audio Override")]
    public bool overrideAudioClips = false;
    public AudioClip openSound;
    public AudioClip closeSound;

    [Header("Animation Override")]
    public bool overrideAnimationTriggers = false;
    public string openTrigger = "Open";
    public string closeTrigger = "Close";

    [Header("Door Type")]
    public DoorType doorType = DoorType.Standard;
    public bool requiresKey = false;
    public string requiredKeyId = "";
}

public enum DoorType
{
    Standard,
    Automatic,
    Locked,
    OneWay
}
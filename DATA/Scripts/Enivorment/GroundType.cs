using UnityEngine;

[CreateAssetMenu(fileName = "New Ground Type", menuName = "Audio/Ground Type", order = 1)]
public class GroundType : ScriptableObject
{
    [Header("Ground Information")]
    [SerializeField] private string groundName;
    [SerializeField] private Color debugColor = Color.white;
    [SerializeField, TextArea(2, 4)] private string description;

    [Header("Audio Settings")]
    [SerializeField] private FootstepSoundData footstepSoundData;

    [Header("Visual Effects (Optional)")]
    [SerializeField] private GameObject dustParticlePrefab;
    [SerializeField] private Color dustColor = Color.gray;

    // Properties
    public string GroundName => groundName;
    public Color DebugColor => debugColor;
    public string Description => description;
    public FootstepSoundData FootstepSoundData => footstepSoundData;
    public GameObject DustParticlePrefab => dustParticlePrefab;
    public Color DustColor => dustColor;

    // Validation
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(groundName))
        {
            groundName = name;
        }
    }
}
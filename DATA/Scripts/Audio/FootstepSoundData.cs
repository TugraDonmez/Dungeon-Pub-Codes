using UnityEngine;

[CreateAssetMenu(fileName = "New Footstep Sound Data", menuName = "Audio/Footstep Sound Data", order = 2)]
public class FootstepSoundData : ScriptableObject
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip[] footstepClips;

    [Header("Audio Settings")]
    [Range(0f, 2f)]
    [SerializeField] private float volumeMultiplier = 1f;

    [Range(0f, 3f)]
    [SerializeField] private float pitchVariation = 0.1f;

    [Header("Advanced Settings")]
    [SerializeField] private bool preventRepeat = true;
    [SerializeField, Tooltip("Minimum time between playing the same clip")]
    private float repeatCooldown = 0.5f;

    // Private variables for preventing repeats
    private int lastPlayedIndex = -1;
    private float lastPlayTime = 0f;

    // Properties
    public float VolumeMultiplier => volumeMultiplier;
    public float PitchVariation => pitchVariation;
    public bool HasFootstepClips => footstepClips != null && footstepClips.Length > 0;
    public int ClipCount => footstepClips?.Length ?? 0;

    /// <summary>
    /// Rastgele bir adım sesi klibini döndürür
    /// </summary>
    public AudioClip GetRandomClip()
    {
        if (!HasFootstepClips)
            return null;

        // Tek klip varsa onu döndür
        if (footstepClips.Length == 1)
            return footstepClips[0];

        int selectedIndex;

        if (preventRepeat && footstepClips.Length > 1)
        {
            // Tekrar önleme sistemi
            selectedIndex = GetNonRepeatingRandomIndex();
        }
        else
        {
            // Tamamen rastgele seçim
            selectedIndex = Random.Range(0, footstepClips.Length);
        }

        // Seçili klibi kontrol et
        if (footstepClips[selectedIndex] == null)
        {
            Debug.LogWarning($"FootstepSoundData '{name}': Clip at index {selectedIndex} is null!");
            return GetFirstValidClip();
        }

        lastPlayedIndex = selectedIndex;
        lastPlayTime = Time.time;

        return footstepClips[selectedIndex];
    }

    /// <summary>
    /// Belirli bir indexteki klibi döndürür
    /// </summary>
    public AudioClip GetClip(int index)
    {
        if (!HasFootstepClips || index < 0 || index >= footstepClips.Length)
            return null;

        return footstepClips[index];
    }

    /// <summary>
    /// Tekrar etmeyen rastgele indeks döndürür
    /// </summary>
    private int GetNonRepeatingRandomIndex()
    {
        // Eğer cooldown süresi geçmişse, normal rastgele seçim yap
        if (Time.time - lastPlayTime > repeatCooldown)
        {
            return Random.Range(0, footstepClips.Length);
        }

        // Son çalınan klipten farklı bir klip seç
        int attempts = 0;
        int selectedIndex;

        do
        {
            selectedIndex = Random.Range(0, footstepClips.Length);
            attempts++;

            // Sonsuz döngüyü önle
            if (attempts > 10)
                break;

        } while (selectedIndex == lastPlayedIndex && footstepClips.Length > 1);

        return selectedIndex;
    }

    /// <summary>
    /// İlk geçerli (null olmayan) klibi döndürür
    /// </summary>
    private AudioClip GetFirstValidClip()
    {
        for (int i = 0; i < footstepClips.Length; i++)
        {
            if (footstepClips[i] != null)
                return footstepClips[i];
        }

        Debug.LogError($"FootstepSoundData '{name}': No valid clips found!");
        return null;
    }

    /// <summary>
    /// Tüm kliplerin geçerli olup olmadığını kontrol eder
    /// </summary>
    public bool ValidateClips()
    {
        if (!HasFootstepClips)
            return false;

        for (int i = 0; i < footstepClips.Length; i++)
        {
            if (footstepClips[i] == null)
            {
                Debug.LogWarning($"FootstepSoundData '{name}': Clip at index {i} is null!");
                return false;
            }
        }

        return true;
    }

    // Editor validation
    private void OnValidate()
    {
        // Volume multiplier kontrolü
        volumeMultiplier = Mathf.Clamp(volumeMultiplier, 0f, 2f);

        // Pitch variation kontrolü
        pitchVariation = Mathf.Clamp(pitchVariation, 0f, 3f);

        // Repeat cooldown kontrolü
        repeatCooldown = Mathf.Max(0f, repeatCooldown);

        // Klipleri validate et
        ValidateClips();
    }

    // Debug info
    public string GetDebugInfo()
    {
        return $"FootstepSoundData '{name}': {ClipCount} clips, Volume: {volumeMultiplier:F2}, Pitch Var: {pitchVariation:F2}";
    }
}
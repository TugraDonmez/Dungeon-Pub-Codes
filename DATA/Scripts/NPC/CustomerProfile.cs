using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Customer/CustomerProfile")]
public class CustomerProfile : ScriptableObject
{
    [Header("Basic Info")]
    public string customerName;
    public Sprite customerSprite;
    public float baseWaitTime = 60f; // Saniye cinsinden bekleme süresi

    [Header("Personality Traits")]
    [Range(0f, 1f)] public float patience = 0.5f; // Sabır seviyesi
    [Range(0f, 1f)] public float generosity = 0.5f; // Cömertlik seviyesi
    [Range(0f, 1f)] public float criticalness = 0.3f; // Eleştirel olma seviyesi

    [Header("Food Preferences")]
    public CookingType preferredCookingType;
    public float typePreferenceBonus = 0.2f; // Sevdiği türdeki yemekler için ekstra memnuniyet

    [Header("Tip Behavior")]
    public float minTipMultiplier = 0.8f; // Minimum %80 ödeme
    public float maxTipMultiplier = 1.5f; // Maksimum %150 ödeme

    [Header("Visual & Audio")]
    public Color nameColor = Color.white;
    public AudioClip greetingSound;
    public AudioClip happySound;
    public AudioClip angrySound;
}
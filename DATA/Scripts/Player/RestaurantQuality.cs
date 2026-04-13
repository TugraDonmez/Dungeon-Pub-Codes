using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Restaurant/QualitySystem")]
public class RestaurantQuality : ScriptableObject
{
    [Header("Quality Settings")]
    [Range(0f, 5f)] public float currentRating = 2.5f;
    public int totalCustomersServed = 0;
    public float totalSatisfactionSum = 0f;

    [Header("Rating Thresholds")]
    public float[] starThresholds = { 1f, 2f, 3f, 4f, 5f };
    public string[] ratingNames = { "Terrible", "Poor", "Average", "Good", "Excellent" };

    public void AddCustomerFeedback(CustomerSatisfaction satisfaction)
    {
        totalCustomersServed++;
        totalSatisfactionSum += satisfaction.satisfactionScore;

        // Ortalama rating hesapla (0-1 arası satisfaction'ı 0-5 arası ratinge dönüştür)
        currentRating = (totalSatisfactionSum / totalCustomersServed) * 5f;

        Debug.Log($"Restaurant rating updated: {GetRatingName()} ({currentRating:F1}/5.0)");
    }

    public string GetRatingName()
    {
        for (int i = 0; i < starThresholds.Length; i++)
        {
            if (currentRating <= starThresholds[i])
                return ratingNames[i];
        }
        return ratingNames[ratingNames.Length - 1];
    }

    public int GetStarCount()
    {
        return Mathf.RoundToInt(currentRating);
    }

    public void ResetRating()
    {
        currentRating = 2.5f;
        totalCustomersServed = 0;
        totalSatisfactionSum = 0f;
    }
}
using UnityEngine;

[System.Serializable]
public class CustomerOrder
{
    public CookingRecipe requestedRecipe;
    public float orderTime; // Siparişin verildiği zaman
    public float maxWaitTime; // Müşterinin bekleyebileceği maksimum süre

    public bool IsExpired => Time.time > orderTime + maxWaitTime;
    public float RemainingTime => Mathf.Max(0, (orderTime + maxWaitTime) - Time.time);
    public float WaitProgress => 1f - (RemainingTime / maxWaitTime); // 0-1 arası bekleme ilerlemesi
}

// CUSTOMER SATISFACTION
public enum SatisfactionLevel
{
    VeryAngry = 0,
    Angry = 1,
    Neutral = 2,
    Happy = 3,
    VeryHappy = 4
}

[System.Serializable]
public class CustomerSatisfaction
{
    public SatisfactionLevel level = SatisfactionLevel.Neutral;
    public float satisfactionScore = 0.5f; // 0-1 arası

    public void CalculateSatisfaction(CustomerProfile profile, CustomerOrder order, bool correctOrder)
    {
        satisfactionScore = 0.5f; // Base satisfaction

        if (!correctOrder)
        {
            satisfactionScore = 0.1f; // Wrong order = very low satisfaction
        }
        else
        {
            // Doğru sipariş verildi
            float waitProgress = order.WaitProgress;

            // Bekleme süresine göre memnuniyet hesapla
            if (waitProgress <= 0.3f) // Çok hızlı servis
                satisfactionScore = 1f;
            else if (waitProgress <= 0.6f) // Normal süre
                satisfactionScore = 0.8f;
            else if (waitProgress <= 0.8f) // Geç ama kabul edilebilir
                satisfactionScore = 0.5f;
            else // Çok geç
                satisfactionScore = 0.2f;

            // Müşteri özelliklerine göre ayarlama
            satisfactionScore += profile.generosity * 0.2f;
            satisfactionScore -= profile.criticalness * 0.3f;

            // Yemek türü tercihi bonusu
            if (order.requestedRecipe.cookingType == profile.preferredCookingType)
                satisfactionScore += profile.typePreferenceBonus;
        }

        satisfactionScore = Mathf.Clamp01(satisfactionScore);

        // Satisfaction level belirleme
        if (satisfactionScore >= 0.8f) level = SatisfactionLevel.VeryHappy;
        else if (satisfactionScore >= 0.6f) level = SatisfactionLevel.Happy;
        else if (satisfactionScore >= 0.4f) level = SatisfactionLevel.Neutral;
        else if (satisfactionScore >= 0.2f) level = SatisfactionLevel.Angry;
        else level = SatisfactionLevel.VeryAngry;
    }
}
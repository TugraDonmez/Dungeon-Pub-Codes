using UnityEngine;
using System.Collections;

public enum CustomerState
{
    Waiting,      // Sırada bekliyor
    Ordering,     // Sipariş veriyor
    WaitingFood,  // Yemeği bekliyor
    Eating,       // Yemek yiyor
    Paying,       // Ödeme yapıyor
    Leaving       // Gidiyor
}

public class Customer : MonoBehaviour
{
    [Header("References")]
    public CustomerProfile profile;
    public SpriteRenderer spriteRenderer;
    public GameObject satisfactionIndicator; // UI elementi

    [Header("Current State")]
    public CustomerState currentState = CustomerState.Waiting;
    public CustomerOrder currentOrder;
    public CustomerSatisfaction satisfaction;

    [Header("Timing")]
    public float eatingTime = 10f;
    public float paymentTime = 3f;

    // Events
    public System.Action<Customer> OnCustomerLeft;
    public System.Action<Customer, float> OnPaymentMade;

    private Coroutine currentCoroutine;

    public void Initialize(CustomerProfile customerProfile)
    {
        profile = customerProfile;
        satisfaction = new CustomerSatisfaction();

        if (spriteRenderer && profile.customerSprite)
            spriteRenderer.sprite = profile.customerSprite;

        // Başlangıç durumu
        currentState = CustomerState.Waiting;
    }

    public void StartOrdering(LearnedRecipes learnedRecipes)
    {
        if (learnedRecipes.knownRecipes.Count == 0) return;

        currentState = CustomerState.Ordering;

        // Rastgele bir tarif seç (müşteri tercihlerine göre ağırlıklı)
        CookingRecipe selectedRecipe = SelectRecipe(learnedRecipes);

        // Sipariş oluştur
        currentOrder = new CustomerOrder
        {
            requestedRecipe = selectedRecipe,
            orderTime = Time.time,
            maxWaitTime = profile.baseWaitTime * (1f + profile.patience)
        };

        currentState = CustomerState.WaitingFood;

        // Bekleme coroutine'ini başlat
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(WaitForFood());

        Debug.Log($"{profile.customerName} ordered {selectedRecipe.name}");
    }

    private CookingRecipe SelectRecipe(LearnedRecipes learnedRecipes)
    {
        // Tercihi olan yemek türü varsa ona öncelik ver
        var preferredRecipes = learnedRecipes.GetRecipesByType(profile.preferredCookingType);

        if (preferredRecipes.Count > 0 && Random.value < 0.7f) // %70 ihtimalle tercih ettiği türden
        {
            return preferredRecipes[Random.Range(0, preferredRecipes.Count)];
        }
        else
        {
            return learnedRecipes.knownRecipes[Random.Range(0, learnedRecipes.knownRecipes.Count)];
        }
    }

    public void ServeFood(CookingRecipe servedRecipe)
    {
        if (currentState != CustomerState.WaitingFood) return;

        bool correctOrder = (servedRecipe == currentOrder.requestedRecipe);

        // Memnuniyeti hesapla
        satisfaction.CalculateSatisfaction(profile, currentOrder, correctOrder);

        // Yemek yeme durumuna geç
        currentState = CustomerState.Eating;

        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(EatFood());

        UpdateSatisfactionIndicator();
        PlaySatisfactionSound();
    }

    private IEnumerator WaitForFood()
    {
        while (currentState == CustomerState.WaitingFood && !currentOrder.IsExpired)
        {
            yield return null;
        }

        if (currentOrder.IsExpired)
        {
            // Müşteri beklemekten sıkıldı
            satisfaction.satisfactionScore = 0f;
            satisfaction.level = SatisfactionLevel.VeryAngry;
            UpdateSatisfactionIndicator();
            PlaySatisfactionSound();

            StartCoroutine(LeaveAngry());
        }
    }

    private IEnumerator EatFood()
    {
        yield return new WaitForSeconds(eatingTime);

        currentState = CustomerState.Paying;
        StartCoroutine(MakePayment());
    }

    private IEnumerator MakePayment()
    {
        yield return new WaitForSeconds(paymentTime);

        // Ödeme hesapla
        float basePrice = GetRecipePrice(currentOrder.requestedRecipe);
        float tipMultiplier = Mathf.Lerp(profile.minTipMultiplier, profile.maxTipMultiplier, satisfaction.satisfactionScore);
        float finalPayment = basePrice * tipMultiplier;

        OnPaymentMade?.Invoke(this, finalPayment);

        Debug.Log($"{profile.customerName} paid ${finalPayment:F2} (satisfaction: {satisfaction.level})");

        StartCoroutine(Leave());
    }

    private IEnumerator LeaveAngry()
    {
        currentState = CustomerState.Leaving;
        yield return new WaitForSeconds(1f);
        OnCustomerLeft?.Invoke(this);
        Destroy(gameObject);
    }

    private IEnumerator Leave()
    {
        currentState = CustomerState.Leaving;
        yield return new WaitForSeconds(1f);
        OnCustomerLeft?.Invoke(this);
        Destroy(gameObject);
    }

    private float GetRecipePrice(CookingRecipe recipe)
    {
        // Bu fonksiyonu recipe'ye göre fiyat döndürecek şekilde ayarla
        // Şimdilik sabit değer
        return 30f;
    }

    private void UpdateSatisfactionIndicator()
    {
        if (satisfactionIndicator)
        {
            // Memnuniyet göstergesini güncelle (emoji, renk vs.)
        }
    }

    private void PlaySatisfactionSound()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (!audioSource) return;

        switch (satisfaction.level)
        {
            case SatisfactionLevel.VeryHappy:
            case SatisfactionLevel.Happy:
                if (profile.happySound) audioSource.PlayOneShot(profile.happySound);
                break;
            case SatisfactionLevel.Angry:
            case SatisfactionLevel.VeryAngry:
                if (profile.angrySound) audioSource.PlayOneShot(profile.angrySound);
                break;
        }
    }

    private void OnDestroy()
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);
    }
}
// 1. CustomerWithMovement'a IInteractable interface'ini ekleyin
using UnityEngine;
using System.Collections;

public class CustomerWithMovement : MonoBehaviour, IInteractable
{
    [Header("References")]
    public CustomerProfile profile;
    public SpriteRenderer spriteRenderer;
    public GameObject satisfactionIndicator;
    public GameObject interactionIndicator; // Etkileşim göstergesi (ör: "!" işareti)

    [Header("Movement")]
    public CustomerMovementController movementController;
    public Transform spawnPoint;

    [Header("Seat Assignment")]
    public CustomerSeat assignedSeat;

    [Header("Current State")]
    public CustomerState currentState = CustomerState.Waiting;
    public CustomerOrder currentOrder;
    public CustomerSatisfaction satisfaction;

    [Header("Timing")]
    public float eatingTime = 10f;
    public float paymentTime = 3f;

    [Header("Interaction")]
    public float interactionRange = 2f;

    // Events
    public System.Action<CustomerWithMovement> OnCustomerLeft;
    public System.Action<CustomerWithMovement, float> OnPaymentMade;

    private bool hasReachedSeat = false;
    private Coroutine currentCoroutine;
    private bool canInteract = false;

    private void Awake()
    {
        if (movementController == null)
            movementController = GetComponent<CustomerMovementController>();

        // Movement events
        if (movementController != null)
        {
            movementController.OnMovementCompleted += OnReachedSeat;
        }
    }

    private void Update()
    {
        UpdateInteractionState();
    }

    private void UpdateInteractionState()
    {
        // Sadece yemek bekleyen müşteriler etkileşime açık
        bool shouldBeInteractable = (currentState == CustomerState.WaitingFood && hasReachedSeat);

        if (shouldBeInteractable != canInteract)
        {
            canInteract = shouldBeInteractable;
            UpdateInteractionIndicator();
        }
    }

    private void UpdateInteractionIndicator()
    {
        if (interactionIndicator != null)
        {
            interactionIndicator.SetActive(canInteract);
        }
    }

    // IInteractable Implementation
    public void Interact()
    {
        if (!canInteract || currentState != CustomerState.WaitingFood)
        {
            Debug.Log($"{profile?.customerName ?? "Customer"} is not ready to receive food");
            return;
        }

        // BUG FIX 2: Hand slot manager'ı kontrol et
        if (HandSlotManager.Instance == null)
        {
            Debug.LogError("HandSlotManager.Instance is null!");
            return;
        }

        // BUG FIX 2: Aktif hand slot'tan 1 tane al (tüm stack'i değil!)
        if (HandSlotManager.Instance.TryTakeOneItemFromActiveSlot(out Item heldItem))
        {
            // Elindeki itemin hangi tariften geldiğini kontrol et
            if (!string.IsNullOrEmpty(heldItem.originRecipeID) &&
                currentOrder?.requestedRecipe != null &&
                heldItem.originRecipeID == currentOrder.requestedRecipe.recipeID)
            {
                ServeFood(currentOrder.requestedRecipe);
                Debug.Log($"Served 1 {heldItem.itemName} to {profile.customerName}");
            }
            else
            {
                // Yanlış item verildi, geri koy
                int activeSlotIndex = HandSlotManager.Instance.activeHandSlotIndex;
                var activeSlot = HandSlotManager.Instance.GetHandSlot(activeSlotIndex);

                if (activeSlot.IsEmpty)
                {
                    activeSlot.AddItem(heldItem, 1);
                }
                else if (activeSlot.item == heldItem)
                {
                    activeSlot.amount++;
                }

                HandSlotManager.Instance.UpdateHandSlotUI(activeSlotIndex);
                HandSlotManager.Instance.UpdateHandVisual();

                Debug.Log($"Item {heldItem.itemName} does not match customer's order! Item returned.");
            }
        }
        else
        {
            Debug.Log("Player has no item in hand to serve");
        }
    }



    private CookingRecipe FindRecipeByItem(Item item)
    {
        if (item == null) return null;
        CookingRecipe[] allRecipes = Resources.LoadAll<CookingRecipe>("Recipes");
        foreach (var recipe in allRecipes)
        {
            if (recipe.MatchesOutputItem(item))
                return recipe;
        }
        return null;
    }



    public void Initialize(CustomerProfile customerProfile)
    {
        profile = customerProfile;
        satisfaction = new CustomerSatisfaction();

        if (spriteRenderer && profile.customerSprite)
            spriteRenderer.sprite = profile.customerSprite;

        currentState = CustomerState.Waiting;
        UpdateInteractionIndicator();
    }

    public void StartOrdering(LearnedRecipes learnedRecipes)
    {
        if (learnedRecipes.knownRecipes.Count == 0) return;

        currentState = CustomerState.Ordering;

        CookingRecipe selectedRecipe = SelectRecipe(learnedRecipes);

        currentOrder = new CustomerOrder
        {
            requestedRecipe = selectedRecipe,
            orderTime = Time.time,
            maxWaitTime = profile.baseWaitTime * (1f + profile.patience)
        };

        currentState = CustomerState.WaitingFood;
        UpdateInteractionIndicator();

        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(WaitForFood());

        Debug.Log($"{profile.customerName} ordered {selectedRecipe.GetDisplayName()}");
    }

    private CookingRecipe SelectRecipe(LearnedRecipes learnedRecipes)
    {
        var preferredRecipes = learnedRecipes.GetRecipesByType(profile.preferredCookingType);

        if (preferredRecipes.Count > 0 && Random.value < 0.7f)
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

        satisfaction.CalculateSatisfaction(profile, currentOrder, correctOrder);

        currentState = CustomerState.Eating;
        UpdateInteractionIndicator();

        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(EatFood());

        UpdateSatisfactionIndicator();
        PlaySatisfactionSound();

        Debug.Log($"{profile.customerName} received {servedRecipe.GetDisplayName()}. Correct order: {correctOrder}");
    }

    private IEnumerator WaitForFood()
    {
        while (currentState == CustomerState.WaitingFood && !currentOrder.IsExpired)
        {
            yield return null;
        }

        if (currentOrder.IsExpired)
        {
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
        UpdateInteractionIndicator();

        yield return new WaitForSeconds(1f);
        OnCustomerLeft?.Invoke(this);
        Destroy(gameObject);
    }

    private IEnumerator Leave()
    {
        currentState = CustomerState.Leaving;
        UpdateInteractionIndicator();

        yield return new WaitForSeconds(1f);
        OnCustomerLeft?.Invoke(this);
        Destroy(gameObject);
    }

    private float GetRecipePrice(CookingRecipe recipe)
    {
        return 30f;
    }

    private void UpdateSatisfactionIndicator()
    {
        if (satisfactionIndicator)
        {
            // Memnuniyet göstergesini güncelle
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

    public void AssignSeat(CustomerSeat seatToAssign)
    {
        assignedSeat = seatToAssign;
        if (seatToAssign != null)
        {
            seatToAssign.OccupySeat(this);
        }
    }

    private void StartMovingToSeat()
    {
        if (assignedSeat != null && movementController != null)
        {
            Vector3 seatPosition = assignedSeat.GetSeatPosition();
            bool movementStarted = movementController.MoveTo(seatPosition);

            if (!movementStarted)
            {
                Debug.LogError($"Could not start movement to seat at {seatPosition}");
            }
        }
    }

    private void OnReachedSeat()
    {
        hasReachedSeat = true;
        UpdateInteractionIndicator();

        // Koltuk bonusu uygula
        if (assignedSeat != null && assignedSeat.comfortBonus > 0f)
        {
            // Başlangıç memnuniyetine bonus ekle
            satisfaction.satisfactionScore += assignedSeat.comfortBonus;
            satisfaction.satisfactionScore = Mathf.Clamp01(satisfaction.satisfactionScore);
        }

        // Sipariş vermeye başla
        StartCoroutine(DelayedOrderAfterSitting());
    }

    public void InitializeWithSeat(CustomerProfile customerProfile, CustomerSeat seat)
    {
        Initialize(customerProfile);
        AssignSeat(seat);
        StartMovingToSeat();

        hasReachedSeat = true;

        // Koltuk bonusu uygula
        if (assignedSeat != null && assignedSeat.comfortBonus > 0f)
        {
            // Başlangıç memnuniyetine bonus ekle
            satisfaction.satisfactionScore += assignedSeat.comfortBonus;
            satisfaction.satisfactionScore = Mathf.Clamp01(satisfaction.satisfactionScore);
        }

        // Sipariş vermeye başla
        StartCoroutine(DelayedOrderAfterSitting());
    }

    private IEnumerator DelayedOrderAfterSitting()
    {
        yield return new WaitForSeconds(1f); // Oturmak için 1 saniye bekle

        // Öğrenilen tarifleri al ve sipariş ver
        LearnedRecipes recipes = FindObjectOfType<CustomerManager>()?.learnedRecipes;
        if (recipes != null)
        {
            StartOrdering(recipes);
        }
    }

    private void OnDestroy()
    {
        // Koltuğu serbest bırak
        if (assignedSeat != null)
        {
            assignedSeat.FreeSeat();
        }

        // Movement events
        if (movementController != null)
        {
            movementController.OnMovementCompleted -= OnReachedSeat;
        }

        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);
    }

    public bool HasReachedSeat()
    {
        return hasReachedSeat;
    }

    // Oyuncunun etkileşim menzilinde olup olmadığını kontrol et
    public bool IsInInteractionRange(Transform playerTransform)
    {
        if (!canInteract || playerTransform == null) return false;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        return distance <= interactionRange;
    }

    // UI için sipariş bilgisi
    public string GetOrderInfo()
    {
        if (currentOrder == null) return "";

        string orderInfo = $"{profile.customerName}: {currentOrder.requestedRecipe.GetDisplayName()}";

        if (currentState == CustomerState.WaitingFood)
        {
            float remainingTime = currentOrder.RemainingTime;
            orderInfo += $" ({remainingTime:F0}s left)";
        }

        return orderInfo;
    }
}
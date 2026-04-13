using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class CustomerManagerWithMovement : MonoBehaviour
{
    [Header("References")]
    public CustomerProfile[] availableProfiles;
    public Transform customerSpawnPoint;
    public CustomerSeat[] customerSeats;
    public LearnedRecipes learnedRecipes;
    public RestaurantQuality restaurantQuality;

    [Header("Spawn Settings")]
    public int maxSimultaneousCustomers = 2;
    public float minSpawnDelay = 10f;
    public float maxSpawnDelay = 30f;

    [Header("Prefabs")]
    public CustomerWithMovement customerPrefab; // Updated prefab type

    // Runtime
    private List<CustomerWithMovement> activeCustomers = new();
    private Coroutine spawnCoroutine;

    // Events
    public System.Action<CustomerWithMovement> OnCustomerArrived;
    public System.Action<CustomerWithMovement> OnCustomerSeated;
    public System.Action<CustomerWithMovement> OnCustomerLeft;
    public System.Action<float> OnMoneyEarned;

    private void Start()
    {
        StartCustomerService();
    }

    public void StartCustomerService()
    {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnCustomers());
    }

    private IEnumerator SpawnCustomers()
    {
        while (true)
        {
            if (ShouldSpawnNewCustomer())
            {
                SpawnRandomCustomer();
            }

            float delay = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(delay);
        }
    }

    private bool ShouldSpawnNewCustomer()
    {
        return activeCustomers.Count < maxSimultaneousCustomers &&
               availableProfiles.Length > 0 &&
               learnedRecipes.knownRecipes.Count > 0 &&
               GetAvailableSeat() != null;
    }

    private void SpawnRandomCustomer()
    {
        CustomerSeat availableSeat = GetAvailableSeat();
        if (availableSeat == null) return;

        CustomerProfile profile = availableProfiles[Random.Range(0, availableProfiles.Length)];

        // Spawn point'te müşteriyi oluştur
        Vector3 spawnPos = customerSpawnPoint != null ? customerSpawnPoint.position : Vector3.zero;
        CustomerWithMovement newCustomer = Instantiate(customerPrefab, spawnPos, Quaternion.identity);

        // Koltuk ata ve hareket etmeye başlat
        newCustomer.InitializeWithSeat(profile, availableSeat);

        // Event'leri bağla
        newCustomer.OnCustomerLeft += OnCustomerLeftHandler;
        newCustomer.OnPaymentMade += OnPaymentMadeHandler;

        activeCustomers.Add(newCustomer);

        OnCustomerArrived?.Invoke(newCustomer);

        newCustomer.GetComponent<CustomerMovementController>().npcName.text = profile.customerName;

        Debug.Log($"{profile.customerName} spawned and moving to seat");
    }

    private CustomerSeat GetAvailableSeat()
    {
        foreach (var seat in customerSeats)
        {
            if (!seat.isOccupied)
                return seat;
        }
        return null;
    }

    private void OnCustomerLeftHandler(CustomerWithMovement customer)
    {
        activeCustomers.Remove(customer);

        if (restaurantQuality)
            restaurantQuality.AddCustomerFeedback(customer.satisfaction);

        OnCustomerLeft?.Invoke(customer);
    }

    private void OnPaymentMadeHandler(CustomerWithMovement customer, float amount)
    {
        PlayerWallet playerWallet = FindObjectOfType<PlayerWallet>();
        if (playerWallet != null)
        {
            playerWallet.AddMoney(Mathf.RoundToInt(amount));
        }

        OnMoneyEarned?.Invoke(amount);
        Debug.Log($"Earned ${amount:F2}");
    }

    public bool ServeRecipeToCustomer(CookingRecipe recipe)
    {
        CustomerWithMovement waitingCustomer = null;
        float closestDistance = float.MaxValue;

        foreach (var customer in activeCustomers)
        {
            if (customer.currentState == CustomerState.WaitingFood && customer.HasReachedSeat())
            {
                float distance = Vector3.Distance(transform.position, customer.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    waitingCustomer = customer;
                }
            }
        }

        if (waitingCustomer != null)
        {
            waitingCustomer.ServeFood(recipe);
            return true;
        }

        return false;
    }
}

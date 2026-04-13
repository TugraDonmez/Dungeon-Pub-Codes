using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class CustomerManager : MonoBehaviour
{
    [Header("References")]
    public CustomerProfile[] availableProfiles;
    public Transform customerSpawnPoint;
    public Transform[] customerSeats;
    public LearnedRecipes learnedRecipes;
    public RestaurantQuality restaurantQuality;

    [Header("Spawn Settings")]
    public int maxSimultaneousCustomers = 2;
    public float minSpawnDelay = 10f;
    public float maxSpawnDelay = 30f;

    [Header("Prefabs")]
    public Customer customerPrefab;

    // Runtime
    private List<Customer> activeCustomers = new();
    private Queue<CustomerProfile> customerQueue = new();
    private Coroutine spawnCoroutine;

    // Events
    public System.Action<Customer> OnCustomerArrived;
    public System.Action<Customer> OnCustomerLeft;
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

    public void StopCustomerService()
    {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
    }

    private IEnumerator SpawnCustomers()
    {
        while (true)
        {
            // Yeni müşteri spawn etmeye hazır mıyız?
            if (activeCustomers.Count < maxSimultaneousCustomers &&
                availableProfiles.Length > 0 &&
                learnedRecipes.knownRecipes.Count > 0)
            {
                SpawnRandomCustomer();
            }

            float delay = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(delay);
        }
    }

    private void SpawnRandomCustomer()
    {
        // Boş koltuk var mı kontrol et
        Transform availableSeat = GetAvailableSeat();
        if (!availableSeat) return;

        // Rastgele profil seç
        CustomerProfile profile = availableProfiles[Random.Range(0, availableProfiles.Length)];

        // Müşteriyi oluştur
        Customer newCustomer = Instantiate(customerPrefab, availableSeat.position, Quaternion.identity);
        newCustomer.Initialize(profile);

        // Event'leri bağla
        newCustomer.OnCustomerLeft += OnCustomerLeftHandler;
        newCustomer.OnPaymentMade += OnPaymentMadeHandler;

        // Listeye ekle
        activeCustomers.Add(newCustomer);

        // Sipariş vermeye başlat
        StartCoroutine(DelayedOrderStart(newCustomer));

        OnCustomerArrived?.Invoke(newCustomer);

        Debug.Log($"{profile.customerName} arrived at the restaurant");
    }

    private IEnumerator DelayedOrderStart(Customer customer)
    {
        yield return new WaitForSeconds(2f); // 2 saniye bekle
        customer.StartOrdering(learnedRecipes);
    }

    private Transform GetAvailableSeat()
    {
        foreach (var seat in customerSeats)
        {
            bool occupied = false;
            foreach (var customer in activeCustomers)
            {
                if (Vector3.Distance(customer.transform.position, seat.position) < 0.5f)
                {
                    occupied = true;
                    break;
                }
            }
            if (!occupied) return seat;
        }
        return null;
    }

    public bool ServeRecipeToCustomer(CookingRecipe recipe)
    {
        // En yakın bekleyen müşteriyi bul
        Customer waitingCustomer = null;
        float closestDistance = float.MaxValue;

        foreach (var customer in activeCustomers)
        {
            if (customer.currentState == CustomerState.WaitingFood)
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

    private void OnCustomerLeftHandler(Customer customer)
    {
        activeCustomers.Remove(customer);

        // Restoran kalitesini güncelle
        if (restaurantQuality)
            restaurantQuality.AddCustomerFeedback(customer.satisfaction);

        OnCustomerLeft?.Invoke(customer);
    }

    private void OnPaymentMadeHandler(Customer customer, float amount)
    {
        // Para sistemine entegrasyon
        PlayerWallet playerWallet = FindObjectOfType<PlayerWallet>();
        if (playerWallet != null)
        {
            playerWallet.AddMoney(Mathf.RoundToInt(amount));
        }

        OnMoneyEarned?.Invoke(amount);
        Debug.Log($"Earned ${amount:F2}");
    }

    public List<Customer> GetWaitingCustomers()
    {
        return activeCustomers.FindAll(c => c.currentState == CustomerState.WaitingFood);
    }

    public CustomerOrder GetNextOrder()
    {
        var waitingCustomers = GetWaitingCustomers();
        if (waitingCustomers.Count > 0)
            return waitingCustomers[0].currentOrder;
        return null;
    }
}
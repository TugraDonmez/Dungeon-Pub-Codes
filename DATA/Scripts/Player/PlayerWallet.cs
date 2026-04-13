using UnityEngine;
using System;

public class PlayerWallet : MonoBehaviour
{
    public static event Action<int> OnMoneyChanged;

    [SerializeField] private int startingMoney = 100;
    private int currentMoney;

    private void Awake()
    {
        currentMoney = startingMoney;
        OnMoneyChanged?.Invoke(currentMoney);
    }

    public bool HasEnoughMoney(int amount) => currentMoney >= amount;

    public void AddMoney(int amount)
    {
        currentMoney += amount;
        OnMoneyChanged?.Invoke(currentMoney);
    }

    public bool SpendMoney(int amount)
    {
        if (!HasEnoughMoney(amount)) return false;

        currentMoney -= amount;
        OnMoneyChanged?.Invoke(currentMoney);
        return true;
    }

    public int GetCurrentMoney() => currentMoney;
}

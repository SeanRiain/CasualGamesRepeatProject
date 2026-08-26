using System;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    [Header("Temporary Player")]
    [SerializeField] private string temporaryPlayerId = "local-test-player";
    [SerializeField] private string temporaryDisplayName = "Player";
    [SerializeField] private int temporaryStartingCurrency = 0;

    public PlayerData CurrentPlayer { get; private set; }

    public int Currency => CurrentPlayer.SoftCurrency;

    public event Action<int> CurrencyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateTemporaryPlayer();
    }

    private void CreateTemporaryPlayer()
    {
        CurrentPlayer = new PlayerData(
            temporaryPlayerId,
            temporaryDisplayName,
            temporaryStartingCurrency
        );
    }

    public void AddCurrency(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("Currency added must be greater than zero.");
            return;
        }

        int newBalance = Currency + amount;

        CurrentPlayer.SetSoftCurrency(newBalance);

        CurrencyChanged?.Invoke(Currency);

        Debug.Log(
            $"Added {amount} currency. New balance: {Currency}"
        );
    }

    public bool CanAfford(int amount)
    {
        if (amount < 0)
            return false;

        return Currency >= amount;
    }

    public bool TrySpendCurrency(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("Currency cost must be greater than zero.");
            return false;
        }

        if (!CanAfford(amount))
        {
            Debug.Log($"Cannot afford cost of {amount}. Current balance: {Currency}");

            return false;
        }

        int newBalance = Currency - amount;

        CurrentPlayer.SetSoftCurrency(newBalance);

        CurrencyChanged?.Invoke(Currency);

        Debug.Log(
            $"Spent {amount} currency. New balance: {Currency}"
        );

        return true;
    }


    //Temporary testing tools

    [ContextMenu("Debug/Add 100 Currency")]
    private void DebugAdd100()
    {
        AddCurrency(100);
    }

    [ContextMenu("Debug/Spend 50 Currency")]
    private void DebugSpend50()
    {
        TrySpendCurrency(50);
    }
}
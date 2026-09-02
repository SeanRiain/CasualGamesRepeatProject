using System;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    [Header("Default / Fallback Player")]
    [SerializeField] private string temporaryPlayerId = "local-test-player";
    [SerializeField] private string temporaryDisplayName = "Player";
    [SerializeField] private int temporaryStartingCurrency = 0;
    [SerializeField] private int temporaryStartingWins = 0;
    [SerializeField] private int temporaryStartingLosses = 0;

    public PlayerData CurrentPlayer { get; private set; }

    public bool IsReady { get; private set; }

    public bool IsCloudBacked { get; private set; }

    public string DefaultDisplayName => temporaryDisplayName;

    public string PlayerId => CurrentPlayer != null ? CurrentPlayer.PlayerId : string.Empty;

    public string DisplayName => CurrentPlayer != null ? CurrentPlayer.DisplayName : string.Empty;

    public int Currency => CurrentPlayer != null ? CurrentPlayer.SoftCurrency : 0;

    public int TotalWins => CurrentPlayer != null ? CurrentPlayer.TotalWins : 0;

    public int TotalLosses => CurrentPlayer != null ? CurrentPlayer.TotalLosses : 0;

    public event Action PlayerReady;
    public event Action<int> CurrencyChanged;
    public event Action<int, int> RecordChanged;
    public event Action CosmeticsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    internal PlayerData CreateDefaultCloudPlayer(string authenticatedPlayerId)
    {
        return new PlayerData(
            authenticatedPlayerId,
            temporaryDisplayName,
            temporaryStartingCurrency,
            temporaryStartingWins,
            temporaryStartingLosses);
    }

    internal PlayerData CreateLocalFallbackPlayer()
    {
        return new PlayerData(
            temporaryPlayerId,
            temporaryDisplayName,
            temporaryStartingCurrency,
            temporaryStartingWins,
            temporaryStartingLosses);
    }

    internal void InitializePlayer(PlayerData playerData, bool cloudBacked)
    {
        if (playerData == null)
        {
            Debug.LogError("[PlayerData] Cannot initialise with null player data.");
            return;
        }

        CurrentPlayer = playerData;
        IsCloudBacked = cloudBacked;
        IsReady = true;

        Debug.Log($"[PlayerData] Player account ready. ID: {PlayerId}. Cloud backed: {IsCloudBacked}.");

        PlayerReady?.Invoke();
        CurrencyChanged?.Invoke(Currency);
        RecordChanged?.Invoke(TotalWins, TotalLosses);
        CosmeticsChanged?.Invoke();
    }

    public void AddCurrency(int amount)
    {
        if (!CanMutatePlayer())
            return;

        if (amount <= 0)
        {
            Debug.LogWarning("Currency added must be greater than zero.");
            return;
        }

        int newBalance = Currency + amount;

        CurrentPlayer.SetSoftCurrency(newBalance);

        CurrencyChanged?.Invoke(Currency);

        Debug.Log($"Added {amount} currency. New balance: {Currency}");
    }

    public bool CanAfford(int amount)
    {
        if (!IsReady || CurrentPlayer == null)
        {
            return false;
        }

        if (amount < 0)
            return false;

        return Currency >= amount;
    }

    public bool TrySpendCurrency(int amount)
    {
        if (!CanMutatePlayer())
            return false;

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

        Debug.Log($"Spent {amount} currency. New balance: {Currency}");

        return true;
    }

    public void RecordWin()
    {
        if (!CanMutatePlayer())
            return;

        CurrentPlayer.AddWin();

        RecordChanged?.Invoke(TotalWins, TotalLosses);

        Debug.Log($"Recorded win. Total record: {TotalWins}-{TotalLosses}");
    }

    public void RecordLoss()
    {
        if (!CanMutatePlayer())
            return;

        CurrentPlayer.AddLoss();

        RecordChanged?.Invoke(TotalWins, TotalLosses);

        Debug.Log($"Recorded loss. Total record: {TotalWins}-{TotalLosses}");
    }

    public bool OwnsCosmetic(string cosmeticId)
    {
        if (!IsReady || CurrentPlayer == null)
        {
            return false;
        }

        return CurrentPlayer.OwnsCosmetic(cosmeticId);
    }

    public string GetEquippedCosmeticId(CosmeticCategory category)
    {
        if (!IsReady || CurrentPlayer == null)
        {
            return null;
        }

        return CurrentPlayer.GetEquippedCosmeticId(category);
    }

    public bool GrantCosmetic(string cosmeticId)
    {
        if (!CanMutatePlayer())
            return false;

        bool added = CurrentPlayer.AddOwnedCosmetic(cosmeticId);

        if (added)
        {
            CosmeticsChanged?.Invoke();
        }

        return added;
    }

    public bool EquipCosmetic(CosmeticCategory category, string cosmeticId)
    {
        if (!CanMutatePlayer())
            return false;

        bool changed = CurrentPlayer.SetEquippedCosmetic(category, cosmeticId);

        if (changed)
        {
            CosmeticsChanged?.Invoke();
        }

        return changed;
    }

    private bool CanMutatePlayer()
    {
        if (IsReady && CurrentPlayer != null)
        {
            return true;
        }

        Debug.LogWarning("[PlayerData] Player data is not ready for modification.");

        return false;
    }

    // Temporary testing tools

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

    [ContextMenu("Debug/Record Win")]
    private void DebugRecordWin()
    {
        RecordWin();
    }

    [ContextMenu("Debug/Record Loss")]
    private void DebugRecordLoss()
    {
        RecordLoss();
    }
}
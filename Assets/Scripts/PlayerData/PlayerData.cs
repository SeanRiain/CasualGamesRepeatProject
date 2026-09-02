using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerData
{
    [SerializeField]
    private string playerId;

    [SerializeField]
    private string displayName;

    [SerializeField]
    private int softCurrency;

    [SerializeField]
    private int totalWins;

    [SerializeField]
    private int totalLosses;

    [SerializeField]
    private List<string> ownedCosmeticIds = new List<string>();

    [SerializeField]
    private List<EquippedCosmeticData> equippedCosmetics = new List<EquippedCosmeticData>();

    public string PlayerId => playerId;

    public string DisplayName => displayName;

    public int SoftCurrency => softCurrency;

    public int TotalWins => totalWins;

    public int TotalLosses => totalLosses;

    public IReadOnlyList<string> OwnedCosmeticIds
    {
        get
        {
            EnsureCosmeticCollections();

            return ownedCosmeticIds;
        }
    }

    public IReadOnlyList<EquippedCosmeticData> EquippedCosmetics
    {
        get
        {
            EnsureCosmeticCollections();

            return equippedCosmetics;
        }
    }

    public PlayerData(string playerId, string displayName, int startingCurrency, int startingWins, int startingLosses)
    {
        this.playerId = playerId;
        this.displayName = displayName;

        softCurrency = Mathf.Max(0, startingCurrency);

        totalWins = Mathf.Max(0, startingWins);

        totalLosses = Mathf.Max(0, startingLosses);

        ownedCosmeticIds = new List<string>();

        equippedCosmetics = new List<EquippedCosmeticData>();
    }

    public void SetSoftCurrency(int amount)
    {
        softCurrency = Mathf.Max(0, amount);
    }

    public void AddWin()
    {
        totalWins++;
    }

    public void AddLoss()
    {
        totalLosses++;
    }

    private void EnsureCosmeticCollections()
    {
        if (ownedCosmeticIds == null)
        {
            ownedCosmeticIds = new List<string>();
        }

        if (equippedCosmetics == null)
        {
            equippedCosmetics = new List<EquippedCosmeticData>();
        }
    }

    public bool OwnsCosmetic(string cosmeticId)
    {
        EnsureCosmeticCollections();

        if (string.IsNullOrWhiteSpace(cosmeticId))
            return false;

        return ownedCosmeticIds.Contains(cosmeticId);
    }

    public bool AddOwnedCosmetic(string cosmeticId)
    {
        EnsureCosmeticCollections();

        if (string.IsNullOrWhiteSpace(cosmeticId))
            return false;

        if (ownedCosmeticIds.Contains(cosmeticId))
            return false;

        ownedCosmeticIds.Add(cosmeticId);

        return true;
    }

    public string GetEquippedCosmeticId(CosmeticCategory category)
    {
        EnsureCosmeticCollections();

        foreach (EquippedCosmeticData equipped in equippedCosmetics)
        {
            if (equipped.Category == category)
            {
                return equipped.CosmeticId;
            }
        }

        return null;
    }

    public bool SetEquippedCosmetic(CosmeticCategory category, string cosmeticId)
    {
        EnsureCosmeticCollections();

        if (!OwnsCosmetic(cosmeticId))
            return false;

        foreach (
            EquippedCosmeticData equipped
            in equippedCosmetics)
        {
            if (equipped.Category != category)
                continue;

            if (equipped.CosmeticId == cosmeticId)
                return false;

            equipped.SetCosmeticId(cosmeticId);

            return true;
        }

        equippedCosmetics.Add(new EquippedCosmeticData(category, cosmeticId));

        return true;
    }
}
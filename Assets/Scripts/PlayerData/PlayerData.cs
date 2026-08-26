using System;
using UnityEngine;

[Serializable]
public class PlayerData
{
    [SerializeField] private string playerId;
    [SerializeField] private string displayName;
    [SerializeField] private int softCurrency;

    [SerializeField] private int totalWins;
    [SerializeField] private int totalLosses;


    public int TotalWins => totalWins;
    public int TotalLosses => totalLosses;

    public void AddWin()
    {
        totalWins++;
    }

    public void AddLoss()
    {
        totalLosses++;
    }

    public string PlayerId => playerId;
    public string DisplayName => displayName;
    public int SoftCurrency => softCurrency;

    public PlayerData(string playerId, string displayName, int startingCurrency, int startingWins, int startingLosses)
    {
        this.playerId = playerId;
        this.displayName = displayName;
        softCurrency = Mathf.Max(0, startingCurrency);

        totalWins = Mathf.Max(0, startingWins);
        totalLosses = Mathf.Max(0, startingLosses);
    }

    public void SetSoftCurrency(int amount)
    {
        softCurrency = Mathf.Max(0, amount);
    }
}
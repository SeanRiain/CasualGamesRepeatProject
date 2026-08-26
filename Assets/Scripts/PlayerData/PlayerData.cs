using System;
using UnityEngine;

[Serializable]
public class PlayerData
{
    [SerializeField] private string playerId;
    [SerializeField] private string displayName;
    [SerializeField] private int softCurrency;

    public string PlayerId => playerId;
    public string DisplayName => displayName;
    public int SoftCurrency => softCurrency;

    public PlayerData(string playerId, string displayName, int startingCurrency)
    {
        this.playerId = playerId;
        this.displayName = displayName;
        softCurrency = Mathf.Max(0, startingCurrency);
    }

    public void SetSoftCurrency(int amount)
    {
        softCurrency = Mathf.Max(0, amount);
    }
}
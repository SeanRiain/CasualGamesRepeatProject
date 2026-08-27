using System;
using UnityEngine;

[Serializable]
public class PlayerProfileData
{
    [SerializeField] private string playerId;
    [SerializeField] private string displayName;

    public string PlayerId => playerId;
    public string DisplayName => displayName;

    public PlayerProfileData(string playerId, string displayName)
    {
        this.playerId = playerId;
        this.displayName = displayName;
    }
}
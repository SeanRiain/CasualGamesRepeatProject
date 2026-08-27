using System;
using UnityEngine;

[Serializable]
public class MatchSetupData
{
    [SerializeField] private string hostPlayerId;
    [SerializeField] private string opponentPlayerId;
    [SerializeField] private string matchReason;

    public string HostPlayerId => hostPlayerId;
    public string OpponentPlayerId => opponentPlayerId;
    public string MatchReason => matchReason;

    public MatchSetupData(string hostPlayerId, string opponentPlayerId, string matchReason)
    {
        this.hostPlayerId = hostPlayerId;
        this.opponentPlayerId = opponentPlayerId;
        this.matchReason = matchReason;
    }
}
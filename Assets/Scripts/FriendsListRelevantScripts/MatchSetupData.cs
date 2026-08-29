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

    public bool ContainsPlayer(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return false;

        return string.Equals(hostPlayerId, playerId, StringComparison.Ordinal) || string.Equals(opponentPlayerId, playerId, StringComparison.Ordinal);
    }

    public string GetOtherPlayerId(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return null;

        if (string.Equals(hostPlayerId, playerId, StringComparison.Ordinal))
        {
            return opponentPlayerId;
        }

        if (string.Equals(opponentPlayerId, playerId, StringComparison.Ordinal))
        {
            return hostPlayerId;
        }

        return null;
    }
}
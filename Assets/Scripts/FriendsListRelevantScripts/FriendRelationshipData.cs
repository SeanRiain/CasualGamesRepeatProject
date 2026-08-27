using System;
using UnityEngine;

[Serializable]
public class FriendRelationshipData
{
    [SerializeField] private string playerAId;
    [SerializeField] private string playerBId;

    [SerializeField] private int playerAWins;
    [SerializeField] private int playerBWins;

    [SerializeField] private int matchesPlayed;

    public string PlayerAId => playerAId;
    public string PlayerBId => playerBId;

    public int PlayerAWins => playerAWins;
    public int PlayerBWins => playerBWins;

    public int MatchesPlayed => matchesPlayed;

    public string RelationshipKey =>
        $"{playerAId}::{playerBId}";

    public FriendRelationshipData(
        string firstPlayerId,
        string secondPlayerId)
    {
        //Always store IDs in the same order! This prevents A-B and B-A from becoming two conceptually different relationships.

        if (string.CompareOrdinal(
                firstPlayerId,
                secondPlayerId) <= 0)
        {
            playerAId = firstPlayerId;
            playerBId = secondPlayerId;
        }
        else
        {
            playerAId = secondPlayerId;
            playerBId = firstPlayerId;
        }

        playerAWins = 0;
        playerBWins = 0;

        matchesPlayed = 0;
    }

    public bool ContainsPlayer(string playerId)
    {
        return playerAId == playerId ||
               playerBId == playerId;
    }

    public string GetOtherPlayerId(string playerId)
    {
        if (playerAId == playerId)
            return playerBId;

        if (playerBId == playerId)
            return playerAId;

        return null;
    }

    public int GetWinsFor(string playerId)
    {
        if (playerAId == playerId)
            return playerAWins;

        if (playerBId == playerId)
            return playerBWins;

        return 0;
    }

    public int GetLossesFor(string playerId)
    {
        if (playerAId == playerId)
            return playerBWins;

        if (playerBId == playerId)
            return playerAWins;

        return 0;
    }

    public bool TryRecordCompletedMatch(string winnerPlayerId)
    {
        if (winnerPlayerId == playerAId)
        {
            playerAWins++;
        }
        else if (winnerPlayerId == playerBId)
        {
            playerBWins++;
        }
        else
        {
            return false;
        }

        matchesPlayed++;

        return true;
    }
}

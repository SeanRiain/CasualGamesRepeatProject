using System;
using System.Collections.Generic;
using UnityEngine;

public class FriendsManager : MonoBehaviour
{
    public static FriendsManager Instance { get; private set; }

    [Header("Temporary Registered Account")]
    [SerializeField]
    private string demoFriendPlayerId = "test-friend-001";

    [SerializeField]
    private string demoFriendDisplayName = "Demo Friend";

    [Header("Runtime Data")]
    [SerializeField]
    private List<PlayerProfileData> temporaryRegisteredAccounts = new List<PlayerProfileData>();

    [SerializeField]
    private List<FriendRelationshipData> relationships = new List<FriendRelationshipData>();

    public event Action FriendsChanged;

    public MatchSetupData PreparedChallenge { get; private set; }

    public IReadOnlyList<FriendRelationshipData> Relationships => relationships;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }

        Instance = this;

        BuildTemporaryRegisteredAccountDirectory();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void BuildTemporaryRegisteredAccountDirectory()
    {
        temporaryRegisteredAccounts.Clear();

        temporaryRegisteredAccounts.Add(new PlayerProfileData(demoFriendPlayerId, demoFriendDisplayName));
    }

    public PlayerProfileData FindRegisteredAccount(string playerId)
    {
        foreach (PlayerProfileData profile in temporaryRegisteredAccounts)
        {
            if (string.Equals(profile.PlayerId, playerId, StringComparison.Ordinal))
            {
                return profile;
            }
        }

        return null;
    }

    public PlayerProfileData GetProfileForPlayer(string playerId)
    {
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.PlayerId == playerId)
        {
            return new PlayerProfileData(PlayerDataManager.Instance.PlayerId, PlayerDataManager.Instance.DisplayName);
        }

        return FindRegisteredAccount(playerId);
    }

    public List<FriendRelationshipData>
        GetRelationshipsForPlayer(string playerId)
    {
        List<FriendRelationshipData> result = new List<FriendRelationshipData>();

        foreach (FriendRelationshipData relationship in relationships)
        {
            if (relationship.ContainsPlayer(playerId))
            {
                result.Add(relationship);
            }
        }

        return result;
    }

    public List<FriendRelationshipData>
        GetCurrentPlayerRelationships()
    {
        if (PlayerDataManager.Instance == null)
            return new List<FriendRelationshipData>();

        return GetRelationshipsForPlayer(PlayerDataManager.Instance.PlayerId);
    }

    public FriendRelationshipData GetRelationshipBetween(string firstPlayerId, string secondPlayerId)
    {
        foreach (FriendRelationshipData relationship in relationships)
        {
            if (relationship.ContainsPlayer(firstPlayerId) && relationship.ContainsPlayer(secondPlayerId))
            {
                return relationship;
            }
        }

        return null;
    }

    public FriendRelationshipData GetRelationshipWith(string friendPlayerId)
    {
        if (PlayerDataManager.Instance == null)
            return null;

        return GetRelationshipBetween(PlayerDataManager.Instance.PlayerId, friendPlayerId);
    }

    public bool AreFriends(string otherPlayerId)
    {
        return GetRelationshipWith(otherPlayerId) != null;
    }

    public bool TryAddFriend(string targetPlayerId, out string message)
    {
        if (PlayerDataManager.Instance == null)
        {
            message = "Current player account is unavailable.";
            return false;
        }

        string cleanedId = targetPlayerId?.Trim();

        if (string.IsNullOrWhiteSpace(cleanedId))
        {
            message = "Enter a player ID.";
            return false;
        }

        string currentPlayerId =
            PlayerDataManager.Instance.PlayerId;

        if (cleanedId == currentPlayerId)
        {
            message = "You cannot add your own account.";
            return false;
        }

        PlayerProfileData targetProfile = FindRegisteredAccount(cleanedId);

        if (targetProfile == null)
        {
            message = "No registered account was found with that ID.";
            return false;
        }

        if (AreFriends(cleanedId))
        {
            message = $"{targetProfile.DisplayName} is already your friend.";

            return false;
        }

        FriendRelationshipData relationship = new FriendRelationshipData(currentPlayerId, cleanedId);

        relationships.Add(relationship);

        FriendsChanged?.Invoke();

        message = $"Added {targetProfile.DisplayName}.";

        return true;
    }

    public bool RecordMatchResult(string opponentPlayerId, bool currentPlayerWon)
    {
        if (PlayerDataManager.Instance == null)
            return false;

        FriendRelationshipData relationship = GetRelationshipWith(opponentPlayerId);

        if (relationship == null)
        {
            Debug.LogWarning("Cannot record pair result because the opponent is not in the friends list.");

            return false;
        }

        string winnerPlayerId = currentPlayerWon ? PlayerDataManager.Instance.PlayerId : opponentPlayerId;

        bool resultRecorded = relationship.TryRecordCompletedMatch(winnerPlayerId);

        if (!resultRecorded)
            return false;

        FriendsChanged?.Invoke();

        Debug.Log($"Pair record updated. Opponent: {opponentPlayerId}, " +
            $"Current player record: {relationship.GetWinsFor(PlayerDataManager.Instance.PlayerId)}W / {relationship.GetLossesFor(PlayerDataManager.Instance.PlayerId)}L");

        return true;
    }

    public bool TryPrepareChallenge(
        string opponentPlayerId,
        string matchReason,
        out string message)
    {
        if (PlayerDataManager.Instance == null)
        {
            message = "Current player account is unavailable.";
            return false;
        }

        FriendRelationshipData relationship =
            GetRelationshipWith(opponentPlayerId);

        if (relationship == null)
        {
            message = "A challenge can only be prepared for a friend.";

            return false;
        }

        PlayerProfileData opponent =
            GetProfileForPlayer(opponentPlayerId);

        if (opponent == null)
        {
            message = "The friend's account profile could not be found.";

            return false;
        }

        string cleanedReason =matchReason?.Trim() ?? string.Empty;

        PreparedChallenge = new MatchSetupData(PlayerDataManager.Instance.PlayerId, opponentPlayerId,cleanedReason);

        message = $"Challenge prepared for {opponent.DisplayName}.";

        return true;
    }

    public void ClearPreparedChallenge()
    {
        PreparedChallenge = null;
    }


    // Temporary testing tools

    [ContextMenu("Debug/Add Demo Friend")]
    private void DebugAddDemoFriend()
    {
        if (TryAddFriend(demoFriendPlayerId, out string message))
        {
            Debug.Log(message);
        }
        else
        {
            Debug.LogWarning(message);
        }
    }

    [ContextMenu("Debug/Win Against Demo Friend")]
    private void DebugWinAgainstDemoFriend()
    {
        RecordMatchResult(demoFriendPlayerId, true);
    }

    [ContextMenu("Debug/Lose Against Demo Friend")]
    private void DebugLoseAgainstDemoFriend()
    {
        RecordMatchResult(demoFriendPlayerId,false);
    }
}
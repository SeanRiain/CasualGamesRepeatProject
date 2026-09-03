using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Model;
using Newtonsoft.Json.Linq;

namespace PongBackend;

public class MatchSettlementModule
{
    private const string AccountKey = "player_account_v1";
    private const string RelationshipKey = "relationship_v1";
    private const int AccountSchemaVersion = 2;
    private const int RelationshipSchemaVersion = 1;
    private const int WinReward = 100;
    private const int LossReward = 50;

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<MatchSettlementModule> logger;

    public MatchSettlementModule(ILogger<MatchSettlementModule> logger)
    {
        this.logger = logger;
    }

    [CloudCodeFunction("SettleMatch")]
    public async Task<string> SettleMatch(
      IExecutionContext context,
      IGameApiClient gameApiClient,
      string sessionId,
      int matchNumber,
      string winnerPlayerId)
    {
        const int maximumConflictAttempts = 3;

        for (
            int attempt = 1;
            attempt <= maximumConflictAttempts;
            attempt++)
        {
            try
            {
                return await SettleMatchOnce(
                    context,
                    gameApiClient,
                    sessionId,
                    matchNumber,
                    winnerPlayerId);
            }
            catch (ApiException exception)
                when (
                    exception.Response.StatusCode ==
                    HttpStatusCode.Conflict)
            {
                logger.LogWarning(
                    "Cloud Save write-lock conflict " +
                    "while settling session {SessionId}, " +
                    "match {MatchNumber}. " +
                    "Attempt {Attempt}/{MaximumAttempts}.",
                    sessionId,
                    matchNumber,
                    attempt,
                    maximumConflictAttempts);

                if (attempt ==
                    maximumConflictAttempts)
                {
                    throw;
                }

                await Task.Delay(
                    100 * attempt);
            }
        }

        throw new InvalidOperationException(
            "Settlement conflict retry loop " +
            "ended unexpectedly.");
    }


    private async Task<string> SettleMatchOnce(IExecutionContext context, IGameApiClient gameApiClient, string sessionId, int matchNumber, string winnerPlayerId)
    {
        if (string.IsNullOrWhiteSpace(context.PlayerId))
        {
            throw new InvalidOperationException("The Cloud Code caller has no Player ID.");
        }

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("A session ID is required.");
        }

        if (matchNumber <= 0)
        {
            throw new ArgumentException("Match number must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(winnerPlayerId))
        {
            throw new ArgumentException("A winner Player ID is required.");
        }

        // Retrieve the actual Lobby/MPS participants
        // instead of trusting an opponent ID from the client.
        var lobbyResponse = await gameApiClient.Lobby.GetLobbyAsync(
            context,
            context.ServiceToken,
            sessionId,
            "cloud-code");

        var lobby = lobbyResponse.Data;

        if (!string.Equals(lobby.HostId, context.PlayerId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Only the active session host may settle this match.");
        }

        List<string> playerIds = lobby.Players
            .Select(player => player.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (playerIds.Count != 2)
        {
            throw new InvalidOperationException($"Expected exactly two session players, but found {playerIds.Count}.");
        }

        if (!playerIds.Contains(winnerPlayerId, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("The reported winner is not a participant in this session.");
        }

        string loserPlayerId = playerIds.First(id => !string.Equals(id, winnerPlayerId, StringComparison.Ordinal));

        string settlementId = $"{sessionId}:{matchNumber}";

        LoadedRelationship relationship = await LoadRelationshipAsync(
            context,
            gameApiClient,
            playerIds[0],
            playerIds[1]);

        bool relationshipAlreadyProcessed = relationship.Data.processedSettlementIds.Contains(settlementId, StringComparer.Ordinal);

        // Enforce sequential match numbering within
        // each Relay/MPS session.
        if (!relationshipAlreadyProcessed)
        {
            if (string.Equals(relationship.Data.lastSessionId, sessionId, StringComparison.Ordinal))
            {
                int expectedMatchNumber = relationship.Data.lastMatchNumber + 1;

                if (matchNumber != expectedMatchNumber)
                {
                    throw new InvalidOperationException($"Expected match {expectedMatchNumber}, received {matchNumber}.");
                }
            }
            else if (matchNumber != 1)
            {
                throw new InvalidOperationException("The first settlement in a new session must use match number 1.");
            }
        }

        LoadedAccount winnerAccount = await LoadPlayerAccountAsync(context, gameApiClient, winnerPlayerId);
        LoadedAccount loserAccount = await LoadPlayerAccountAsync(context, gameApiClient, loserPlayerId);

        bool winnerAlreadyProcessed = winnerAccount.Data.processedSettlementIds.Contains(settlementId, StringComparer.Ordinal);
        bool loserAlreadyProcessed = loserAccount.Data.processedSettlementIds.Contains(settlementId, StringComparer.Ordinal);

        bool alreadySettled = winnerAlreadyProcessed && loserAlreadyProcessed && relationshipAlreadyProcessed;

        if (!winnerAlreadyProcessed)
        {
            ApplyAccountResult(winnerAccount.Data, settlementId, won: true);

            await SavePlayerAccountAsync(context, gameApiClient, winnerPlayerId, winnerAccount);
        }

        if (!loserAlreadyProcessed)
        {
            ApplyAccountResult(loserAccount.Data, settlementId, won: false);

            await SavePlayerAccountAsync(context, gameApiClient, loserPlayerId, loserAccount);
        }

        if (!relationshipAlreadyProcessed)
        {
            ApplyRelationshipResult(relationship.Data, settlementId, sessionId, matchNumber, winnerPlayerId);

            await SaveRelationshipAsync(context, gameApiClient, relationship);
        }

        logger.LogInformation(
            "Settled {SettlementId}. Winner: {Winner}. Loser: {Loser}. Already settled: {AlreadySettled}.",
            settlementId,
            winnerPlayerId,
            loserPlayerId,
            alreadySettled);

        SettlementResponse response = new SettlementResponse
        {
            success = true,
            alreadySettled = alreadySettled,
            settlementId = settlementId,
            relationship = relationship.Data
        };

        return JsonSerializer.Serialize(response, JsonOptions);
    }

    private async Task<LoadedAccount> LoadPlayerAccountAsync(IExecutionContext context, IGameApiClient gameApiClient, string playerId)
    {
        var response = await gameApiClient.CloudSaveData.GetItemsAsync(
            context,
            context.ServiceToken,
            context.ProjectId,
            playerId,
            new List<string>
            {
                AccountKey
            });

        Item item = response.Data.Results.FirstOrDefault(result => result.Key == AccountKey);

        if (item == null)
        {
            throw new InvalidOperationException($"Player {playerId} has no {AccountKey} Cloud Save record.");
        }

        PlayerAccountData data = DeserializeValue<PlayerAccountData>(item.Value);

        NormalizeAccount(data);

        return new LoadedAccount(data, item.WriteLock);
    }

    private async Task SavePlayerAccountAsync(IExecutionContext context, IGameApiClient gameApiClient, string playerId, LoadedAccount account)
    {
        await gameApiClient.CloudSaveData.SetItemAsync(
            context,
            context.ServiceToken,
            context.ProjectId,
            playerId,
            new SetItemBody(
                AccountKey,
                account.Data,
                account.WriteLock));
    }

    private async Task<LoadedRelationship> LoadRelationshipAsync(IExecutionContext context, IGameApiClient gameApiClient, string firstPlayerId, string secondPlayerId)
    {
        CanonicalizePlayers(firstPlayerId, secondPlayerId, out string playerAId, out string playerBId);

        string customId = BuildRelationshipCustomId(playerAId, playerBId);

        try
        {
            var response = await gameApiClient.CloudSaveData.GetPrivateCustomItemsAsync(
                context,
                context.ServiceToken,
                context.ProjectId,
                customId);

            Item item = response.Data.Results.FirstOrDefault(result => result.Key == RelationshipKey);

            if (item == null)
            {
                return new LoadedRelationship(
                    CreateRelationship(
                        customId,
                        playerAId,
                        playerBId),
                    null,
                    customId);
            }

            RelationshipData data = DeserializeValue<RelationshipData>(item.Value);

            NormalizeRelationship(data, playerAId, playerBId);

            return new LoadedRelationship(data, item.WriteLock, customId);
        }
        catch (ApiException exception) when (exception.Response.StatusCode == HttpStatusCode.NotFound)
        {
            return new LoadedRelationship(
                CreateRelationship(customId, playerAId, playerBId),
                null,
                customId);
        }
    }

    private async Task SaveRelationshipAsync(IExecutionContext context, IGameApiClient gameApiClient, LoadedRelationship relationship)
    {
        await gameApiClient.CloudSaveData.SetPrivateCustomItemAsync(
            context,
            context.ServiceToken,
            context.ProjectId,
            relationship.CustomId,
            new SetItemBody(
                RelationshipKey,
                relationship.Data,
                relationship.WriteLock));
    }

    private static void ApplyAccountResult(PlayerAccountData account, string settlementId, bool won)
    {
        NormalizeAccount(account);

        if (account.processedSettlementIds.Contains(settlementId, StringComparer.Ordinal))
        {
            return;
        }

        if (won)
        {
            account.totalWins++;
            account.softCurrency += WinReward;
        }
        else
        {
            account.totalLosses++;
            account.softCurrency += LossReward;
        }

        account.processedSettlementIds.Add(settlementId);
        account.schemaVersion = AccountSchemaVersion;
    }

    private static void ApplyRelationshipResult(RelationshipData relationship, string settlementId, string sessionId, int matchNumber, string winnerPlayerId)
    {
        if (relationship.processedSettlementIds.Contains(settlementId, StringComparer.Ordinal))
        {
            return;
        }

        if (string.Equals(winnerPlayerId, relationship.playerAId, StringComparison.Ordinal))
        {
            relationship.playerAWins++;
        }
        else if (string.Equals(winnerPlayerId, relationship.playerBId, StringComparison.Ordinal))
        {
            relationship.playerBWins++;
        }
        else
        {
            throw new InvalidOperationException("Winner does not belong to the relationship.");
        }

        relationship.matchesPlayed++;

        relationship.processedSettlementIds.Add(settlementId);

        relationship.lastSessionId = sessionId;
        relationship.lastMatchNumber = matchNumber;
    }

    private static RelationshipData CreateRelationship(string customId, string playerAId, string playerBId)
    {
        return new RelationshipData
        {
            schemaVersion = RelationshipSchemaVersion,
            playerAId = playerAId,
            playerBId = playerBId,
            playerAWins = 0,
            playerBWins = 0,
            matchesPlayed = 0,
            lastSessionId = string.Empty,
            lastMatchNumber = 0,
            processedSettlementIds = new List<string>()
        };
    }

    private static void NormalizeAccount(PlayerAccountData data)
    {
        data.schemaVersion = AccountSchemaVersion;
        data.displayName ??= "Player";
        data.softCurrency = Math.Max(0, data.softCurrency);
        data.totalWins = Math.Max(0, data.totalWins);
        data.totalLosses = Math.Max(0, data.totalLosses);
        data.ownedCosmeticIds ??= new List<string>();
        data.equippedCosmetics ??= new List<CloudEquippedCosmeticData>();
        data.processedSettlementIds ??= new List<string>();
    }

    private static void NormalizeRelationship(RelationshipData data, string expectedPlayerAId, string expectedPlayerBId)
    {
        if (!string.Equals(data.playerAId, expectedPlayerAId, StringComparison.Ordinal) ||
            !string.Equals(data.playerBId, expectedPlayerBId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Stored relationship players do not match its canonical key.");
        }

        data.schemaVersion = RelationshipSchemaVersion;
        data.playerAWins = Math.Max(0, data.playerAWins);
        data.playerBWins = Math.Max(0, data.playerBWins);
        data.matchesPlayed = Math.Max(0, data.matchesPlayed);
        data.lastSessionId ??= string.Empty;
        data.lastMatchNumber = Math.Max(0, data.lastMatchNumber);
        data.processedSettlementIds ??= new List<string>();
    }

    private static T DeserializeValue<T>(object value) where T : class
    {
        if (value == null)
        {
            throw new InvalidOperationException($"Cloud Save returned no value for {typeof(T).Name}.");
        }

        if (value is T alreadyTyped)
        {
            return alreadyTyped;
        }

        string json;

        if (value is JToken token)
        {
            json = token.ToString(Newtonsoft.Json.Formatting.None);
        }
        else if (value is string text)
        {
            json = text;
        }
        else
        {
            json = Newtonsoft.Json.JsonConvert.SerializeObject(value);
        }

        T data = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);

        if (data == null)
        {
            throw new InvalidOperationException($"Could not deserialize {typeof(T).Name}.");
        }

        return data;
    }

    private static void CanonicalizePlayers(string firstPlayerId, string secondPlayerId, out string playerAId, out string playerBId)
    {
        if (string.CompareOrdinal(firstPlayerId, secondPlayerId) <= 0)
        {
            playerAId = firstPlayerId;
            playerBId = secondPlayerId;
        }
        else
        {
            playerAId = secondPlayerId;
            playerBId = firstPlayerId;
        }
    }

    private static string BuildRelationshipCustomId(string playerAId, string playerBId)
    {
        string canonicalPair = $"{playerAId}::{playerBId}";

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPair));

        string hash = Convert.ToHexString(digest).ToLowerInvariant();

        return "rel_" + hash.Substring(0, 40);
    }
}

public class CloudEquippedCosmeticData
{
    public int category { get; set; }

    public string cosmeticId { get; set; } = string.Empty;
}

public class PlayerAccountData
{
    public int schemaVersion { get; set; }

    public string displayName { get; set; } = "Player";

    public int softCurrency { get; set; }

    public int totalWins { get; set; }

    public int totalLosses { get; set; }

    public List<string> ownedCosmeticIds { get; set; } = new();

    public List<CloudEquippedCosmeticData> equippedCosmetics { get; set; } = new();

    public List<string> processedSettlementIds { get; set; } = new();
}

public class RelationshipData
{
    public int schemaVersion { get; set; }

    public string playerAId { get; set; } = string.Empty;

    public string playerBId { get; set; } = string.Empty;

    public int playerAWins { get; set; }

    public int playerBWins { get; set; }

    public int matchesPlayed { get; set; }

    public string lastSessionId { get; set; } = string.Empty;

    public int lastMatchNumber { get; set; }

    public List<string> processedSettlementIds { get; set; } = new();
}

public class SettlementResponse
{
    public bool success { get; set; }

    public bool alreadySettled { get; set; }

    public string settlementId { get; set; } = string.Empty;

    public RelationshipData relationship { get; set; }
}

internal sealed class LoadedAccount
{
    public PlayerAccountData Data { get; }

    public string WriteLock { get; }

    public LoadedAccount(PlayerAccountData data, string writeLock)
    {
        Data = data;
        WriteLock = writeLock;
    }
}

internal sealed class LoadedRelationship
{
    public RelationshipData Data { get; }

    public string WriteLock { get; }

    public string CustomId { get; }

    public LoadedRelationship(RelationshipData data, string writeLock, string customId = null)
    {
        Data = data;
        WriteLock = writeLock;
        CustomId = customId ?? string.Empty;
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.CloudCode;
using Unity.Services.CloudCode.GeneratedBindings;
using UnityEngine;

public class NetworkMatchSettlementController : NetworkBehaviour
{
    [Header("Match")]
    [SerializeField] private MatchManager matchManager;
    [SerializeField] private MatchSessionManager matchSessionManager;

    [Header("Retry")]
    [SerializeField]
    [Min(1)]
    private int maximumAttempts = 3;

    [SerializeField]
    [Min(0.1f)]
    private float retryDelaySeconds = 1f;

    private MatchSettlementModuleBindings backend;

    private readonly HashSet<int> attemptedMatchNumbers = new HashSet<int>();

    private void Awake()
    {
        backend = new MatchSettlementModuleBindings(CloudCodeService.Instance);
    }

    private void OnEnable()
    {
        if (matchManager != null)
        {
            matchManager.MatchEnded += HandleMatchEnded;
        }
    }

    private void OnDisable()
    {
        if (matchManager != null)
        {
            matchManager.MatchEnded -= HandleMatchEnded;
        }
    }

    private async void HandleMatchEnded(PlayerSide winner)
    {
        if (!IsSpawned || !IsServer)
        {
            return;
        }

        if (matchManager == null || matchSessionManager == null)
        {
            Debug.LogError("[Settlement] Required match references are missing.");
            return;
        }

        int matchNumber = matchManager.CurrentMatchNumber;

        if (matchNumber <= 0)
        {
            Debug.LogError("[Settlement] Match has no valid statistical sequence number.");
            matchSessionManager.ServerCompleteSettlement(false);
            return;
        }

        if (!attemptedMatchNumbers.Add(matchNumber))
        {
            Debug.LogWarning($"[Settlement] Duplicate local settlement request ignored for match {matchNumber}.");
            return;
        }

        bool succeeded = await TrySettleMatchAsync(winner, matchNumber);

        matchSessionManager.ServerCompleteSettlement(succeeded);
    }

    private async Task<bool> TrySettleMatchAsync(PlayerSide winner, int matchNumber)
    {
        NetworkSessionController session = NetworkSessionController.Instance;

        if (session == null)
        {
            Debug.LogError("[Settlement] No NetworkSessionController exists.");
            return false;
        }

        if (!await session.EnsureServicesReadyAsync())
        {
            Debug.LogError("[Settlement] UGS is unavailable.");
            return false;
        }

        string sessionId = session.CurrentSessionId;

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            Debug.LogError("[Settlement] No active MPS Session ID exists.");
            return false;
        }

        string hostPlayerId = session.AuthenticatedPlayerId;

        if (!session.TryGetOnlineOpponentPlayerId(out string opponentPlayerId))
        {
            Debug.LogError("[Settlement] Could not resolve the authenticated opponent.");
            return false;
        }

        // NetworkPaddleCoordinator assigns:
        // Host = Left
        // remote Client = Right.
        string winnerPlayerId = winner == PlayerSide.Left ? hostPlayerId : opponentPlayerId;

        for (int attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            try
            {
                Debug.Log($"[Settlement] Calling Cloud Code. Session: {sessionId}. Match: {matchNumber}. Winner: {winnerPlayerId}. Attempt: {attempt}.");

                string responseJson = await backend.SettleMatch(sessionId, matchNumber, winnerPlayerId);

                MatchSettlementResponseData response = JsonUtility.FromJson<MatchSettlementResponseData>(responseJson);

                if (response == null || !response.success || response.relationship == null)
                {
                    throw new InvalidOperationException("Cloud Code returned an invalid settlement response.");
                }

                MatchRelationshipSnapshotData r = response.relationship;

                ApplySettlementResultRpc(
                    new FixedString64Bytes(r.playerAId),
                    new FixedString64Bytes(r.playerBId),
                    r.playerAWins,
                    r.playerBWins,
                    r.matchesPlayed);

                Debug.Log($"[Settlement] Backend settlement completed. ID: {response.settlementId}. Already settled: {response.alreadySettled}.");

                return true;
            }
            catch (CloudCodeException exception)
            {
                Debug.LogWarning($"[Settlement] Cloud Code attempt {attempt} failed.");
                Debug.LogException(exception);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Settlement] Settlement attempt {attempt} failed.");
                Debug.LogException(exception);
            }

            if (attempt < maximumAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
            }
        }

        Debug.LogError("[Settlement] Match result could not be persisted after all attempts.");

        return false;
    }

    [Rpc(SendTo.Everyone)]
    private void ApplySettlementResultRpc(FixedString64Bytes playerAId, FixedString64Bytes playerBId, int playerAWins, int playerBWins, int matchesPlayed)
    {
        string playerA = playerAId.ToString();
        string playerB = playerBId.ToString();

        if (FriendsManager.Instance != null)
        {
            FriendsManager.Instance.ApplyBackendRelationshipSnapshot(playerA, playerB, playerAWins, playerBWins, matchesPlayed);
        }

        _ = ReloadLocalAccountAsync();
    }

    private async Task ReloadLocalAccountAsync()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("[Settlement] No local PlayerDataManager exists.");
            return;
        }

        PlayerCloudPersistence persistence = PlayerDataManager.Instance.GetComponent<PlayerCloudPersistence>();

        if (persistence == null)
        {
            Debug.LogError("[Settlement] No PlayerCloudPersistence exists.");
            return;
        }

        bool reloaded = await persistence.ReloadCurrentPlayerFromCloudAsync();

        if (!reloaded)
        {
            Debug.LogError("[Settlement] Backend result was saved, but this client's local account refresh failed.");
        }
    }
}
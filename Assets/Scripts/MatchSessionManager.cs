using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum MatchSessionState
{
    MatchInProgress,
    SettlingResult,
    AwaitingRematchDecision,
    SettlementFailed,
    Closing
}

public class MatchSessionManager : NetworkBehaviour
{
    [Header("Match")]
    [SerializeField] private MatchManager matchManager;
    [SerializeField] private NetworkPaddleCoordinator networkPaddleCoordinator;

    [Header("Match Result UI")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text rematchStatusText;
    [SerializeField] private Button rematchButton;

    [Header("Navigation")]
    [SerializeField] private string menusSceneName = "Menus";

    public MatchSessionState State { get; private set; } = MatchSessionState.MatchInProgress;

    // Retained only for the direct, non-networked local test path.
    private bool localHostRematchConsent;
    private bool localOpponentRematchConsent;

    private NetworkVariable<MatchSessionState> networkState = new NetworkVariable<MatchSessionState>(
        MatchSessionState.MatchInProgress,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> leftRematchConsent = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> rightRematchConsent = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private void OnEnable()
    {
        if (matchManager != null)
        {
            matchManager.MatchEnded += HandleMatchEnded;
        }
    }

    private void Start()
    {
        if (!IsNetworkSessionActive())
        {
            ResetLocalRematchConsent();
            ApplyStatePresentation(MatchSessionState.MatchInProgress);
        }
    }

    private void OnDisable()
    {
        if (matchManager != null)
        {
            matchManager.MatchEnded -= HandleMatchEnded;
        }
    }

    public override void OnNetworkSpawn()
    {
        networkState.OnValueChanged += HandleNetworkStateChanged;
        leftRematchConsent.OnValueChanged += HandleNetworkConsentChanged;
        rightRematchConsent.OnValueChanged += HandleNetworkConsentChanged;

        if (IsServer)
        {
            leftRematchConsent.Value = false;
            rightRematchConsent.Value = false;
            networkState.Value = MatchSessionState.MatchInProgress;
        }

        ApplyStatePresentation(networkState.Value);
        RefreshNetworkRematchPresentation();
    }

    public override void OnNetworkDespawn()
    {
        networkState.OnValueChanged -= HandleNetworkStateChanged;
        leftRematchConsent.OnValueChanged -= HandleNetworkConsentChanged;
        rightRematchConsent.OnValueChanged -= HandleNetworkConsentChanged;
    }

    private void HandleMatchEnded(PlayerSide winner)
    {
        if (!IsNetworkSessionActive())
        {
            ResetLocalRematchConsent();
            ApplyStatePresentation(MatchSessionState.AwaitingRematchDecision);
            return;
        }

        // MatchManager publishes the winner to both peers,
        // but only the server decides canonical session state.
        if (!IsServer)
            return;

        leftRematchConsent.Value = false;
        rightRematchConsent.Value = false;

        SetServerState(MatchSessionState.SettlingResult);
    }

    public void RequestLocalRematch()
    {
        if (State != MatchSessionState.AwaitingRematchDecision)
        {
            return;
        }

        if (!IsNetworkSessionActive())
        {
            RequestLocalOfflineRematch();
            return;
        }

        if (!IsSpawned)
        {
            Debug.LogWarning("[MatchSession] Network session object is not spawned.");
            return;
        }

        RequestRematchRpc();
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void RequestRematchRpc(RpcParams rpcParams = default)
    {
        if (networkState.Value != MatchSessionState.AwaitingRematchDecision)
        {
            return;
        }

        ulong senderClientId = rpcParams.Receive.SenderClientId;

        if (!TryGetParticipantSide(senderClientId, out PlayerSide senderSide))
        {
            Debug.LogWarning($"[MatchSession] Rematch request rejected from non-participant Client {senderClientId}.");
            return;
        }

        if (senderSide == PlayerSide.Left)
        {
            leftRematchConsent.Value = true;
        }
        else
        {
            rightRematchConsent.Value = true;
        }

        Debug.Log($"[MatchSession] Rematch requested by {senderSide} / Client {senderClientId}.");

        RefreshNetworkRematchPresentation();

        if (BothNetworkPlayersWantRematch())
        {
            BeginNetworkRematch();
        }
    }

    private bool BothNetworkPlayersWantRematch()
    {
        return leftRematchConsent.Value && rightRematchConsent.Value;
    }

    private void BeginNetworkRematch()
    {
        if (!IsServer)
            return;

        Debug.Log("[MatchSession] Both players accepted rematch.");

        // MatchManager.ResetMatch() is already server-authoritative
        // from Milestone 4 and republishes score/countdown/winner.
        matchManager.ResetMatch();

        leftRematchConsent.Value = false;
        rightRematchConsent.Value = false;

        SetServerState(MatchSessionState.MatchInProgress);
    }

    public void ServerCompleteSettlement(bool succeeded)
    {
        if (!IsServer)
            return;

        if (networkState.Value != MatchSessionState.SettlingResult)
        {
            return;
        }

        SetServerState(succeeded ? MatchSessionState.AwaitingRematchDecision : MatchSessionState.SettlementFailed);
    }

    public void RequestLocalLeave()
    {
        if (State == MatchSessionState.Closing)
        {
            return;
        }

        if (State == MatchSessionState.SettlingResult)
        {
            SetRematchStatus("Saving match result...");

            return;
        }

        if (!IsNetworkSessionActive())
        {
            string currentPlayerId = null;

            if (PlayerDataManager.Instance != null)
            {
                currentPlayerId = PlayerDataManager.Instance.PlayerId;
            }

            HandlePlayerLeftLocally(currentPlayerId);
            return;
        }

        if (!IsSpawned)
        {
            Debug.LogWarning("[MatchSession] Network session object is not spawned.");
            return;
        }

        RequestLeaveRpc();
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void RequestLeaveRpc(RpcParams rpcParams = default)
    {
        if (networkState.Value == MatchSessionState.Closing)
        {
            return;
        }

        ulong senderClientId = rpcParams.Receive.SenderClientId;

        if (!TryGetParticipantSide(senderClientId, out PlayerSide senderSide))
        {
            Debug.LogWarning($"[MatchSession] Leave request rejected from non-participant Client {senderClientId}.");
            return;
        }

        Debug.Log($"[MatchSession] Leave requested by {senderSide} / Client {senderClientId}.");

        MatchSessionState previousState = networkState.Value;

        SetServerState(MatchSessionState.Closing);

        if (NetworkSessionController.Instance == null)
        {
            Debug.LogError("[MatchSession] No persistent NetworkSessionController exists.");
            SetServerState(previousState);
            return;
        }

        bool closeStarted = NetworkSessionController.Instance.TryCloseSessionToScene(menusSceneName);

        if (!closeStarted)
        {
            Debug.LogError("[MatchSession] Network session close could not be started.");
            SetServerState(previousState);
        }
    }

    private bool TryGetParticipantSide(ulong clientId, out PlayerSide side)
    {
        if (networkPaddleCoordinator == null)
        {
            side = default;

            Debug.LogError("[MatchSession] No NetworkPaddleCoordinator is assigned.");
            return false;
        }

        return networkPaddleCoordinator.TryGetPlayerSide(clientId, out side);
    }

    private void SetServerState(MatchSessionState newState)
    {
        if (!IsServer)
            return;

        networkState.Value = newState;

        // Ensure Host presentation updates immediately
        // even before the next synchronization pass.
        ApplyStatePresentation(newState);
    }

    private void HandleNetworkStateChanged(MatchSessionState previousValue, MatchSessionState newValue)
    {
        ApplyStatePresentation(newValue);
    }

    private void HandleNetworkConsentChanged(bool previousValue, bool newValue)
    {
        RefreshNetworkRematchPresentation();
    }

    private void ApplyStatePresentation(MatchSessionState newState)
    {
        State = newState;

        switch (newState)
        {
            case MatchSessionState.MatchInProgress:
                if (resultPanel != null)
                {
                    resultPanel.SetActive(false);
                }

                SetRematchStatus(string.Empty);

                if (rematchButton != null)
                {
                    rematchButton.interactable = true;
                }

                break;

            case MatchSessionState.SettlingResult:

                if (resultPanel != null)
                {
                    resultPanel.SetActive(true);
                }

                if (rematchButton != null)
                {
                    rematchButton.interactable =
                        false;
                }

                SetRematchStatus(
                    "Saving match result...");

                break;

            case MatchSessionState.AwaitingRematchDecision:
                if (resultPanel != null)
                {
                    resultPanel.SetActive(true);
                }

                if (IsNetworkSessionActive())
                {
                    RefreshNetworkRematchPresentation();
                }
                else
                {
                    if (rematchButton != null)
                    {
                        rematchButton.interactable = true;
                    }

                    SetRematchStatus(string.Empty);
                }

                break;

            case MatchSessionState.SettlementFailed:

                if (resultPanel != null)
                {
                    resultPanel.SetActive(true);
                }

                if (rematchButton != null)
                {
                    rematchButton.interactable =
                        false;
                }

                SetRematchStatus(
                    "Match result could not be saved. " +
                    "Leave the session and try again.");

                break;

            case MatchSessionState.Closing:
                if (rematchButton != null)
                {
                    rematchButton.interactable = false;
                }

                SetRematchStatus("Leaving session...");
                ClearLocalMatchContext();

                break;
        }
    }

    private void RefreshNetworkRematchPresentation()
    {
        if (!IsNetworkSessionActive())
            return;

        if (State != MatchSessionState.AwaitingRematchDecision)
        {
            return;
        }

        if (networkPaddleCoordinator == null || !networkPaddleCoordinator.TryGetLocalPlayerSide(out PlayerSide localSide))
        {
            if (rematchButton != null)
            {
                rematchButton.interactable = false;
            }

            SetRematchStatus("Waiting for player assignment...");
            return;
        }

        bool localConsent;
        bool opponentConsent;

        if (localSide == PlayerSide.Left)
        {
            localConsent = leftRematchConsent.Value;
            opponentConsent = rightRematchConsent.Value;
        }
        else
        {
            localConsent = rightRematchConsent.Value;
            opponentConsent = leftRematchConsent.Value;
        }

        if (rematchButton != null)
        {
            rematchButton.interactable = !localConsent;
        }

        if (localConsent && !opponentConsent)
        {
            SetRematchStatus("Rematch requested. Waiting for opponent...");
        }
        else if (!localConsent && opponentConsent)
        {
            SetRematchStatus("Opponent requested a rematch.");
        }
        else
        {
            SetRematchStatus(string.Empty);
        }
    }

    private bool IsNetworkSessionActive()
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        return networkManager != null && networkManager.IsListening;
    }

    private void ClearLocalMatchContext()
    {
        if (FriendsManager.Instance != null)
        {
            FriendsManager.Instance.ClearActiveMatchSetup();
        }
    }

    // -------------------------------------------------
    // Direct local/offline fallback
    // -------------------------------------------------

    private void RequestLocalOfflineRematch()
    {
        if (PlayerDataManager.Instance == null)
        {
            SetRematchStatus("Current player account is unavailable.");
            return;
        }

        RegisterOfflineRematchConsent(PlayerDataManager.Instance.PlayerId);
    }

    private bool RegisterOfflineRematchConsent(string playerId)
    {
        if (State != MatchSessionState.AwaitingRematchDecision)
        {
            return false;
        }

        if (FriendsManager.Instance == null || FriendsManager.Instance.ActiveMatchSetup == null)
        {
            SetRematchStatus("No active two-player match exists.");
            return false;
        }

        MatchSetupData setup = FriendsManager.Instance.ActiveMatchSetup;

        if (!setup.ContainsPlayer(playerId))
        {
            return false;
        }

        if (playerId == setup.HostPlayerId)
        {
            localHostRematchConsent = true;
        }
        else if (playerId == setup.OpponentPlayerId)
        {
            localOpponentRematchConsent = true;
        }

        if (localHostRematchConsent && localOpponentRematchConsent)
        {
            BeginOfflineRematch();
            return true;
        }

        bool isCurrentPlayer = PlayerDataManager.Instance != null && playerId == PlayerDataManager.Instance.PlayerId;

        if (isCurrentPlayer)
        {
            if (rematchButton != null)
            {
                rematchButton.interactable = false;
            }

            SetRematchStatus("Rematch requested. Waiting for opponent...");
        }
        else
        {
            SetRematchStatus("Opponent requested a rematch.");
        }

        return true;
    }

    private void BeginOfflineRematch()
    {
        ResetLocalRematchConsent();
        ApplyStatePresentation(MatchSessionState.MatchInProgress);
        matchManager.ResetMatch();
    }

    private bool HandlePlayerLeftLocally(string leavingPlayerId)
    {
        if (State == MatchSessionState.Closing)
        {
            return false;
        }

        MatchSetupData setup = null;

        if (FriendsManager.Instance != null)
        {
            setup = FriendsManager.Instance.ActiveMatchSetup;
        }

        if (setup != null && !string.IsNullOrWhiteSpace(leavingPlayerId) && !setup.ContainsPlayer(leavingPlayerId))
        {
            return false;
        }

        ApplyStatePresentation(MatchSessionState.Closing);
        SceneManager.LoadScene(menusSceneName);

        return true;
    }

    private void ResetLocalRematchConsent()
    {
        localHostRematchConsent = false;
        localOpponentRematchConsent = false;
    }

    private void SetRematchStatus(string message)
    {
        if (rematchStatusText != null)
        {
            rematchStatusText.text = message;
        }
    }

    [ContextMenu("Debug/Simulate Other Player Rematch Consent")]
    private void DebugSimulateOtherPlayerRematchConsent()
    {
        if (IsNetworkSessionActive())
        {
            Debug.LogWarning("The simulated rematch action is only for direct local testing.");
            return;
        }

        if (!TryGetOtherPlayerId(out string otherPlayerId))
        {
            return;
        }

        RegisterOfflineRematchConsent(otherPlayerId);
    }

    [ContextMenu("Debug/Simulate Other Player Leave")]
    private void DebugSimulateOtherPlayerLeave()
    {
        if (IsNetworkSessionActive())
        {
            Debug.LogWarning("The simulated leave action is only for direct local testing.");
            return;
        }

        if (!TryGetOtherPlayerId(out string otherPlayerId))
        {
            return;
        }

        HandlePlayerLeftLocally(otherPlayerId);
    }

    private bool TryGetOtherPlayerId(out string otherPlayerId)
    {
        otherPlayerId = null;

        if (FriendsManager.Instance == null || FriendsManager.Instance.ActiveMatchSetup == null || PlayerDataManager.Instance == null)
        {
            return false;
        }

        otherPlayerId = FriendsManager.Instance.ActiveMatchSetup.GetOtherPlayerId(PlayerDataManager.Instance.PlayerId);

        return !string.IsNullOrWhiteSpace(otherPlayerId);
    }
}
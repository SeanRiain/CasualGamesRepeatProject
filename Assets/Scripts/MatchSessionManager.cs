using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum MatchSessionState
{
    MatchInProgress,
    AwaitingRematchDecision,
    Closing
}

public class MatchSessionManager : MonoBehaviour
{
    [Header("Match")]
    [SerializeField]
    private MatchManager matchManager;

    [Header("Match Result UI")]
    [SerializeField]
    private GameObject resultPanel;

    [SerializeField]
    private TMP_Text rematchStatusText;

    [SerializeField]
    private Button rematchButton;

    [Header("Navigation")]
    [SerializeField]
    private string menusSceneName = "Menus";

    public MatchSessionState State { get; private set; } = MatchSessionState.MatchInProgress;

    private bool hostRematchConsent;
    private bool opponentRematchConsent;

    private void OnEnable()
    {
        if (matchManager != null)
        {
            matchManager.MatchEnded += HandleMatchEnded;
        }
    }

    private void Start()
    {
        State = MatchSessionState.MatchInProgress;

        ResetRematchConsent();

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        SetRematchStatus(string.Empty);

        if (rematchButton != null)
        {
            rematchButton.interactable = true;
        }
    }

    private void OnDisable()
    {
        if (matchManager != null)
        {
            matchManager.MatchEnded -= HandleMatchEnded;
        }
    }

    private void HandleMatchEnded(PlayerSide winner)
    {
        ResetRematchConsent();

        State = MatchSessionState.AwaitingRematchDecision;

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        if (rematchButton != null)
        {
            rematchButton.interactable = true;
        }

        SetRematchStatus(string.Empty);
    }

    public void RequestLocalRematch()
    {
        if (State !=
            MatchSessionState.AwaitingRematchDecision)
        {
            return;
        }

        if (PlayerDataManager.Instance == null)
        {
            SetRematchStatus("Current player account is unavailable.");

            return;
        }

        RegisterRematchConsent(PlayerDataManager.Instance.PlayerId);
    }

    public bool RegisterRematchConsent(string playerId)
    {
        if (State != MatchSessionState.AwaitingRematchDecision)
        {
            Debug.LogWarning("Rematch consent cannot be recorded because the current match has not ended.");

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
            Debug.LogWarning("Rematch consent came from an account that is not part of this session.");

            return false;
        }

        if (playerId == setup.HostPlayerId)
        {
            hostRematchConsent = true;
        }
        else if (playerId == setup.OpponentPlayerId)
        {
            opponentRematchConsent = true;
        }

        if (BothPlayersWantRematch())
        {
            BeginRematch();

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

    private bool BothPlayersWantRematch()
    {
        return hostRematchConsent && opponentRematchConsent;
    }

    private void BeginRematch()
    {
        ResetRematchConsent();

        State = MatchSessionState.MatchInProgress;

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        SetRematchStatus(string.Empty);

        if (rematchButton != null)
        {
            rematchButton.interactable = true;
        }

        matchManager.ResetMatch();
    }

    public void RequestLocalLeave()
    {
        if (State == MatchSessionState.Closing)
            return;

        string currentPlayerId = null;

        if (PlayerDataManager.Instance != null)
        {
            currentPlayerId = PlayerDataManager.Instance.PlayerId;
        }

        HandlePlayerLeft(currentPlayerId);
    }

    public bool HandlePlayerLeft(string leavingPlayerId)
    {
        if (State == MatchSessionState.Closing)
            return false;

        MatchSetupData setup = null;

        if (FriendsManager.Instance != null)
        {
            setup = FriendsManager.Instance.ActiveMatchSetup;
        }

        if (setup != null && !string.IsNullOrWhiteSpace(leavingPlayerId) && !setup.ContainsPlayer(leavingPlayerId))
        {
            Debug.LogWarning("A leave request was received from an account that is not part of this session.");

            return false;
        }

        Debug.Log($"Player {leavingPlayerId ?? "unknown"} left the match session.");

        CloseSessionLocally();

        return true;
    }

    private void CloseSessionLocally()
    {
        State = MatchSessionState.Closing;

        if (FriendsManager.Instance != null)
        {
            FriendsManager.Instance.ClearActiveMatchSetup();
        }

        SceneManager.LoadScene(menusSceneName);
    }

    private void ResetRematchConsent()
    {
        hostRematchConsent = false;
        opponentRematchConsent = false;
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
        if (!TryGetOtherPlayerId(out string otherPlayerId))
        {
            Debug.LogWarning("No other player exists in the active session.");

            return;
        }

        RegisterRematchConsent(otherPlayerId);
    }

    [ContextMenu("Debug/Simulate Other Player Leave")]
    private void DebugSimulateOtherPlayerLeave()
    {
        if (!TryGetOtherPlayerId(out string otherPlayerId))
        {
            Debug.LogWarning("No other player exists in the active session.");

            return;
        }

        HandlePlayerLeft(otherPlayerId);
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
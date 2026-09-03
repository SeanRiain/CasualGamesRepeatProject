using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public enum PlayerSide
{
    Left,
    Right
}

public class MatchManager : NetworkBehaviour
{
    [Header("Match Rules")]
    public int winningScore = 3;

    [Header("Match UI")]
    public TMP_Text leftScoreText;
    public TMP_Text rightScoreText;
    public TMP_Text resultText;

    [SerializeField]
    private TMP_Text countdownText;

    [Header("Ball")]
    public Ball ball;

    [Header("Point Reset")]
    [SerializeField]
    private int scoreCountdownSeconds = 3;

    [Header("Currency Rewards")]
    public int normalWinReward = 100;
    public int normalLossReward = 50;

    [Header("Local Player")]
    public PlayerSide localPlayerSide = PlayerSide.Left;

    [Header("Network Match Startup")]
    [SerializeField]
    private NetworkPaddleCoordinator networkPaddleCoordinator;

    public event System.Action<PlayerSide> MatchEnded;

    private int leftScore = 0;
    private int rightScore = 0;

    private bool matchOver = false;
    private bool pointResetInProgress = false;

    private int currentMatchNumber;
    public int CurrentMatchNumber => currentMatchNumber;

    private Coroutine pointResetCoroutine;
    private Coroutine networkStartCoroutine;

    private NetworkVariable<int> networkLeftScore = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> networkRightScore = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> networkCountdown = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> networkWinnerSide = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Start()
    {
        if (!IsNetworkSessionActive())
        {
            ResetMatch();
        }
    }

    public override void OnNetworkSpawn()
    {
        networkLeftScore.OnValueChanged += HandleNetworkScoreChanged;

        networkRightScore.OnValueChanged += HandleNetworkScoreChanged;

        networkCountdown.OnValueChanged += HandleNetworkCountdownChanged;

        networkWinnerSide.OnValueChanged += HandleNetworkWinnerChanged;

        RefreshNetworkPresentation();

        if (IsServer)
        {
            networkStartCoroutine = StartCoroutine(StartNetworkMatchWhenReady());
        }
    }

    public override void OnNetworkDespawn()
    {
        networkLeftScore.OnValueChanged -= HandleNetworkScoreChanged;

        networkRightScore.OnValueChanged -= HandleNetworkScoreChanged;

        networkCountdown.OnValueChanged -= HandleNetworkCountdownChanged;

        networkWinnerSide.OnValueChanged -= HandleNetworkWinnerChanged;

        if (networkStartCoroutine != null)
        {
            StopCoroutine(networkStartCoroutine);
            networkStartCoroutine = null;
        }
    }

    private IEnumerator StartNetworkMatchWhenReady()
    {
        const float timeoutSeconds = 5f;

        if (networkPaddleCoordinator == null)
        {
            Debug.LogWarning("[MatchManager] No NetworkPaddleCoordinator is assigned. Starting the network match without waiting for paddle assignment.");

            ResetMatch();

            networkStartCoroutine = null;
            yield break;
        }

        float timeoutAt = Time.realtimeSinceStartup + timeoutSeconds;

        while (Time.realtimeSinceStartup < timeoutAt)
        {
            bool leftAssigned = networkPaddleCoordinator.LeftClientId.Value != ulong.MaxValue;

            bool rightAssigned = networkPaddleCoordinator.RightClientId.Value != ulong.MaxValue;

            if (leftAssigned && rightAssigned)
            {
                ResetMatch();

                networkStartCoroutine = null;
                yield break;
            }

            yield return null;
        }

        Debug.LogWarning("[MatchManager] Timed out waiting for network paddle assignment. Starting the match anyway.");

        ResetMatch();

        networkStartCoroutine = null;
    }

    public void AwardPoint(PlayerSide player)
    {
        if (!HasGameplayAuthority())
            return;

        if (matchOver || pointResetInProgress)
            return;

        if (player == PlayerSide.Left)
        {
            leftScore++;
        }
        else
        {
            rightScore++;
        }

        UpdateScoreDisplay();
        PublishNetworkScores();

        if (leftScore >= winningScore)
        {
            EndMatch(PlayerSide.Left);
            return;
        }

        if (rightScore >= winningScore)
        {
            EndMatch(PlayerSide.Right);
            return;
        }

        BeginPointReset(player);
    }

    private void BeginPointReset(PlayerSide scoringPlayer)
    {
        ball.ResetToCentre();

        pointResetInProgress = true;

        pointResetCoroutine = StartCoroutine(PointResetRoutine(scoringPlayer));
    }

    private IEnumerator PointResetRoutine(PlayerSide scoringPlayer)
    {
        for (int secondsRemaining = scoreCountdownSeconds; secondsRemaining > 0; secondsRemaining--)
        {
            SetCountdownText(secondsRemaining.ToString());

            PublishNetworkCountdown(secondsRemaining);

            yield return new WaitForSeconds(1f);
        }

        SetCountdownText(string.Empty);
        PublishNetworkCountdown(0);

        if (!matchOver)
        {
            ball.ServeTowards(scoringPlayer);
        }

        pointResetInProgress = false;
        pointResetCoroutine = null;
    }

    private void EndMatch(PlayerSide winner)
    {
        if (!HasGameplayAuthority())
            return;

        matchOver = true;

        CancelPointReset();

        SetResultText(winner);

        ball.StopBall();

        PublishNetworkWinner(winner);

        if (!IsNetworkSessionActive())
        {
            RecordLocalMatchResult(winner);
        }

        MatchEnded?.Invoke(winner); RecordLocalMatchResult(winner);

        MatchEnded?.Invoke(winner);
    }

    private void RecordLocalMatchResult(PlayerSide winner)
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("Cannot record match result because no PlayerDataManager exists.");

            return;
        }

        bool localPlayerWon = winner == localPlayerSide;

        if (localPlayerWon)
        {
            PlayerDataManager.Instance.AddCurrency(normalWinReward);

            PlayerDataManager.Instance.RecordWin();
        }
        else
        {
            PlayerDataManager.Instance.AddCurrency(normalLossReward);

            PlayerDataManager.Instance.RecordLoss();
        }

        TryRecordPairSpecificResult(localPlayerWon);
    }

    private void TryRecordPairSpecificResult(bool localPlayerWon)
    {
        if (FriendsManager.Instance == null)
        {
            Debug.Log("No FriendsManager exists. Pair-specific result was not recorded.");

            return;
        }

        if (!FriendsManager.Instance.TryGetActiveFriendOpponentId(out string opponentPlayerId))
        {
            Debug.Log("This match does not have a valid friend opponent. Pair-specific statistics were not changed.");

            return;
        }

        bool pairResultRecorded = FriendsManager.Instance.RecordMatchResult(opponentPlayerId,localPlayerWon);

        if (!pairResultRecorded)
        {
            Debug.LogWarning("The overall match result was recorded, but the pair-specific result could not be recorded.");
        }
    }

    public void ResetMatch()
    {
        if (!HasGameplayAuthority())
        {
            Debug.LogWarning("[MatchManager] A non-authoritative client attempted to reset the match.");

            return;
        }

        if (IsNetworkSessionActive() && IsServer)
        {
            currentMatchNumber++;

            Debug.Log($"[MatchManager] Starting statistical match {currentMatchNumber}.");
        }

        CancelPointReset();

        leftScore = 0;
        rightScore = 0;

        matchOver = false;
        pointResetInProgress = false;

        resultText.text = string.Empty;

        SetCountdownText(string.Empty);

        UpdateScoreDisplay();

        PublishNetworkReset();

        ball.ResetToCentre();
        ball.ServeRandom();
    }

    private void CancelPointReset()
    {
        if (pointResetCoroutine != null)
        {
            StopCoroutine(pointResetCoroutine);

            pointResetCoroutine = null;
        }

        pointResetInProgress = false;

        SetCountdownText(string.Empty);
        PublishNetworkCountdown(0);
    }

    private void SetCountdownText(string value)
    {
        if (countdownText != null)
        {
            countdownText.text = value;
        }
    }

    private void SetResultText(PlayerSide winner)
    {
        if (resultText == null)
            return;

        if (winner == PlayerSide.Left)
        {
            resultText.text = "Left Player Wins";
        }
        else
        {
            resultText.text = "Right Player Wins";
        }
    }

    private void UpdateScoreDisplay()
    {
        leftScoreText.text = leftScore.ToString();

        rightScoreText.text = rightScore.ToString();
    }

    private bool IsNetworkSessionActive()
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        return networkManager != null && networkManager.IsListening;
    }

    private bool HasGameplayAuthority()
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null || !networkManager.IsListening)
        {
            return true;
        }

        return networkManager.IsServer;
    }

    private bool CanPublishNetworkState()
    {
        return IsNetworkSessionActive() && NetworkObject != null && NetworkObject.IsSpawned && IsServer;
    }

    private void PublishNetworkReset()
    {
        if (!CanPublishNetworkState())
            return;

        networkLeftScore.Value = 0;
        networkRightScore.Value = 0;
        networkCountdown.Value = 0;
        networkWinnerSide.Value = -1;
    }

    private void PublishNetworkScores()
    {
        if (!CanPublishNetworkState())
            return;

        networkLeftScore.Value = leftScore;

        networkRightScore.Value = rightScore;
    }

    private void PublishNetworkCountdown(int value)
    {
        if (!CanPublishNetworkState())
            return;

        networkCountdown.Value = Mathf.Max(0, value);
    }

    private void PublishNetworkWinner(PlayerSide winner)
    {
        if (!CanPublishNetworkState())
            return;

        networkWinnerSide.Value = (int)winner;
    }

    private void HandleNetworkScoreChanged(int previousValue, int newValue)
    {
        leftScoreText.text = networkLeftScore.Value.ToString();

        rightScoreText.text = networkRightScore.Value.ToString();
    }

    private void HandleNetworkCountdownChanged(int previousValue, int newValue)
    {
        if (newValue > 0)
        {
            SetCountdownText(newValue.ToString());
        }
        else
        {
            SetCountdownText(string.Empty);
        }
    }

    private void HandleNetworkWinnerChanged(int previousValue, int newValue)
    {
        if (newValue < 0)
        {
            if (resultText != null)
            {
                resultText.text = string.Empty;
            }

            return;
        }

        PlayerSide winner = (PlayerSide)newValue;

        SetResultText(winner);

        if (!IsServer)
        {
            MatchEnded?.Invoke(winner);
        }
    }

    private void RefreshNetworkPresentation()
    {
        leftScoreText.text = networkLeftScore.Value.ToString();

        rightScoreText.text = networkRightScore.Value.ToString();

        if (networkCountdown.Value > 0)
        {
            SetCountdownText(networkCountdown.Value.ToString());
        }
        else
        {
            SetCountdownText(string.Empty);
        }

        if (networkWinnerSide.Value >= 0)
        {
            SetResultText((PlayerSide) networkWinnerSide.Value);
        }
        else if (resultText != null)
        {
            resultText.text = string.Empty;
        }
    }
}
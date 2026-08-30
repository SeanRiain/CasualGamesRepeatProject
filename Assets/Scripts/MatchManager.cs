using System.Collections;
using TMPro;
using UnityEngine;

public enum PlayerSide
{
    Left,
    Right
}

public class MatchManager : MonoBehaviour
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

    public event System.Action<PlayerSide> MatchEnded;

    private int leftScore = 0;
    private int rightScore = 0;

    private bool matchOver = false;
    private bool pointResetInProgress = false;

    private Coroutine pointResetCoroutine;

    private void Start()
    {
        ResetMatch();
    }

    public void AwardPoint(PlayerSide player)
    {
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

            yield return new WaitForSeconds(1f);
        }

        SetCountdownText(string.Empty);

        if (!matchOver)
        {
            ball.ServeTowards(scoringPlayer);
        }

        pointResetInProgress = false;
        pointResetCoroutine = null;
    }

    private void EndMatch(PlayerSide winner)
    {
        matchOver = true;

        CancelPointReset();

        if (winner == PlayerSide.Left)
        {
            resultText.text = "Left Player Wins";
        }
        else
        {
            resultText.text = "Right Player Wins";
        }

        ball.StopBall();

        RecordLocalMatchResult(winner);

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

    private void TryRecordPairSpecificResult(
        bool localPlayerWon)
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
        CancelPointReset();

        leftScore = 0;
        rightScore = 0;

        matchOver = false;
        pointResetInProgress = false;

        resultText.text = string.Empty;

        SetCountdownText(string.Empty);

        UpdateScoreDisplay();

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
    }

    private void SetCountdownText(string value)
    {
        if (countdownText != null)
        {
            countdownText.text = value;
        }
    }

    private void UpdateScoreDisplay()
    {
        leftScoreText.text = leftScore.ToString();

        rightScoreText.text = rightScore.ToString();
    }
}
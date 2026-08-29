using UnityEngine;
using TMPro;
public enum PlayerSide
{
    Left,
    Right
}

public class MatchManager : MonoBehaviour
{
    public int winningScore = 3;

    public TMP_Text leftScoreText;
    public TMP_Text rightScoreText;
    public TMP_Text resultText;

    public Ball ball;

    private int leftScore = 0;
    private int rightScore = 0;
    private bool matchOver = false;

    [Header("Currency Rewards")]
    public int normalWinReward = 100;
    public int normalLossReward = 50;

    public PlayerSide localPlayerSide = PlayerSide.Left;

    private void Start()
    {
        ResetMatch();
    }

    public void AwardPoint(PlayerSide player)
    {
        if (matchOver)
            return;

        if (player == PlayerSide.Left)
            leftScore++;
        else
            rightScore++;

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

        ball.ResetBall();
    }

    private void EndMatch(PlayerSide winner)
    {
        matchOver = true;

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

        bool pairResultRecorded = FriendsManager.Instance.RecordMatchResult(opponentPlayerId, localPlayerWon);

        if (!pairResultRecorded)
        {
            Debug.LogWarning("The overall match result was recorded, but the pair-specific result could not be recorded.");
        }
    }

    public void ResetMatch()
    {
        leftScore = 0;
        rightScore = 0;
        matchOver = false;

        resultText.text = "";

        UpdateScoreDisplay();
        ball.ResetBall();
    }

    private void UpdateScoreDisplay()
    {
        leftScoreText.text = leftScore.ToString();
        rightScoreText.text = rightScore.ToString();
    }
}
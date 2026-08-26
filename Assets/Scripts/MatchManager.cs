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
            EndMatch("Left Player Wins");
            return;
        }

        if (rightScore >= winningScore)
        {
            EndMatch("Right Player Wins");
            return;
        }

        ball.ResetBall();
    }

    private void EndMatch(string result)
    {
        matchOver = true;

        resultText.text = result;
        ball.StopBall();
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
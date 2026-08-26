using UnityEngine;

public class Goal : MonoBehaviour
{
    public PlayerSide pointFor;
    public MatchManager matchManager;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Ball ball = collision.gameObject.GetComponent<Ball>();

        if (ball != null)
        {
            matchManager.AwardPoint(pointFor);
        }
    }
}
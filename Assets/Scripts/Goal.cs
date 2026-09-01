using Unity.Netcode;
using UnityEngine;

public class Goal : MonoBehaviour
{
    public PlayerSide pointFor;
    public MatchManager matchManager;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager != null && networkManager.IsListening && !networkManager.IsServer)
        {
            return;
        }

        Ball ball = collision.gameObject.GetComponent<Ball>();

        if (ball != null)
        {
            matchManager.AwardPoint(pointFor);
        }
    }
}
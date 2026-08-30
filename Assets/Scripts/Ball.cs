using UnityEngine;

public class Ball : MonoBehaviour
{
    public float movementSpeed;
    public Rigidbody2D body;

    [Header("Serve Direction")]
    [Range(0f, 1f)]
    [SerializeField]
    private float maximumVerticalServeComponent = 0.75f;

    public void ResetToCentre()
    {
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.position = Vector2.zero;
    }

    public void ServeRandom()
    {
        PlayerSide targetSide = Random.value < 0.5f ? PlayerSide.Left : PlayerSide.Right;

        ServeTowards(targetSide);
    }

    public void ServeTowards(PlayerSide targetSide)
    {
        float horizontalDirection = targetSide == PlayerSide.Left? -1f : 1f;

        float verticalDirection = Random.Range(-maximumVerticalServeComponent, maximumVerticalServeComponent);

        Vector2 serveDirection = new Vector2(horizontalDirection, verticalDirection).normalized;

        body.linearVelocity = serveDirection * movementSpeed;
    }

    public void StopBall()
    {
        ResetToCentre();
    }
}
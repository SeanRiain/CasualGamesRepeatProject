using UnityEngine;

public class Ball : MonoBehaviour
{
    public float movementSpeed;
    public Rigidbody2D body;

    public void ResetBall()
    {
        body.linearVelocity = Vector2.zero;
        body.position = Vector2.zero;

        PickDirectionAndMove();
    }

    public void StopBall()
    {
        body.linearVelocity = Vector2.zero;
        body.position = Vector2.zero;
    }

    private void PickDirectionAndMove()
    {
        float xDirection = Random.Range(-90, 90);
        float yDirection = Random.Range(-90, 90);

        body.linearVelocity =
            new Vector2(xDirection, yDirection).normalized * movementSpeed;
    }
}

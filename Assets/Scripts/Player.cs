using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public float movementSpeed = 10f;

    public float minY = -3.88f;
    public float maxY = 3.90f;

    public Rigidbody2D body;
    public Slider movementSlider;

    private float targetY;

    void Start()
    {
        // Place the slider handle at the paddle's starting position.
        movementSlider.normalizedValue =
            Mathf.InverseLerp(minY, maxY, body.position.y);

        targetY = body.position.y;
    }

    void Update()
    {
        // Convert the slider's 0-1 position into a position
        // between the bottom and top of the play area.
        targetY = Mathf.Lerp(
            minY,
            maxY,
            movementSlider.normalizedValue
        );
    }

    void FixedUpdate()
    {
        // Work out how far the paddle still has to travel.
        float distanceToTarget = targetY - body.position.y;

        // Calculate the velocity which would reach the target
        // during this physics step.
        float requiredVelocity =
            distanceToTarget / Time.fixedDeltaTime;

        // Do not allow that velocity to exceed the paddle's
        // maximum movement speed.
        float verticalVelocity = Mathf.Clamp(
            requiredVelocity,
            -movementSpeed,
            movementSpeed
        );

        body.linearVelocity =
            new Vector2(0f, verticalVelocity);
    }
}

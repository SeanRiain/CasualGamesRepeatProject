using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    public float movementSpeed;
    public Rigidbody2D body;

    void Start()
    {
        ResetPosition();
        PickDirectionAndMove();
    }

    void ResetPosition()
    {
        body.MovePosition(Vector2.zero);
    }

    void PickDirectionAndMove()
    {
        float xDirection = Random.Range(-90, 90);
        float yDirection = Random.Range(-90, 90);

        body.linearVelocity = new Vector2(xDirection, yDirection).normalized * movementSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //collision.gameObject is other object we collided with
        if(collision.gameObject.CompareTag("Goal"))
        {
            ResetPosition();
            PickDirectionAndMove();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CPU : MonoBehaviour
{
    public Rigidbody2D body;
    public GameObject Ball;

    void Update()
    {
        //move to the position
        //x = current X position of the CPU (horizontal)
        //y = Y position of the Ball in the scene
      body.MovePosition(new Vector2(
          transform.position.x,
          Ball.transform.position.y));  
    }
}

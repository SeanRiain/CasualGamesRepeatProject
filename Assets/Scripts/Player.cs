using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float movementSpeed;
    public float verticalInput;
    public Rigidbody2D body;

    void Start()
    {
        
    }

    void Update()
    {
        //get vertical input, return a value between -1 and 1
        verticalInput = Input.GetAxisRaw("Vertical");

        //No Key 100 * 0 = 0
        //A Key 100 * 1 = 100
        //S Key 100 * -1 = -100 
        body.linearVelocity = new Vector2(0, movementSpeed * verticalInput);
    }
}

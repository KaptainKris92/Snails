using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed;
    private Rigidbody2D body;
    private Sprite playerSprite;
    private Animator anim;
    private Vector3 directionVector;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    private void Update() // Runs on every frame of the game
    {
        // Horizontal input 
        float horizontalInput = Input.GetAxis("Horizontal");

        Debug.Log(horizontalInput);

        // Player movement based on horizontal input 
        body.velocity = new Vector2(Input.GetAxis("Horizontal") * speed, body.velocity.y);

        // Flip the player sprite when moving left and right
        if (horizontalInput > 0f)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
        else if (horizontalInput < 0f)
        {
            transform.localScale = Vector3.one;
        }

        //Set animator parameters
        anim.SetBool("Move", horizontalInput != 0);


    }


}

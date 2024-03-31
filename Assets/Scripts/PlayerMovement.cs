using System;
using System.Security.Cryptography;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float singleJumpHeight;
    [SerializeField] private float singleJumpDistance;
    private Rigidbody2D body;
    private Animator anim;
    private bool grounded;


    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    private void Update() // Runs on every frame of the game
    {
        // Horizontal input 
        float horizontalInput = Input.GetAxis("Horizontal");

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

        // Single jump
        if (Input.GetKey(KeyCode.Space) && grounded)
        {
            SingleJump();
        }

        //Set animator parameters
        anim.SetBool("Move", horizontalInput != 0);
        anim.SetBool("Grounded", grounded);


    }

    private void SingleJump()
    {
        body.velocity = new Vector2(body.velocity.x * singleJumpDistance, singleJumpHeight);
        anim.SetTrigger("SingleJump");
        grounded = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Ground")
        {
            grounded = true;
        }
    }


}

using System;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed;
    private Rigidbody2D body;
    private Animator anim;
    private bool grounded;
    private bool spaceHeld = false;
    [SerializeField] private float jumpHeight = 1;    

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
        if (horizontalInput < 0f)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
        else if (horizontalInput > 0f)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }

        // Charge jump
        if (Input.GetKey(KeyCode.Space) && grounded)
        {
            spaceHeld = true;
            if (jumpHeight <= 10){
                jumpHeight += 0.05f;
            }
        }

        // Release to jump
        if (Input.GetKeyUp("space"))
        {
            if (grounded){
                Jump();
            }
            spaceHeld = false;
        }        

        //Set animator parameters
        anim.SetBool("Move", horizontalInput != 0);
        anim.SetBool("Grounded", grounded);
        anim.SetBool("SpaceHeld", spaceHeld);
    
    }



    private void Jump()
    {
        body.velocity = new Vector2(body.velocity.x * 1, jumpHeight);
        grounded = false;
        
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Ground")
        {
            grounded = true;     
            jumpHeight = 1;               
        }
    }
}

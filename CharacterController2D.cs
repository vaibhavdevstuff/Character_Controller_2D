using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController2D : MonoBehaviour
{
    //Components
    private Rigidbody2D controllerRigidbody;
    private Animator anim;
    private PhysicsMaterial2D slipness;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 400f;
    [SerializeField] float jumpForce = 12f;
    [SerializeField] float moveSmoothness = 12f;

    [Header("Wall Jump & Slide")]
    [SerializeField] float wallSlidingSpeed = 5f;
    [SerializeField] float xWallForce = 15f;
    [SerializeField] float yWallForce = 20f;
    [SerializeField] float wallJumpTime = 0.05f;

    [Header("Gravity Modifier")]
    [SerializeField] float fallMultiplier = 4f;
    [SerializeField] float lowjumpMultiplier = 1.5f;

    [Header("Ground and Wall Detector")]
    [SerializeField] Transform groundDetector = null;
    [SerializeField] Transform wallDetectorUp = null;
    [SerializeField] Transform wallDetectorDown = null;
    [SerializeField] LayerMask groundLayer = default;
    [SerializeField] LayerMask wallLayer = default;
    [SerializeField] float radius = 0.25f;

    //Inputs
    private float moveInput;
    private bool jumpInput;
    
    //Bools
    private bool isJumping;
    [SerializeField]private bool isGrounded;
    private bool wallJump;
    [SerializeField]private bool wallDetected;

    //modifiers
    private float slideModifire;
    private float jumpCount;


    void Awake()
    {
        controllerRigidbody = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        slipness = GetComponent<CapsuleCollider2D>().sharedMaterial;
    }

    void Update()
    {
        //get the horizontal axis input
        moveInput = Input.GetAxisRaw("Horizontal");
        //get jump input into bool
        if (Input.GetButtonDown("Jump"))
            jumpInput = true;

        //WallJump Input
        if (wallDetected && jumpInput && moveInput != 0)
        {
            wallJump = true;
            Invoke("UpdateWallJump", wallJumpTime);
        }
    }

    void FixedUpdate()
    {
        UpdateVelocity();
        UpdateLookDirection();
        UpdateJump();
        UpdateWallJumpAndSlide();
        UpdateModifiers();
        UpdateAnimation();
        UpdateGravityandCollisions();

    }

    void UpdateVelocity()
    {
        Vector2 velocity = controllerRigidbody.velocity;
        
        if(moveInput != 0)
        {
            //provide fixed velocity to move
            velocity.x = moveInput * moveSpeed * Time.fixedDeltaTime;
        }
        else
        {
            //give smoothness to the jump velocity on X-axis
            if (isGrounded)
            {
                velocity.x = 0f;
            }
            else if (velocity.x > 1f)
            {
                velocity.x -= Time.deltaTime * slideModifire;  //slidemodifier changes according to player isGrounded or not
            }
            else if (velocity.x < -1f)
            {
                velocity.x += Time.deltaTime * slideModifire;
            }
                    

        }

        controllerRigidbody.velocity = velocity;
    }

    void UpdateLookDirection()
    {
        //change player look direction according to moveInput only on X-axis
        Vector2 m_scale = transform.localScale;

        if (moveInput < 0)
        {
            if (m_scale.x > 0)
                m_scale.x *= -1f;
        }
        else if (moveInput > 0)
        {
            if (m_scale.x < 0)
                m_scale.x *= -1f;
        }

        transform.localScale = m_scale;

    }

    void UpdateJump()
    {
        bool canJump = jumpInput && jumpCount != 0 && Input.GetAxisRaw("Vertical") >= 0;

        if (canJump)
        {
            isJumping = true;
            //to get right jumpForce during falling if jumpcount is left
            if(controllerRigidbody.velocity.y != 0)
                controllerRigidbody.velocity = new Vector2(controllerRigidbody.velocity.x, 0f);


            //add jump force to player
            controllerRigidbody.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);

            //stop jumping
            jumpInput = false;

            //reduce jump
            jumpCount -= 1f;
        }
        else
        {
            isJumping = false;
            jumpInput = false;
        }       

    }

    void UpdateWallJumpAndSlide()
    {
        #region Wall_Jump        

        if(wallJump)
        {
            controllerRigidbody.velocity = new Vector2(xWallForce * -moveInput, yWallForce);
        }

        #endregion

        #region Wall_Sliding

        if (wallDetected && !isGrounded && moveInput != 0 && !wallJump)
        {
            controllerRigidbody.velocity = new Vector2(controllerRigidbody.velocity.x, -Mathf.Clamp(controllerRigidbody.velocity.y, wallSlidingSpeed, float.MaxValue));
        }

        #endregion
    }

    void UpdateModifiers()
    {
        //falling smoothness
        if (isGrounded)
            slideModifire = moveSmoothness * 10f;
        else
            slideModifire = moveSmoothness;      

        //Jump Count
        if (isGrounded)
            jumpCount = 1f;
        if (wallDetected && moveInput != 0)
            jumpCount = 1f;

        //Physics Material 2D
        if(wallDetected || isGrounded)
        {
            slipness.friction = 0.4f;
        }
        else
        {
            slipness.friction = 0f;
        }

    }

    void UpdateAnimation()
    {
        if(isJumping)
        {
            anim.SetTrigger("Jumping");
        }
    }

    void UpdateWallJump()
    {
        wallJump = false;
    }

    void UpdateGravityandCollisions()
    {
        #region Gravity

        //gravity while jumping
        if (controllerRigidbody.velocity.y < 0)
            controllerRigidbody.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;

        //gravity while falling
        else if (controllerRigidbody.velocity.y > 0)
            controllerRigidbody.velocity += Vector2.up * Physics2D.gravity.y * (lowjumpMultiplier - 1) * Time.deltaTime;

        #endregion

        #region Collision

        //detecting Ground
        isGrounded = Physics2D.OverlapCircle(groundDetector.position, radius, groundLayer);

        //detecting Wall
        wallDetected = Physics2D.OverlapCircle(wallDetectorUp.position, radius, wallLayer) || Physics2D.OverlapCircle(wallDetectorDown.position, radius, wallLayer);

        #endregion

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        if (groundDetector != null)
            Gizmos.DrawWireSphere(groundDetector.position, radius);

        if (wallDetectorUp != null)
            Gizmos.DrawWireSphere(wallDetectorUp.position, radius);

        if (wallDetectorDown != null)
            Gizmos.DrawWireSphere(wallDetectorDown.position, radius);
    }






























}//class


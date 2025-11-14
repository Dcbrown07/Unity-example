using UnityEngine;

public class PlayerController2D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float acceleration = 50f;     // How fast you reach max speed
    public float deceleration = 70f;     // How fast you stop
    public float airAcceleration = 35f;  // Slower acceleration in air
    public float airDeceleration = 25f;  // Slower deceleration in air
    private float moveInput;
    private Animator animator;

    [Header("Jumping")]
    public float jumpForce = 14f;
    public float coyoteTime = 0.15f;     // Slightly longer for forgiveness
    public float jumpBufferTime = 0.2f;  // Longer buffer time
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;
    public float maxFallSpeed = -20f;
    public float jumpCutMultiplier = 0.4f; // More responsive jump cutting
    
    [Header("Jump Apex")]
    public float apexThreshold = 3f;
    public float apexMultiplier = 0.3f;  // Even floatier apex
    public float apexHangTime = 0.1f;    // Extra hang time at peak

    private float coyoteCounter;
    private float jumpBufferCounter;
    private bool hasJumped = false;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer = 1 << 3;
    private bool isGrounded;
    private bool wasGrounded;

    [Header("References")]
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    [Header("Particles")]
    public ParticleSystem jumpDust;
    public ParticleSystem landingDust;
    public ParticleSystem walkDust;

    [Header("Squash & Stretch")]
    public bool enableSquashStretch = true;
    public float scaleXNormal = 1f;
    public float scaleYNormal = 1f;
    public float scaleXStretch = 1.1f;
    public float scaleYSquash = 0.9f;
    public float scaleSpeed = 12f;
    
    [Header("Arena Movement Feel")]
    public float landingBoostMultiplier = 1.1f; // Slight speed boost on landing
    public float turnAroundBoost = 1.2f;        // Speed boost when changing direction
    public float dodgeBoost = 1.5f;             // Extra speed for dodging spells
    private float lastMoveDirection = 0f;
    private float landingBoostTimer = 0f;
    private float dodgeBoostTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        wasGrounded = true;
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // Flip sprite
        if (moveInput != 0)
            sr.flipX = moveInput < 0;

        // Ground check
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Coyote time and jump reset
        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
            hasJumped = false;
            
            if (!wasGrounded) // just landed
            {
                landingBoostTimer = 0.3f; // Give landing boost for brief period
                
                if (landingDust != null)
                    landingDust.Play();
            }
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        // Timers
        if (landingBoostTimer > 0)
            landingBoostTimer -= Time.deltaTime;
        if (dodgeBoostTimer > 0)
            dodgeBoostTimer -= Time.deltaTime;

        wasGrounded = isGrounded;

        // Jump buffer
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        // Jump
        if (jumpBufferCounter > 0f && coyoteCounter > 0f && !hasJumped)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            hasJumped = true;

            if (jumpDust != null)
                jumpDust.Play();
        }

        // Variable jump height with better feel
        if ((Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W)) && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);

        // Dodge boost trigger (could be called from spell detection)
        if (Input.GetKeyDown(KeyCode.LeftShift) && Mathf.Abs(moveInput) > 0.1f)
        {
            TriggerDodgeBoost();
        }

        // Walking dust
        if (walkDust != null && isGrounded && Mathf.Abs(moveInput) > 0.1f)
        {
            if (!walkDust.isPlaying)
                walkDust.Play();
        }
        else if (walkDust != null && walkDust.isPlaying)
            walkDust.Stop();

        // Squash & stretch
        if (enableSquashStretch)
            AnimateSquashStretch();
        else
            transform.localScale = new Vector3(scaleXNormal, scaleYNormal, 1f);
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleGravity();
        
        // Animation
        if (Mathf.Abs(moveInput) > 0.1f && isGrounded) {
            animator.SetBool("isRunning", true);
        } else {
            animator.SetBool("isRunning", false);
        }
    }

    void HandleMovement()
    {
        float targetSpeed = moveInput * moveSpeed;
        
        // Apply movement boosts for arena feel
        if (landingBoostTimer > 0 && Mathf.Abs(moveInput) > 0.1f)
        {
            targetSpeed *= landingBoostMultiplier;
        }
        
        if (dodgeBoostTimer > 0 && Mathf.Abs(moveInput) > 0.1f)
        {
            targetSpeed *= dodgeBoost;
        }
        
        // Apply turn-around boost for snappy direction changes
        if (moveInput != 0 && Mathf.Sign(moveInput) != Mathf.Sign(lastMoveDirection) && Mathf.Abs(lastMoveDirection) > 0.1f)
        {
            targetSpeed *= turnAroundBoost;
        }
        
        lastMoveDirection = moveInput;
        
        // Choose acceleration/deceleration based on ground state
        float accelRate;
        if (Mathf.Abs(targetSpeed) > 0.01f)
        {
            accelRate = isGrounded ? acceleration : airAcceleration;
        }
        else
        {
            accelRate = isGrounded ? deceleration : airDeceleration;
        }
        
        // Use direct velocity setting for reliable movement (like your original script)
        rb.linearVelocity = new Vector2(targetSpeed, rb.linearVelocity.y);
        
        // But add some smoothing for feel when changing directions
        if (Mathf.Abs(moveInput) < 0.1f && isGrounded)
        {
            // Smooth deceleration when stopping
            float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, 0, deceleration * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);
        }
    }

    void HandleGravity()
    {
        // Jump apex handling for floaty combat feel
        bool isNearApex = Mathf.Abs(rb.linearVelocity.y) < apexThreshold && rb.linearVelocity.y > 0;
        
        if (rb.linearVelocity.y < 0)
        {
            // Falling - faster gravity
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !(Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W)))
        {
            if (isNearApex)
            {
                // Apex hang time - very reduced gravity for spell aiming
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (apexMultiplier - 1) * Time.fixedDeltaTime;
            }
            else
            {
                // Rising but not holding jump
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
            }
        }
        else if (rb.linearVelocity.y > 0)
        {
            if (isNearApex)
            {
                // Apex with jump held - maximum hang time for precise aiming
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (apexMultiplier * 0.2f - 1) * Time.fixedDeltaTime;
            }
        }

        // Clamp fall speed
        if (rb.linearVelocity.y < maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
    }

    // Public method that can be called when player needs to dodge spells
    public void TriggerDodgeBoost()
    {
        dodgeBoostTimer = 0.2f;
    }

    void AnimateSquashStretch()
    {
        Vector3 targetScale = new Vector3(scaleXNormal, scaleYNormal, 1f);

        if (!isGrounded) // In air
        {
            targetScale = new Vector3(scaleXStretch, scaleYSquash, 1f);
        }
        else if (Mathf.Abs(moveInput) > 0.1f) // Walking
        {
            targetScale = new Vector3(scaleXStretch, scaleYNormal, 1f);
        }

        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
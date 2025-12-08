using UnityEngine;
using System.Collections;

public class PlayerController2D : MonoBehaviour
{
    [Header("Movement - Hollow Knight Style")]
    [Range(4f, 12f)]
    public float moveSpeed = 8f;
    public float acceleration = 80f;
    public float deceleration = 100f;
    public float airAcceleration = 60f;
    public float airDeceleration = 40f;
    
    [Header("Dashing - Bullet Hell Precision")]
    public bool canDash = true;
    public float dashSpeed = 25f;
    public float dashDuration = 0.12f;
    public float dashCooldown = 0.4f;
    public int maxDashCharges = 2;
    public bool invincibleDuringDash = true;
    private int currentDashCharges;
    private float lastDashTime = -999f;
    private bool isDashing = false;
    private Vector2 dashDirection;
    
    [Header("Jumping - Tight & Responsive")]
    public float jumpForce = 16f;
    public float shortHopMultiplier = 0.5f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.15f;
    public float fallMultiplier = 3f;
    public float lowJumpMultiplier = 2.5f;
    public float maxFallSpeed = -25f;
    
    [Header("Air Control - Bullet Hell Precision")]
    public float airControlMultiplier = 0.85f;
    public bool allowAirTurnaround = true;
    public float airDragOnStop = 0.95f;
    
    [Header("Jump Apex - Hollow Knight Float")]
    public float apexThreshold = 2f;
    public float apexGravityMultiplier = 0.2f;
    public float apexHangTime = 0.15f;
    
    [Header("Wall Interaction")]
    public bool enableWallSlide = true;
    public float wallSlideSpeed = 2f;
    public float wallJumpForce = 18f;
    public Vector2 wallJumpAngle = new Vector2(1f, 1.5f);
    public LayerMask wallLayer;
    public Transform wallCheckLeft;
    public Transform wallCheckRight;
    public float wallCheckDistance = 0.3f;
    private bool isTouchingWallLeft;
    private bool isTouchingWallRight;
    private bool isWallSliding;
    private float wallJumpTime = 0f;
    private float wallJumpDuration = 0.15f;
    
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;
    private bool isGrounded;
    private bool wasGrounded;
    
    [Header("Slope Handling")]
    public bool enableSlopeHandling = true;
    public float slopeCheckDistance = 0.6f;
    public float maxSlopeAngle = 45f;
    [Tooltip("Extra acceleration multiplier on slopes (increase if stuck)")]
    public float slopeForceMultiplier = 3f;
    private float slopeAngle;
    private Vector2 slopeNormal;
    private bool isOnSlope;
    
    [Header("IMPORTANT: Add Physics Material!")]
    [Tooltip("Create Physics Material 2D with Friction=0, assign to Rigidbody2D")]
    public bool needsPhysicsMaterial = true;
    
    [Header("Fast Fall - Bullet Hell Dodge")]
    public bool enableFastFall = true;
    public float fastFallMultiplier = 2f;
    private bool isFastFalling = false;
    
    private float coyoteCounter;
    private float jumpBufferCounter;
    private bool hasJumped = false;
    private float moveInput;
    
    [Header("References")]
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator animator;
    private Collider2D playerCollider;
    
    [Header("Visual Effects")]
    public bool enableSquashStretch = true;
    public float scaleSpeed = 15f;
    public GameObject dashTrailPrefab;
    public GameObject landingDustPrefab;
    public GameObject jumpDustPrefab;
    public GameObject wallSlideDustPrefab;
    
    [Header("Audio")]
    public AudioClip jumpSound;
    public AudioClip dashSound;
    public AudioClip landSound;
    public AudioClip wallSlideSound;
    private AudioSource audioSource;
    
    [Header("Advanced Feel")]
    public bool enableInputBuffer = true;
    public float inputBufferTime = 0.1f;
    private float lastMoveInputTime = -999f;
    private float lastMoveInputDirection = 0f;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        playerCollider = GetComponent<Collider2D>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        currentDashCharges = maxDashCharges;
        wasGrounded = true;
        
        // Optimize physics
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }
    
    void Update()
    {
        CheckGrounded();
        CheckSlope();
        CheckWalls();
        HandleInput();
        HandleJump();
        HandleDash();
        HandleWallSlide();
        HandleFastFall();
        UpdateAnimations();
        
        // Recharge dash on ground
        if (isGrounded && currentDashCharges < maxDashCharges)
        {
            currentDashCharges = maxDashCharges;
        }
    }
    
    void FixedUpdate()
    {
        if (!isDashing)
        {
            HandleMovement();
            HandleGravity();
        }
        else
        {
            HandleDashMovement();
        }
    }
    
    void CheckGrounded()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        
        // Coyote time
        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
            hasJumped = false;
            
            // Landing
            if (!wasGrounded)
            {
                OnLanded();
            }
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }
    }
    
    void CheckWalls()
    {
        if (!enableWallSlide) return;
        
        isTouchingWallLeft = Physics2D.Raycast(wallCheckLeft.position, Vector2.left, wallCheckDistance, wallLayer);
        isTouchingWallRight = Physics2D.Raycast(wallCheckRight.position, Vector2.right, wallCheckDistance, wallLayer);
    }
    
    void CheckSlope()
    {
        if (!enableSlopeHandling || !isGrounded)
        {
            isOnSlope = false;
            slopeAngle = 0f;
            return;
        }
        
        // Cast ray down to detect slope
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, slopeCheckDistance, groundLayer);
        
        if (hit)
        {
            slopeNormal = hit.normal;
            slopeAngle = Vector2.Angle(slopeNormal, Vector2.up);
            
            isOnSlope = slopeAngle != 0f && slopeAngle <= maxSlopeAngle;
        }
        else
        {
            isOnSlope = false;
            slopeAngle = 0f;
        }
    }
    
    void HandleInput()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
        
        // Input buffering for precise control
        if (enableInputBuffer && Mathf.Abs(moveInput) > 0.1f)
        {
            lastMoveInputTime = Time.time;
            lastMoveInputDirection = moveInput;
        }
        
        // Sprite flipping
        if (Mathf.Abs(moveInput) > 0.1f && !isWallSliding && Time.time > wallJumpTime + wallJumpDuration)
        {
            sr.flipX = moveInput < 0;
        }
    }
    
    void HandleJump()
    {
        // Jump buffer
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
        
        // Wall jump
        if (jumpBufferCounter > 0f && isWallSliding)
        {
            WallJump();
            return;
        }
        
        // Normal jump
        if (jumpBufferCounter > 0f && coyoteCounter > 0f && !hasJumped)
        {
            Jump();
        }
        
        // Short hop (release early)
        if ((Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W)) && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * shortHopMultiplier);
        }
    }
    
    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
        hasJumped = true;
        isFastFalling = false;
        
        PlaySound(jumpSound);
        SpawnEffect(jumpDustPrefab, transform.position);
        StartCoroutine(JumpSquash());
    }
    
    void WallJump()
    {
        wallJumpTime = Time.time;
        jumpBufferCounter = 0f;
        hasJumped = true;
        isFastFalling = false;
        
        // Jump away from wall
        float jumpDirection = isTouchingWallRight ? -1f : 1f;
        Vector2 force = new Vector2(wallJumpAngle.x * jumpDirection, wallJumpAngle.y).normalized * wallJumpForce;
        
        rb.linearVelocity = force;
        
        PlaySound(jumpSound);
        SpawnEffect(jumpDustPrefab, transform.position);
        StartCoroutine(JumpSquash());
    }
    
    void HandleDash()
    {
        if (!canDash || isDashing) return;
        
        if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) && 
            currentDashCharges > 0 && Time.time >= lastDashTime + dashCooldown)
        {
            StartCoroutine(Dash());
        }
    }
    
    IEnumerator Dash()
    {
        isDashing = true;
        currentDashCharges--;
        lastDashTime = Time.time;
        isFastFalling = false;
        
        // Determine dash direction (8-directional)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        // Default to facing direction if no input
        if (Mathf.Abs(horizontal) < 0.1f && Mathf.Abs(vertical) < 0.1f)
        {
            horizontal = sr.flipX ? -1 : 1;
        }
        
        dashDirection = new Vector2(horizontal, vertical).normalized;
        if (dashDirection == Vector2.zero) dashDirection = Vector2.right;
        
        PlaySound(dashSound);
        
        // Invincibility - use layer check instead
        if (invincibleDuringDash && playerCollider != null)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) // Only ignore if layer exists
            {
                Physics2D.IgnoreLayerCollision(gameObject.layer, enemyLayer, true);
            }
        }
        
        // Dash trail
        StartCoroutine(DashTrail());
        
        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        isDashing = false;
        
        // Re-enable collisions
        if (invincibleDuringDash && playerCollider != null)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) // Only re-enable if layer exists
            {
                Physics2D.IgnoreLayerCollision(gameObject.layer, enemyLayer, false);
            }
        }
    }
    
    void HandleDashMovement()
    {
        rb.linearVelocity = dashDirection * dashSpeed;
    }
    
    void HandleWallSlide()
    {
        if (!enableWallSlide) return;
        
        bool shouldWallSlide = !isGrounded && 
                              (isTouchingWallLeft || isTouchingWallRight) && 
                              rb.linearVelocity.y < 0 &&
                              Time.time > wallJumpTime + wallJumpDuration;
        
        if (shouldWallSlide)
        {
            // Check if pushing into wall
            bool pushingIntoWall = (isTouchingWallLeft && moveInput < -0.1f) || 
                                   (isTouchingWallRight && moveInput > 0.1f);
            
            if (pushingIntoWall || Mathf.Abs(moveInput) < 0.1f)
            {
                isWallSliding = true;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed));
                
                // Face wall
                sr.flipX = isTouchingWallRight;
            }
            else
            {
                isWallSliding = false;
            }
        }
        else
        {
            isWallSliding = false;
        }
    }
    
    void HandleFastFall()
    {
        if (!enableFastFall || isGrounded || isDashing) return;
        
        // Press down to fast fall
        if (Input.GetKey(KeyCode.S) && rb.linearVelocity.y < 0)
        {
            isFastFalling = true;
        }
        
        if (isFastFalling && rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.down * fastFallMultiplier * Time.deltaTime * 60f;
        }
    }
    
    void HandleMovement()
    {
        if (isWallSliding) return;
        
        // Wall jump override
        if (Time.time < wallJumpTime + wallJumpDuration)
        {
            return;
        }
        
        float targetSpeed = moveInput * moveSpeed;
        
        // Air control adjustment
        if (!isGrounded)
        {
            targetSpeed *= airControlMultiplier;
        }
        
        // Choose acceleration based on state
        float accelRate;
        bool isAccelerating = Mathf.Abs(targetSpeed) > 0.01f;
        
        if (isGrounded)
        {
            accelRate = isAccelerating ? acceleration : deceleration;
            
            // Add extra force on slopes
            if (isOnSlope && isAccelerating && slopeAngle > 5f)
            {
                accelRate *= slopeForceMultiplier;
            }
        }
        else
        {
            accelRate = isAccelerating ? airAcceleration : airDeceleration;
            
            // Air drag when no input
            if (!isAccelerating && allowAirTurnaround)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x * airDragOnStop, rb.linearVelocity.y);
            }
        }
        
        // Smooth movement - keep it simple!
        float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);
    }
    
    void HandleGravity()
    {
        if (isWallSliding || isDashing) return;
        
        bool isNearApex = Mathf.Abs(rb.linearVelocity.y) < apexThreshold && !isGrounded;
        
        if (rb.linearVelocity.y < 0)
        {
            // Falling
            float gravityMult = isFastFalling ? fallMultiplier * 2f : fallMultiplier;
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (gravityMult - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0)
        {
            if (isNearApex && (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W)))
            {
                // Apex hang with jump held
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (apexGravityMultiplier - 1) * Time.fixedDeltaTime;
            }
            else if (!(Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W)))
            {
                // Released jump
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
            }
        }
        
        // Clamp fall speed
        if (rb.linearVelocity.y < maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
        }
    }
    
    void OnLanded()
    {
        isFastFalling = false;
        PlaySound(landSound);
        SpawnEffect(landingDustPrefab, groundCheck.position);
        StartCoroutine(LandSquash());
    }
    
    IEnumerator JumpSquash()
    {
        if (!enableSquashStretch) yield break;
        
        Vector3 targetScale = new Vector3(0.8f, 1.3f, 1f);
        float elapsed = 0f;
        float duration = 0.1f;
        
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    
    IEnumerator LandSquash()
    {
        if (!enableSquashStretch) yield break;
        
        Vector3 targetScale = new Vector3(1.3f, 0.7f, 1f);
        float elapsed = 0f;
        float duration = 0.1f;
        
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    
    IEnumerator DashTrail()
    {
        float trailDuration = dashDuration + 0.1f;
        float elapsed = 0f;
        
        while (elapsed < trailDuration)
        {
            if (sr != null)
            {
                GameObject trail = new GameObject("DashTrail");
                trail.transform.position = transform.position;
                trail.transform.localScale = transform.localScale;
                
                SpriteRenderer trailSr = trail.AddComponent<SpriteRenderer>();
                trailSr.sprite = sr.sprite;
                trailSr.color = new Color(0.5f, 1f, 1f, 0.5f);
                trailSr.sortingLayerName = sr.sortingLayerName;
                trailSr.sortingOrder = sr.sortingOrder - 1;
                trailSr.flipX = sr.flipX;
                
                StartCoroutine(FadeTrail(trailSr, 0.2f));
            }
            
            elapsed += 0.03f;
            yield return new WaitForSeconds(0.03f);
        }
    }
    
    IEnumerator FadeTrail(SpriteRenderer trailSr, float duration)
    {
        Color start = trailSr.color;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            trailSr.color = Color.Lerp(start, Color.clear, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Destroy(trailSr.gameObject);
    }
    
    void UpdateAnimations()
    {
        if (animator == null) return;
        
        // Only set parameters that exist - wrapped to prevent warnings
        try
        {
            if (animator.parameters.Length > 0)
            {
                animator.SetBool("isRunning", Mathf.Abs(moveInput) > 0.1f && isGrounded);
            }
        }
        catch
        {
            // Animator parameter doesn't exist, skip
        }
        
        // Squash stretch when not animated
        if (enableSquashStretch)
        {
            Vector3 targetScale = Vector3.one;
            
            if (isDashing)
            {
                targetScale = new Vector3(1.3f, 0.8f, 1f);
            }
            else if (!isGrounded && !isWallSliding)
            {
                if (rb.linearVelocity.y > 0)
                    targetScale = new Vector3(0.9f, 1.2f, 1f);
                else
                    targetScale = new Vector3(1.1f, 0.9f, 1f);
            }
            else if (Mathf.Abs(moveInput) > 0.1f)
            {
                targetScale = new Vector3(1.1f, 0.95f, 1f);
            }
            
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scaleSpeed * Time.deltaTime);
        }
    }
    
    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    void SpawnEffect(GameObject prefab, Vector3 position)
    {
        if (prefab != null)
        {
            Instantiate(prefab, position, Quaternion.identity);
        }
    }
    
    public bool IsDashing() => isDashing;
    public int GetDashCharges() => currentDashCharges;
    
    void OnDrawGizmosSelected()
    {
        // Ground check
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        
        // Slope check
        if (enableSlopeHandling && Application.isPlaying && isOnSlope)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, Vector2.down * slopeCheckDistance);
            
            // Draw slope normal
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, slopeNormal * 2f);
        }
        
        // Wall checks
        if (wallCheckLeft != null && enableWallSlide)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(wallCheckLeft.position, Vector2.left * wallCheckDistance);
        }
        
        if (wallCheckRight != null && enableWallSlide)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(wallCheckRight.position, Vector2.right * wallCheckDistance);
        }
    }
}
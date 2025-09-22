using UnityEngine;

public class PlayerController2D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    private float moveInput;

    [Header("Jumping")]
    public float jumpForce = 14f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;
    public float maxFallSpeed = -20f;

    private float coyoteCounter;
    private float jumpBufferCounter;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer; // Only detect ground layer
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
    public bool enableSquashStretch = true; // Toggle on/off
    public float scaleXNormal = 1f;
    public float scaleYNormal = 1f;
    public float scaleXStretch = 1.1f;
    public float scaleYSquash = 0.9f;
    public float scaleSpeed = 12f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        wasGrounded = true;
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // Flip sprite
        if (moveInput != 0)
            sr.flipX = moveInput < 0;

        // Ground check using LayerMask
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Coyote time
        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
            if (!wasGrounded) // just landed
            {
                if (landingDust != null)
                    landingDust.Play();
            }
        }
        else
            coyoteCounter -= Time.deltaTime;

        wasGrounded = isGrounded;

        // Jump buffer
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        // Jump
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;

            if (jumpDust != null)
                jumpDust.Play();
        }

        // Variable jump height
        if ((Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W)) && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);

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
        // Horizontal movement
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // Better jump physics
        if (rb.linearVelocity.y < 0)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        else if (rb.linearVelocity.y > 0 && !(Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W)))
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;

        // Clamp fall speed
        if (rb.linearVelocity.y < maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
    }

    void AnimateSquashStretch()
    {
        Vector3 targetScale = new Vector3(scaleXNormal, scaleYNormal, 1f);

        if (!isGrounded) // Jumping
            targetScale = new Vector3(scaleXStretch, scaleYSquash, 1f);
        else if (Mathf.Abs(moveInput) > 0.1f) // Walking
            targetScale = new Vector3(scaleXStretch, scaleYNormal, 1f);

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

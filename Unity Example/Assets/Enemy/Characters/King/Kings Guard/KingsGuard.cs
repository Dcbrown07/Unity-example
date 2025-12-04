using UnityEngine;

public class SimpleEnemyAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Animator animator;
    public Rigidbody2D rb;

    [Header("Movement")]
    [Tooltip("How fast the enemy moves")]
    public float moveSpeed = 3f;
    
    [Tooltip("How close to get before stopping")]
    public float stoppingDistance = 1.5f;
    
    [Tooltip("Distance to start attacking")]
    public float attackDistance = 2f;

    [Header("Ground Detection")]
    [Tooltip("Point to check for ground")]
    public Transform groundCheck;
    
    [Tooltip("Radius of ground check")]
    public float groundCheckRadius = 0.2f;
    
    [Tooltip("What counts as ground")]
    public LayerMask groundLayer;

    [Header("Detection")]
    [Tooltip("How far the enemy can see the player")]
    public float detectionRange = 10f;

    [Header("Visuals")]
    public SpriteRenderer spriteRenderer;
    public bool flipSpriteToFacePlayer = true;

    [Header("Debug")]
    public bool showGizmos = true;

    private bool isAttacking = false;
    private float distanceToPlayer;
    private bool isGrounded = false;

    void Start()
    {
        // Auto-find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("No player found! Assign player or tag player as 'Player'");
            }
        }

        // Auto-find rigidbody if not assigned
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        // Auto-find animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // Auto-find sprite renderer if not assigned
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Setup Rigidbody2D for proper ground walking
        if (rb != null)
        {
            rb.gravityScale = 1f; // Enable gravity
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Prevent rotation
        }
    }

    void Update()
    {
        if (player == null) return;

        // Check if on ground
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Check if player is in detection range
        if (distanceToPlayer <= detectionRange)
        {
            // Check if close enough to attack
            if (distanceToPlayer <= attackDistance)
            {
                Attack();
            }
            // Close enough to stop but not attack yet
            else if (distanceToPlayer <= stoppingDistance)
            {
                StopMoving();
            }
            // Chase player
            else
            {
                ChasePlayer();
            }
        }
        else
        {
            // Player too far, stop moving
            Idle();
        }

        // Flip sprite to face player
        if (flipSpriteToFacePlayer && spriteRenderer != null)
        {
            if (player.position.x < transform.position.x)
            {
                spriteRenderer.flipX = true;
            }
            else
            {
                spriteRenderer.flipX = false;
            }
        }
    }

    void ChasePlayer()
    {
        isAttacking = false;

        if (rb != null && isGrounded)
        {
            // Only move horizontally (X axis)
            float direction = Mathf.Sign(player.position.x - transform.position.x);
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        }

        // Set animator to walking
        if (animator != null)
        {
            animator.SetBool("isWalking", true);
            animator.SetBool("isAttacking", false);
        }
    }

    void StopMoving()
    {
        isAttacking = false;

        // Stop horizontal movement
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        // Set animator to idle
        if (animator != null)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isAttacking", false);
        }
    }

    void Attack()
    {
        isAttacking = true;

        // Stop horizontal movement when attacking
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        // Set animator to attacking
        if (animator != null)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isAttacking", true);
        }
    }

    void Idle()
    {
        isAttacking = false;

        // Stop horizontal movement
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        // Set animator to idle
        if (animator != null)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isAttacking", false);
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Draw detection range
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Yellow
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw attack range
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f); // Red
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        // Draw stopping distance
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Green
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);

        // Draw line to player if in range
        if (player != null && Vector2.Distance(transform.position, player.position) <= detectionRange)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, player.position);
        }

        // Draw ground check
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
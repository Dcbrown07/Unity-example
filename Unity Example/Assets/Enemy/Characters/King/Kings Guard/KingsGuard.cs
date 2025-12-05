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

    [Header("Attack Settings")]
    [Tooltip("Damage dealt to player per attack")]
    public int attackDamage = 1;
    
    [Tooltip("Time between attacks")]
    public float attackCooldown = 1.5f;
    
    [Tooltip("Point where sword/weapon is (create empty child at sword tip)")]
    public Transform attackPoint;
    
    [Tooltip("How far the attack reaches")]
    public float attackRange = 2f;
    
    [Tooltip("Layer(s) that can be hit")]
    public LayerMask playerLayer;

    [Header("Attack Timing")]
    [Tooltip("Delay before damage is dealt (sync with animation)")]
    public float attackDelay = 0.3f;

    [Header("Hit Feedback")]
    [Tooltip("Color to flash player on hit")]
    public Color hitFlashColor = Color.red;
    
    [Tooltip("How long to flash")]
    public float hitFlashDuration = 0.2f;
    
    [Tooltip("Show debug text when hit lands")]
    public bool showHitText = true;

    [Header("Visuals")]
    public SpriteRenderer spriteRenderer;
    public bool flipSpriteToFacePlayer = true;
    
    [Tooltip("Delay before turning to face player (lower = harder to flank)")]
    [Range(0f, 3f)]
    public float turnDelay = 1f;
    
    [Tooltip("Only turn when player moves significantly (harder to flank when still)")]
    public bool requireMovementToTurn = true;
    
    [Tooltip("Distance player must move to trigger turn")]
    public float movementThreshold = 0.5f;
    
    private float lastTurnTime = 0f;
    private bool isFacingRight = true;
    private Vector2 lastPlayerPosition;

    [Header("Debug")]
    public bool showGizmos = true;
    public bool showAttackDebug = true;

    private bool isAttacking = false;
    private float distanceToPlayer;
    private bool isGrounded = false;
    private float lastAttackTime = -999f;
    private bool isDealingDamage = false;
    private Vector2 lastAttackRayStart;
    private Vector2 lastAttackRayEnd;

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
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        // Initialize facing direction
        if (spriteRenderer != null)
        {
            isFacingRight = spriteRenderer.flipX; // Match current sprite orientation
        }

        // Initialize last player position
        if (player != null)
        {
            lastPlayerPosition = player.position;
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

        // DEBUG: Always show distance
        if (Time.frameCount % 30 == 0) // Every 30 frames
        {
            Debug.Log($"Distance to player: {distanceToPlayer:F2} | Attack Distance: {attackDistance}");
        }

        // Check if player is in detection range
        if (distanceToPlayer <= detectionRange)
        {
            // Check if close enough to attack
            if (distanceToPlayer <= attackDistance)
            {
                Debug.Log($"<color=magenta>IN ATTACK RANGE! Distance: {distanceToPlayer:F2}</color>");
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

        // Flip sprite to face player (with delay and movement check)
        if (flipSpriteToFacePlayer && spriteRenderer != null && player != null)
        {
            bool shouldFaceRight = player.position.x > transform.position.x;
            
            // Check if player has moved enough to trigger turn
            bool playerMoved = true;
            if (requireMovementToTurn)
            {
                float distanceMoved = Vector2.Distance(player.position, lastPlayerPosition);
                playerMoved = distanceMoved >= movementThreshold;
                
                if (playerMoved)
                {
                    lastPlayerPosition = player.position;
                }
            }
            
            // Only turn if enough time has passed AND player moved
            if (shouldFaceRight != isFacingRight && Time.time >= lastTurnTime + turnDelay && playerMoved)
            {
                isFacingRight = shouldFaceRight;
                spriteRenderer.flipX = shouldFaceRight;
                lastTurnTime = Time.time;
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
        // Stop horizontal movement when attacking
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        // Check if we can attack (cooldown)
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            isAttacking = true;
            lastAttackTime = Time.time;

            Debug.Log($"<color=cyan>ENEMY ATTACKING! Time: {Time.time}</color>");

            // Set animator to attacking
            if (animator != null)
            {
                animator.SetBool("isWalking", false);
                animator.SetBool("isAttacking", true);
            }

            // Deal damage after delay (to sync with animation)
            if (!isDealingDamage)
            {
                Debug.Log($"<color=magenta>Starting damage coroutine with delay: {attackDelay}s</color>");
                StartCoroutine(DealDamageAfterDelay());
            }
            else
            {
                Debug.Log("<color=yellow>Already dealing damage, skipping coroutine</color>");
            }
        }
        else
        {
            float timeUntilNextAttack = (lastAttackTime + attackCooldown) - Time.time;
            if (showAttackDebug && Time.frameCount % 60 == 0) // Only log occasionally
            {
                Debug.Log($"<color=grey>Attack on cooldown. {timeUntilNextAttack:F1}s remaining</color>");
            }
            
            // Still in cooldown, just stay idle
            isAttacking = false;
            if (animator != null)
            {
                animator.SetBool("isWalking", false);
                animator.SetBool("isAttacking", false);
            }
        }
    }

    // Called by EnemyHealth when counter attacking
    void ForceCounterAttack()
    {
        Debug.Log("<color=yellow>★ COUNTER ATTACK!</color>");
        lastAttackTime = Time.time - attackCooldown; // Reset cooldown
        Attack();
    }

    System.Collections.IEnumerator DealDamageAfterDelay()
    {
        isDealingDamage = true;
        Debug.Log($"<color=cyan>Waiting {attackDelay}s before dealing damage...</color>");
        
        yield return new WaitForSeconds(attackDelay);

        Debug.Log("<color=cyan>Delay complete! Performing raycast attack...</color>");

        // Get attack start position (sword point or enemy center)
        Vector2 attackStartPos = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;
        
        Debug.Log($"Attack starting from: {attackStartPos}");

        // Get all hits in both directions
        RaycastHit2D[] hitsRight = Physics2D.RaycastAll(attackStartPos, Vector2.right, attackRange, playerLayer);
        RaycastHit2D[] hitsLeft = Physics2D.RaycastAll(attackStartPos, Vector2.left, attackRange, playerLayer);

        // Find player hit (ignore self)
        RaycastHit2D hit = new RaycastHit2D();
        Vector2 attackDirection = Vector2.right;
        bool foundPlayer = false;

        // Check right hits
        foreach (RaycastHit2D h in hitsRight)
        {
            if (h.collider.gameObject != gameObject && h.collider.CompareTag("Player"))
            {
                hit = h;
                attackDirection = Vector2.right;
                foundPlayer = true;
                Debug.Log($"<color=yellow>RIGHT raycast found player at distance {h.distance}</color>");
                break;
            }
        }

        // Check left hits if player not found yet
        if (!foundPlayer)
        {
            foreach (RaycastHit2D h in hitsLeft)
            {
                if (h.collider.gameObject != gameObject && h.collider.CompareTag("Player"))
                {
                    hit = h;
                    attackDirection = Vector2.left;
                    foundPlayer = true;
                    Debug.Log($"<color=yellow>LEFT raycast found player at distance {h.distance}</color>");
                    break;
                }
            }
        }

        // Store for gizmo drawing
        lastAttackRayStart = attackStartPos;
        lastAttackRayEnd = attackStartPos + attackDirection * attackRange;

        if (foundPlayer && hit.collider != null)
        {
            Debug.Log($"<color=yellow>Hit player: {hit.collider.gameObject.name}, Distance: {hit.distance}</color>");
            
            PlayerHealth playerHealth = hit.collider.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                
                if (showHitText)
                {
                    Debug.Log($"<color=red>★ HIT! Enemy dealt {attackDamage} damage to player!</color>");
                }
                
                // Visual feedback
                StartCoroutine(FlashPlayerRed(hit.collider.GetComponent<SpriteRenderer>()));
                
                if (showAttackDebug)
                {
                    Debug.Log("<color=green>✓ ATTACK HIT!</color>");
                }
            }
            else
            {
                Debug.LogWarning("Player doesn't have PlayerHealth component!");
            }
        }
        else
        {
            if (showAttackDebug)
            {
                Debug.Log("<color=yellow>✗ Attack missed - no player found</color>");
            }
        }

        isDealingDamage = false;
    }

    System.Collections.IEnumerator FlashPlayerRed(SpriteRenderer playerSprite)
    {
        if (playerSprite == null) yield break;

        Color originalColor = playerSprite.color;
        playerSprite.color = hitFlashColor;
        
        yield return new WaitForSeconds(hitFlashDuration);
        
        playerSprite.color = originalColor;
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
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw attack distance (when to start attacking)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        // Draw stopping distance
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);

        // Draw attack range raycasts (BOTH directions)
        Vector2 previewStart = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;
        Vector2 previewEndRight = previewStart + Vector2.right * attackRange;
        Vector2 previewEndLeft = previewStart + Vector2.left * attackRange;

        Gizmos.color = new Color(1f, 0f, 0f, 0.6f); // Red
        Gizmos.DrawLine(previewStart, previewEndRight);
        Gizmos.DrawLine(previewStart, previewEndLeft);
        Gizmos.DrawWireSphere(previewEndRight, 0.2f);
        Gizmos.DrawWireSphere(previewEndLeft, 0.2f);

        // If recently attacked, show actual attack ray
        if (Application.isPlaying && Time.time - lastAttackTime < 0.5f)
        {
            Gizmos.color = Color.red; // Bright red
            Gizmos.DrawLine(lastAttackRayStart, lastAttackRayEnd);
            Gizmos.DrawSphere(lastAttackRayEnd, 0.15f);
        }

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

        // Draw attack point
        if (attackPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(attackPoint.position, 0.3f);
        }
    }
}
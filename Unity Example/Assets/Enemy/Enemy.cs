using UnityEngine;
using System.Collections;

public enum AIState
{
    Hunting,      // Moving to attack position
    Attacking,    // Actively shooting at player
    Retreating,   // Moving away from danger
    Circling,     // Moving around player
    Dodging,      // Avoiding incoming orbs
    Jumping       // Jumping over obstacles
}

public class EnemyController : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 3;
    [HideInInspector] public int currentHealth;
    public float moveSpeed = 3f;
    public float maxMoveSpeed = 5f;
    
    [Header("Jumping")]
    public float jumpForce = 12f;
    public float coyoteTime = 0.1f;
    private float coyoteCounter;
    private bool canJump = true;
    
    [Header("AI Difficulty (0-1 scale for 5 levels)")]
    [Range(0f, 1f)] public float aggressionLevel = 0.3f;    // How aggressive the AI is
    [Range(0f, 1f)] public float parrySkill = 0.2f;        // Chance to successfully parry
    [Range(0f, 1f)] public float aimAccuracy = 0.5f;       // Shooting accuracy
    [Range(0f, 1f)] public float reactionSpeed = 0.4f;     // How fast AI reacts
    [Range(0f, 1f)] public float obstacleNavigation = 0.6f; // How well it navigates obstacles
    
    [Header("AI Behavior")]
    public float preferredDistance = 4f;
    public float retreatDistance = 2f;
    public float visionRange = 8f;
    
    [Header("Combat")]
    public GameObject orbPrefab;
    public Transform orbSpawnPoint;
    public float fireRate = 2f;
    public float parryRange = 1.5f;
    public float parryDuration = 0.3f;
    public float dangerDetectionRange = 3f;
    private bool isParrying = false;
    private float lastShotTime = 0f;
    
    [Header("Target")]
    public Transform player;
    
    [Header("Ground & Obstacle Detection")]
    public Transform groundCheck;
    public Transform frontCheck;    // Check for walls/obstacles ahead
    public Transform ledgeCheck;    // Check for ledges/drops
    public float groundCheckRadius = 0.2f;
    public float frontCheckDistance = 0.8f;
    public float ledgeCheckDistance = 1f;
    public LayerMask groundLayer;
    private bool isGrounded;
    private bool hitWall;
    private bool nearLedge;
    
    [Header("Health Display")]
    public Transform healthBarParent;
    public GameObject healthPipPrefab;
    private GameObject[] healthPips;
    
    [Header("Respawn")]
    public bool respawn = false;
    public float respawnDelay = 3f;
    private Vector3 spawnPosition;

    // AI State
    private AIState currentState = AIState.Hunting;
    private Vector2 moveDirection = Vector2.right;
    private float stateTimer = 0f;
    private PongOrb threatOrb = null;
    private float nextReactionTime = 0f;
    
    // Components
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        spawnPosition = transform.position;
        currentHealth = maxHealth;
        
        SetupHealthBar();
        
        StartCoroutine(AIBehaviorUpdate());
        StartCoroutine(CombatUpdate());
    }

    void SetupHealthBar()
    {
        if (healthBarParent == null || healthPipPrefab == null) return;
        
        healthPips = new GameObject[maxHealth];
        for (int i = 0; i < maxHealth; i++)
        {
            healthPips[i] = Instantiate(healthPipPrefab, healthBarParent);
        }
        UpdateHealthDisplay();
    }

    void UpdateHealthDisplay()
    {
        if (healthPips == null) return;
        
        for (int i = 0; i < healthPips.Length; i++)
        {
            if (healthPips[i] != null)
                healthPips[i].SetActive(i < currentHealth);
        }
    }

    void FixedUpdate()
    {
        CheckEnvironment();
        UpdateAIState();
        ExecuteMovement();
        UpdateVisuals();
    }
    
    void CheckEnvironment()
    {
        // Ground check
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        
        // Coyote time for jumping
        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
            canJump = true;
        }
        else
        {
            coyoteCounter -= Time.fixedDeltaTime;
        }
        
        // Wall/obstacle check
        Vector2 frontDirection = sr.flipX ? Vector2.left : Vector2.right;
        hitWall = Physics2D.Raycast(frontCheck.position, frontDirection, frontCheckDistance, groundLayer);
        
        // Ledge check
        Vector2 ledgeCheckPos = (Vector2)ledgeCheck.position + frontDirection * ledgeCheckDistance;
        nearLedge = !Physics2D.OverlapCircle(ledgeCheckPos, groundCheckRadius, groundLayer);
    }

    IEnumerator AIBehaviorUpdate()
    {
        while (true)
        {
            if (Time.time >= nextReactionTime)
            {
                AnalyzeSituation();
                // Reaction speed affects how often AI makes decisions
                nextReactionTime = Time.time + (1f - reactionSpeed) * 0.3f + 0.1f;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    void AnalyzeSituation()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        PongOrb nearestThreat = FindNearestThreatOrb();
        
        // Priority 1: Jump over obstacles
        if ((hitWall || nearLedge) && isGrounded && obstacleNavigation > Random.Range(0f, 1f))
        {
            ChangeState(AIState.Jumping);
        }
        // Priority 2: Dodge incoming orbs
        else if (nearestThreat != null && Vector2.Distance(transform.position, nearestThreat.transform.position) < dangerDetectionRange)
        {
            ChangeState(AIState.Dodging);
            threatOrb = nearestThreat;
        }
        // Priority 3: Retreat if low health and close
        else if (distanceToPlayer < retreatDistance && currentHealth <= 1)
        {
            ChangeState(AIState.Retreating);
        }
        // Priority 4: Attack if in good position
        else if (distanceToPlayer <= preferredDistance && distanceToPlayer > retreatDistance && CanSeePlayer())
        {
            ChangeState(AIState.Attacking);
        }
        // Priority 5: Circle if too close but healthy
        else if (distanceToPlayer < retreatDistance && Random.Range(0f, 1f) < aggressionLevel)
        {
            ChangeState(AIState.Circling);
        }
        // Default: Hunt the player
        else
        {
            ChangeState(AIState.Hunting);
        }
    }

    void ChangeState(AIState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            stateTimer = 0f;
        }
    }

    void UpdateAIState()
    {
        stateTimer += Time.fixedDeltaTime;
        
        switch (currentState)
        {
            case AIState.Hunting:
                HuntPlayer();
                break;
                
            case AIState.Attacking:
                AttackPlayer();
                break;
                
            case AIState.Retreating:
                Retreat();
                break;
                
            case AIState.Circling:
                CirclePlayer();
                break;
                
            case AIState.Dodging:
                DodgeOrb();
                break;
                
            case AIState.Jumping:
                JumpObstacle();
                break;
        }
    }

    void HuntPlayer()
    {
        if (player == null) return;
        
        // Move towards player
        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        moveDirection = new Vector2(dirToPlayer.x, 0f);
    }

    void AttackPlayer()
    {
        if (player == null) return;
        
        // Move slowly to maintain distance while shooting
        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        float currentDistance = Vector2.Distance(transform.position, player.position);
        
        if (currentDistance < preferredDistance)
        {
            moveDirection = new Vector2(-dirToPlayer.x * 0.5f, 0f); // Back away slowly
        }
        else if (currentDistance > preferredDistance * 1.2f)
        {
            moveDirection = new Vector2(dirToPlayer.x * 0.5f, 0f); // Move closer slowly
        }
        else
        {
            moveDirection = Vector2.zero; // Stay in position
        }
    }

    void Retreat()
    {
        if (player == null) return;
        
        // Move away from player
        Vector2 dirFromPlayer = (transform.position - player.position).normalized;
        moveDirection = new Vector2(dirFromPlayer.x, 0f);
    }

    void CirclePlayer()
    {
        if (player == null) return;
        
        // Strafe around player
        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        Vector2 perpendicular = new Vector2(-dirToPlayer.y, dirToPlayer.x);
        
        // Randomly choose left or right strafing
        if (stateTimer < 0.1f)
        {
            perpendicular *= Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
        }
        
        moveDirection = new Vector2(perpendicular.x, 0f);
        
        // Change direction after a while
        if (stateTimer > Random.Range(1f, 3f))
        {
            ChangeState(AIState.Hunting);
        }
    }

    void DodgeOrb()
    {
        if (threatOrb == null)
        {
            ChangeState(AIState.Hunting);
            return;
        }
        
        // Move away from the orb
        Vector2 dirFromOrb = (transform.position - threatOrb.transform.position).normalized;
        moveDirection = new Vector2(dirFromOrb.x, 0f);
        
        // Jump if orb is coming horizontally
        if (Mathf.Abs(threatOrb.GetDirection().x) > 0.5f && isGrounded && Random.Range(0f, 1f) < obstacleNavigation)
        {
            Jump();
        }
        
        // Stop dodging if orb is far away
        if (Vector2.Distance(transform.position, threatOrb.transform.position) > dangerDetectionRange)
        {
            threatOrb = null;
            ChangeState(AIState.Hunting);
        }
    }

    void JumpObstacle()
    {
        // Jump and continue moving forward
        if (canJump && coyoteCounter > 0f)
        {
            Jump();
        }
        
        // Continue moving in the same direction
        Vector2 dirToPlayer = player != null ? (player.position - transform.position).normalized : Vector2.right;
        moveDirection = new Vector2(dirToPlayer.x, 0f);
        
        // Return to hunting after jump
        if (stateTimer > 1f)
        {
            ChangeState(AIState.Hunting);
        }
    }

    void Jump()
    {
        if (canJump && coyoteCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            canJump = false;
            coyoteCounter = 0f;
        }
    }

    void ExecuteMovement()
    {
        // Apply horizontal movement
        float targetVelocityX = moveDirection.x * moveSpeed;
        
        // Speed modifications based on state
        switch (currentState)
        {
            case AIState.Dodging:
                targetVelocityX *= 1.5f; // Move faster when dodging
                break;
            case AIState.Attacking:
                targetVelocityX *= 0.6f; // Move slower when attacking
                break;
            case AIState.Retreating:
                targetVelocityX *= 1.2f; // Move faster when retreating
                break;
        }
        
        rb.linearVelocity = new Vector2(targetVelocityX, rb.linearVelocity.y);
    }

    void UpdateVisuals()
    {
        // Flip sprite based on movement direction
        if (Mathf.Abs(moveDirection.x) > 0.1f)
        {
            sr.flipX = moveDirection.x < 0f;
        }
    }

    IEnumerator CombatUpdate()
    {
        while (true)
        {
            CheckForOrbParry();
            TryShoot();
            yield return new WaitForSeconds(0.15f);
        }
    }

    void TryShoot()
    {
        if (Time.time < lastShotTime + fireRate) return;
        if (player == null || orbPrefab == null || orbSpawnPoint == null) return;
        if (currentState != AIState.Attacking && currentState != AIState.Hunting) return;
        if (!CanSeePlayer()) return;
        
        Vector2 targetPoint = PredictPlayerPosition();
        Vector2 shootDir = (targetPoint - (Vector2)orbSpawnPoint.position).normalized;
        
        // Add inaccuracy based on aimAccuracy
        float maxError = (1f - aimAccuracy) * 45f; // Up to 45 degrees of error
        float angleError = Random.Range(-maxError, maxError);
        float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg + angleError;
        shootDir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        
        Vector2 spawnPos = (Vector2)orbSpawnPoint.position + shootDir * 0.5f;
        
        GameObject orb = Instantiate(orbPrefab, spawnPos, Quaternion.identity);
        PongOrb orbScript = orb.GetComponent<PongOrb>();
        orbScript.owner = gameObject;
        orbScript.SetDirection(shootDir);
        
        lastShotTime = Time.time;
    }

    Vector2 PredictPlayerPosition()
    {
        if (player == null) return Vector2.zero;
        
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null && aimAccuracy > 0.3f)
        {
            float predictionTime = Vector2.Distance(transform.position, player.position) / 8f;
            return (Vector2)player.position + playerRb.linearVelocity * predictionTime * aimAccuracy;
        }
        
        return player.position;
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer > visionRange) return false;
        
        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToPlayer, distanceToPlayer, groundLayer);
        
        return hit.collider == null;
    }

    PongOrb FindNearestThreatOrb()
    {
        PongOrb[] orbs = FindObjectsOfType<PongOrb>();
        PongOrb nearest = null;
        float nearestDistance = float.MaxValue;
        
        foreach (PongOrb orb in orbs)
        {
            if (orb.owner == gameObject) continue;
            
            float distance = Vector2.Distance(orb.transform.position, transform.position);
            if (distance < nearestDistance && distance < dangerDetectionRange)
            {
                Vector2 orbToUs = (transform.position - orb.transform.position).normalized;
                float dot = Vector2.Dot(orb.GetDirection(), orbToUs);
                
                if (dot > 0.2f)
                {
                    nearest = orb;
                    nearestDistance = distance;
                }
            }
        }
        
        return nearest;
    }

    void CheckForOrbParry()
    {
        if (isParrying) return;
        if (Random.Range(0f, 1f) > parrySkill) return; // Parry skill check

        PongOrb[] orbs = FindObjectsOfType<PongOrb>();
        foreach (PongOrb orb in orbs)
        {
            if (orb.owner == gameObject) continue;

            float distance = Vector2.Distance(orb.transform.position, transform.position);
            if (distance <= parryRange)
            {
                StartCoroutine(ParryOrb(orb));
                break;
            }
        }
    }

    IEnumerator ParryOrb(PongOrb orb)
    {
        isParrying = true;

        orb.ReverseDirection();
        orb.owner = gameObject;

        yield return new WaitForSeconds(parryDuration);
        isParrying = false;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        UpdateHealthDisplay();
        
        // Get slightly more aggressive when hurt
        aggressionLevel = Mathf.Min(1f, aggressionLevel + 0.1f);
        
        if (currentHealth <= 0)
            DieEnemy();
    }

    void DieEnemy()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);

        if (respawn)
            StartCoroutine(RespawnEnemy());
    }

    IEnumerator RespawnEnemy()
    {
        yield return new WaitForSeconds(respawnDelay);
        currentHealth = maxHealth;
        UpdateHealthDisplay();
        transform.position = spawnPosition;
        gameObject.SetActive(true);
        
        StartCoroutine(AIBehaviorUpdate());
        StartCoroutine(CombatUpdate());
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, parryRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, dangerDetectionRange);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, visionRange);
        
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        
        if (frontCheck != null)
        {
            Gizmos.color = Color.cyan;
            Vector2 dir = sr != null && sr.flipX ? Vector2.left : Vector2.right;
            Gizmos.DrawRay(frontCheck.position, dir * frontCheckDistance);
        }
    }
}
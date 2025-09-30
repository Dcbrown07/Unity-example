using UnityEngine;
using System.Collections;

public enum AIState
{
    Patrolling,   // Searching for player when not in sight
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
    [Range(0f, 1f)] public float aggressionLevel = 0.3f;
    [Range(0f, 1f)] public float parrySkill = 0.2f;
    [Range(0f, 1f)] public float aimAccuracy = 0.5f;
    [Range(0f, 1f)] public float reactionSpeed = 0.4f;
    [Range(0f, 1f)] public float obstacleNavigation = 0.6f;
    
    [Header("AI Behavior")]
    public float preferredDistance = 4f;
    public float retreatDistance = 2f;
    public float visionRange = 8f;
    public float patrolRange = 6f;
    
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
    public Transform frontCheck;
    public Transform ledgeCheck;
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
    private AIState currentState = AIState.Patrolling;
    private Vector2 targetPosition;
    private Vector2 moveDirection = Vector2.right;
    private float stateTimer = 0f;
    private PongOrb threatOrb = null;
    private float nextReactionTime = 0f;
    private bool playerInSight = false;
    private Vector2 lastKnownPlayerPosition;
    private Vector2 patrolStartPosition;
    private float patrolDirection = 1f;
    
    // Components
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        spawnPosition = transform.position;
        patrolStartPosition = transform.position;
        currentHealth = maxHealth;
        
        patrolDirection = Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
        
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
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        
        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
            canJump = true;
        }
        else
        {
            coyoteCounter -= Time.fixedDeltaTime;
        }
        
        Vector2 frontDirection = sr.flipX ? Vector2.left : Vector2.right;
        hitWall = Physics2D.Raycast(frontCheck.position, frontDirection, frontCheckDistance, groundLayer);
        
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
        
        playerInSight = CanSeePlayer() && distanceToPlayer <= visionRange;
        
        if (playerInSight)
        {
            lastKnownPlayerPosition = player.position;
        }
        
        if ((hitWall || nearLedge) && isGrounded && obstacleNavigation > Random.Range(0f, 1f))
        {
            ChangeState(AIState.Jumping);
        }
        else if (nearestThreat != null && Vector2.Distance(transform.position, nearestThreat.transform.position) < dangerDetectionRange)
        {
            ChangeState(AIState.Dodging);
            threatOrb = nearestThreat;
        }
        else if (!playerInSight)
        {
            ChangeState(AIState.Patrolling);
        }
        else if (distanceToPlayer < retreatDistance && currentHealth <= 1)
        {
            ChangeState(AIState.Retreating);
        }
        else if (distanceToPlayer <= preferredDistance && distanceToPlayer > retreatDistance)
        {
            ChangeState(AIState.Attacking);
        }
        else if (distanceToPlayer > preferredDistance * 1.5f || Random.Range(0f, 1f) < 0.3f)
        {
            ChangeState(AIState.Circling);
        }
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
            case AIState.Patrolling:
                PatrolForPlayer();
                break;
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

    void PatrolForPlayer()
    {
        if (lastKnownPlayerPosition != Vector2.zero)
        {
            float distanceToLastKnown = Mathf.Abs(transform.position.x - lastKnownPlayerPosition.x);
            
            if (distanceToLastKnown > 1f)
            {
                targetPosition = new Vector2(lastKnownPlayerPosition.x, transform.position.y);
                return;
            }
            else
            {
                lastKnownPlayerPosition = Vector2.zero;
            }
        }
        
        float distanceFromStart = transform.position.x - patrolStartPosition.x;
        
        if (Mathf.Abs(distanceFromStart) > patrolRange)
        {
            patrolDirection = -Mathf.Sign(distanceFromStart);
        }
        
        if (Random.Range(0f, 1f) < 0.01f)
        {
            patrolDirection = -patrolDirection;
        }
        
        targetPosition = new Vector2(transform.position.x + patrolDirection * 2f, transform.position.y);
    }

    void HuntPlayer()
    {
        if (player == null) return;
        targetPosition = new Vector2(player.position.x, transform.position.y);
    }

    void AttackPlayer()
    {
        if (player == null) return;
        
        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        float currentDistance = Vector2.Distance(transform.position, player.position);
        
        if (currentDistance < preferredDistance)
        {
            moveDirection = new Vector2(-dirToPlayer.x * 0.5f, 0f);
        }
        else if (currentDistance > preferredDistance * 1.2f)
        {
            moveDirection = new Vector2(dirToPlayer.x * 0.5f, 0f);
        }
        else
        {
            moveDirection = Vector2.zero;
        }
        
        targetPosition = new Vector2(transform.position.x + moveDirection.x * 2f, transform.position.y);
    }

    void Retreat()
    {
        if (player == null) return;
        
        Vector2 dirFromPlayer = (transform.position - player.position).normalized;
        targetPosition = new Vector2(transform.position.x + dirFromPlayer.x * 3f, transform.position.y);
    }

    void CirclePlayer()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        if (distanceToPlayer < preferredDistance)
        {
            float direction = transform.position.x > player.position.x ? 1f : -1f;
            targetPosition = new Vector2(transform.position.x + direction * 2f, transform.position.y);
        }
        else
        {
            targetPosition = new Vector2(player.position.x, transform.position.y);
        }
        
        if (stateTimer > Random.Range(2f, 4f))
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
        
        float directionFromOrb = transform.position.x > threatOrb.transform.position.x ? 1f : -1f;
        targetPosition = new Vector2(transform.position.x + directionFromOrb * 2f, transform.position.y);
        
        if (Mathf.Abs(threatOrb.GetDirection().x) > 0.5f && isGrounded && Random.Range(0f, 1f) < obstacleNavigation)
        {
            Jump();
        }
        
        if (threatOrb == null || Vector2.Distance(transform.position, threatOrb.transform.position) > dangerDetectionRange)
        {
            threatOrb = null;
            ChangeState(AIState.Hunting);
        }
    }

    void JumpObstacle()
    {
        if (canJump && coyoteCounter > 0f)
        {
            Jump();
        }
        
        Vector2 dirToPlayer = player != null ? (player.position - transform.position).normalized : Vector2.right;
        targetPosition = new Vector2(transform.position.x + dirToPlayer.x * 2f, transform.position.y);
        
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
        Vector2 currentPos = transform.position;
        
        float horizontalDirection = 0f;
        float distanceToTarget = Mathf.Abs(targetPosition.x - currentPos.x);
        
        if (distanceToTarget > 0.5f)
        {
            horizontalDirection = targetPosition.x > currentPos.x ? 1f : -1f;
        }
        
        float currentMoveSpeed = moveSpeed;
        switch (currentState)
        {
            case AIState.Dodging:
                currentMoveSpeed = maxMoveSpeed;
                break;
            case AIState.Retreating:
                currentMoveSpeed = moveSpeed * 1.2f;
                break;
            case AIState.Attacking:
                currentMoveSpeed = moveSpeed * 0.8f;
                break;
        }
        
        rb.linearVelocity = new Vector2(horizontalDirection * currentMoveSpeed, rb.linearVelocity.y);
    }

    void UpdateVisuals()
    {
        if (rb.linearVelocity.x != 0)
        {
            sr.flipX = rb.linearVelocity.x < 0f;
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
        if (!playerInSight) return;
        
        Vector2 targetPoint = PredictPlayerPosition();
        Vector2 shootDir = (targetPoint - (Vector2)orbSpawnPoint.position).normalized;
        
        float maxError = (1f - aimAccuracy) * 45f;
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
        if (Random.Range(0f, 1f) > parrySkill) return;

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

    void OnDisable()
    {
        Debug.Log("=== ENEMY DISABLED ===");
        Debug.Log(gameObject.name + " was disabled. Current health: " + currentHealth);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        UpdateHealthDisplay();
        
        Debug.Log("=== ENEMY DAMAGE ===");
        Debug.Log(gameObject.name + " took " + damage + " damage. Health left: " + currentHealth);
        
        aggressionLevel = Mathf.Min(1f, aggressionLevel + 0.1f);
        
        if (currentHealth <= 0)
        {
            Debug.Log(gameObject.name + " health is 0 or below - calling DieEnemy()");
            DieEnemy();
        }
        else
        {
            Debug.Log(gameObject.name + " is still alive with " + currentHealth + " health");
        }
    }

    void DieEnemy()
    {
        Debug.Log("=== DIE ENEMY CALLED ===");
        Debug.Log(gameObject.name + " died!");
        
        StopAllCoroutines();
        gameObject.SetActive(false);
        
        if (LevelManager.Instance != null)
        {
            Debug.Log("LevelManager found! Calling EnemyDefeated()");
            LevelManager.Instance.EnemyDefeated();
        }
        else
        {
            Debug.LogError("LevelManager.Instance is NULL! Can't notify level completion.");
        }

        if (respawn)
        {
            Debug.Log("Enemy will respawn in " + respawnDelay + " seconds");
            StartCoroutine(RespawnEnemy());
        }
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
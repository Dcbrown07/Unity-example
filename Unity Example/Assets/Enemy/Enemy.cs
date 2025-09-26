using UnityEngine;
using System.Collections;

public enum AIState
{
    Hunting,      // Moving to attack position
    Attacking,    // Actively shooting at player
    Retreating,   // Moving away from danger
    Circling,     // Circling around player
    Dodging       // Avoiding incoming orbs
}

public class EnemyController : MonoBehaviour
{
    [Header("Stats")]
    public int health = 3;
    public float moveSpeed = 3f;
    public float maxMoveSpeed = 5f;
    
    [Header("AI Behavior")]
    public float aggressionLevel = 0.7f;    // 0 = defensive, 1 = very aggressive
    public float reactionTime = 0.3f;       // How fast AI reacts to threats
    public float preferredDistance = 4f;    // Ideal distance from player
    public float retreatDistance = 2f;      // Distance to start retreating
    public float circleRadius = 3f;         // Radius for circling behavior
    
    [Header("Combat")]
    public GameObject orbPrefab;
    public Transform orbSpawnPoint;
    public float fireRate = 1.5f;
    public float aimAccuracy = 0.8f;        // 0 = terrible aim, 1 = perfect aim
    public float parryRange = 1.5f;
    public float parryDuration = 0.3f;
    public float dangerDetectionRange = 3f; // How far to look for incoming orbs
    private bool isParrying = false;
    private float lastShotTime = 0f;
    
    [Header("Target")]
    public Transform player;
    
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;
    
    [Header("Respawn")]
    public bool respawn = false;
    public float respawnDelay = 3f;
    private Vector3 spawnPosition;

    // AI State
    private AIState currentState = AIState.Hunting;
    private Vector2 targetPosition;
    private float stateTimer = 0f;
    private float circleAngle = 0f;
    private PongOrb threatOrb = null;
    
    // Components
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        spawnPosition = transform.position;
        
        // Start with a random circle position
        circleAngle = Random.Range(0f, 360f);
        
        StartCoroutine(AIBehaviorUpdate());
        StartCoroutine(CombatUpdate());
    }

    void FixedUpdate()
    {
        CheckGrounded();
        UpdateAIState();
        ExecuteMovement();
        UpdateVisuals();
    }
    
    void CheckGrounded()
    {
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    IEnumerator AIBehaviorUpdate()
    {
        while (true)
        {
            AnalyzeSituation();
            yield return new WaitForSeconds(0.1f); // Update AI decisions 10 times per second
        }
    }

    void AnalyzeSituation()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        PongOrb nearestThreat = FindNearestThreatOrb();
        
        // Priority 1: Dodge incoming orbs
        if (nearestThreat != null && Vector2.Distance(transform.position, nearestThreat.transform.position) < dangerDetectionRange)
        {
            ChangeState(AIState.Dodging);
            threatOrb = nearestThreat;
        }
        // Priority 2: Parry if orb is very close
        else if (ShouldParry())
        {
            // Parrying is handled in CheckForOrbParry
        }
        // Priority 3: Retreat if too close and low health
        else if (distanceToPlayer < retreatDistance && health <= 1)
        {
            ChangeState(AIState.Retreating);
        }
        // Priority 4: Attack if in good position
        else if (distanceToPlayer <= preferredDistance && distanceToPlayer > retreatDistance && CanSeePlayer())
        {
            ChangeState(AIState.Attacking);
        }
        // Priority 5: Circle around player to find good position
        else if (distanceToPlayer > preferredDistance * 1.5f || Random.Range(0f, 1f) < 0.3f)
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
            
            // State-specific initialization
            switch (newState)
            {
                case AIState.Circling:
                    // Pick a new circle angle
                    circleAngle += Random.Range(45f, 180f);
                    break;
                    
                case AIState.Retreating:
                    // Find retreat position opposite from player
                    Vector2 retreatDir = (transform.position - player.position).normalized;
                    targetPosition = (Vector2)transform.position + retreatDir * 3f;
                    break;
            }
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
        }
    }

    void HuntPlayer()
    {
        if (player == null) return;
        targetPosition = player.position;
    }

    void AttackPlayer()
    {
        if (player == null) return;
        
        // Stay at preferred distance while attacking
        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        targetPosition = (Vector2)player.position - dirToPlayer * preferredDistance;
        
        // Add some randomness to make it less predictable
        Vector2 randomOffset = new Vector2(
            Random.Range(-1f, 1f) * (1f - aggressionLevel),
            Random.Range(-0.5f, 0.5f) * (1f - aggressionLevel)
        );
        targetPosition += randomOffset;
    }

    void Retreat()
    {
        // Already set targetPosition in ChangeState
        // Add some vertical movement to avoid being predictable
        if (stateTimer > 1f)
        {
            targetPosition.y += Random.Range(-1f, 1f);
        }
    }

    void CirclePlayer()
    {
        if (player == null) return;
        
        // Update circle angle
        float circleSpeed = 30f * (aggressionLevel + 0.5f); // More aggressive = faster circling
        circleAngle += circleSpeed * Time.fixedDeltaTime;
        
        // Calculate circle position
        Vector2 playerPos = player.position;
        float radians = circleAngle * Mathf.Deg2Rad;
        targetPosition = playerPos + new Vector2(
            Mathf.Cos(radians) * circleRadius,
            Mathf.Sin(radians) * circleRadius * 0.5f // Flatten vertically
        );
    }

    void DodgeOrb()
    {
        if (threatOrb == null)
        {
            ChangeState(AIState.Hunting);
            return;
        }
        
        // Calculate dodge direction perpendicular to orb's path
        Vector2 orbVelocity = threatOrb.GetDirection();
        Vector2 perpendicular = new Vector2(-orbVelocity.y, orbVelocity.x);
        
        // Choose dodge direction based on which side is safer
        float rightSide = Vector2.Dot(perpendicular, Vector2.right);
        if (rightSide < 0) perpendicular = -perpendicular;
        
        targetPosition = (Vector2)transform.position + perpendicular * 2f;
        
        // Stop dodging if orb is far away or destroyed
        if (Vector2.Distance(transform.position, threatOrb.transform.position) > dangerDetectionRange)
        {
            threatOrb = null;
            ChangeState(AIState.Hunting);
        }
    }

    void ExecuteMovement()
    {
        Vector2 currentPos = rb.position;
        Vector2 moveDirection = (targetPosition - currentPos).normalized;
        
        // Adjust speed based on state
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
                currentMoveSpeed = moveSpeed * 0.8f; // Slower when attacking for accuracy
                break;
        }
        
        // Apply movement
        Vector2 newPosition = currentPos + moveDirection * currentMoveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }

    void UpdateVisuals()
    {
        // Flip sprite based on movement direction
        if (player != null)
        {
            sr.flipX = player.position.x < transform.position.x;
        }
    }

    IEnumerator CombatUpdate()
    {
        while (true)
        {
            CheckForOrbParry();
            TryShoot();
            yield return new WaitForSeconds(0.1f);
        }
    }

    void TryShoot()
    {
        if (Time.time < lastShotTime + fireRate) return;
        if (player == null || orbPrefab == null || orbSpawnPoint == null) return;
        if (currentState != AIState.Attacking && currentState != AIState.Hunting) return;
        
        // Only shoot if we have a reasonable shot
        if (!CanSeePlayer()) return;
        
        Vector2 targetPoint = PredictPlayerPosition();
        Vector2 shootDir = (targetPoint - (Vector2)orbSpawnPoint.position).normalized;
        
        // Add inaccuracy based on aimAccuracy
        float inaccuracy = (1f - aimAccuracy) * 30f; // Up to 30 degrees of error
        float angleError = Random.Range(-inaccuracy, inaccuracy);
        float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg + angleError;
        shootDir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        
        Vector2 spawnPos = (Vector2)orbSpawnPoint.position + shootDir * 0.5f;
        
        GameObject orb = Instantiate(orbPrefab, spawnPos, Quaternion.identity);
        PongOrb orbScript = orb.GetComponent<PongOrb>();
        orbScript.owner = gameObject;
        orbScript.SetDirection(shootDir);
        
        lastShotTime = Time.time;
        Debug.Log(name + " shot orb at player!");
    }

    Vector2 PredictPlayerPosition()
    {
        if (player == null) return Vector2.zero;
        
        // Try to predict where player will be
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            float predictionTime = Vector2.Distance(transform.position, player.position) / 8f; // Assuming orb speed of 8
            return (Vector2)player.position + playerRb.linearVelocity * predictionTime;
        }
        
        return player.position;
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;
        
        // Simple line of sight check
        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToPlayer, 
                                           Vector2.Distance(transform.position, player.position), 
                                           groundLayer);
        
        return hit.collider == null; // No obstacles in the way
    }

    PongOrb FindNearestThreatOrb()
    {
        PongOrb[] orbs = FindObjectsOfType<PongOrb>();
        PongOrb nearest = null;
        float nearestDistance = float.MaxValue;
        
        foreach (PongOrb orb in orbs)
        {
            if (orb.owner == gameObject) continue; // Ignore own orbs
            
            float distance = Vector2.Distance(orb.transform.position, transform.position);
            if (distance < nearestDistance && distance < dangerDetectionRange)
            {
                // Check if orb is moving towards us
                Vector2 orbToUs = (transform.position - orb.transform.position).normalized;
                float dot = Vector2.Dot(orb.GetDirection(), orbToUs);
                
                if (dot > 0.3f) // Orb is moving somewhat towards us
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
        if (!ShouldParry()) return;

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

    bool ShouldParry()
    {
        // More aggressive enemies parry more often
        // Lower health enemies parry more often (desperation)
        float parryChance = aggressionLevel + (1f - (float)health / 3f) * 0.3f;
        return Random.Range(0f, 1f) < parryChance;
    }

    IEnumerator ParryOrb(PongOrb orb)
    {
        isParrying = true;
        Debug.Log(name + " is parrying!");

        orb.ReverseDirection();
        orb.owner = gameObject; // Take ownership of parried orb

        yield return new WaitForSeconds(parryDuration);
        isParrying = false;
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log(name + " took " + damage + " damage. Health left: " + health);
        
        // Become more aggressive when hurt
        aggressionLevel = Mathf.Min(1f, aggressionLevel + 0.2f);
        
        if (health <= 0)
            DieEnemy();
    }

    void DieEnemy()
    {
        Debug.Log(name + " died!");
        StopAllCoroutines();
        gameObject.SetActive(false);

        if (respawn)
            StartCoroutine(RespawnEnemy());
    }

    IEnumerator RespawnEnemy()
    {
        yield return new WaitForSeconds(respawnDelay);
        health = 3;
        aggressionLevel = 0.7f; // Reset aggression
        transform.position = spawnPosition;
        gameObject.SetActive(true);
        
        // Restart coroutines
        StartCoroutine(AIBehaviorUpdate());
        StartCoroutine(CombatUpdate());
        
        Debug.Log(name + " respawned!");
    }

    void OnDrawGizmosSelected()
    {
        // Draw detection ranges
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, parryRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, dangerDetectionRange);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, preferredDistance);
        
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
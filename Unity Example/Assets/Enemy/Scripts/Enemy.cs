using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum AIState
{
    Patrolling,
    Hunting,
    Attacking,
    Retreating,
    Circling,
    Dodging,
    Following
}

public class EnemyController : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 3;
    [HideInInspector] public int currentHealth;
    public float moveSpeed = 3f;
    public float maxMoveSpeed = 5f;

    [Header("Movement Smoothing")]
    public float acceleration = 40f;
    public float deceleration = 60f;
    public float maxHorizontalSpeed = 5f;

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

    [Header("AI Behavior")]
    public float preferredDistance = 4f;
    public float retreatDistance = 2f;
    public float visionRange = 15f;
    public float pathUpdateInterval = 1f;

    [Header("Combat")]
    public GameObject orbPrefab;
    public Transform orbSpawnPoint;
    public float fireRate = 2f;
    public float parryRange = 1.5f;
    public float parryDuration = 0.3f;
    public float dangerDetectionRange = 3f;
    private bool isParrying = false;
    private float lastShotTime = -999f;

    [Header("Target")]
    public Transform player;
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    public bool showPathGizmos = true;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("Health Display")]
    public Transform healthBarParent;
    public GameObject healthPipPrefab;
    private GameObject[] healthPips;

    [Header("Respawn")]
    public bool respawn = false;
    public float respawnDelay = 3f;
    private Vector3 spawnPosition;

    // Pathfinding
    private List<Vector2> currentPath;
    private int currentPathIndex = 0;
    private float lastPathUpdate = 0f;
    private Vector2 targetWaypoint;
    private bool needsJump = false;

    private AIState currentState = AIState.Patrolling;
    private float stateTimer = 0f;
    private PongOrb threatOrb = null;
    private float nextReactionTime = 0f;
    private bool playerInSight = false;
    private Vector2 lastKnownPlayerPosition;

    private Rigidbody2D rb;
    private SpriteRenderer sr;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        spawnPosition = transform.position;
        currentHealth = maxHealth;

        SetupHealthBar();

        // Ensure pathfinding exists
        SimplePathfinding.EnsureInstance();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log($"[{gameObject.name}] Auto-found player: {player.name}");
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] NO PLAYER FOUND!");
            }
        }

        lastShotTime = Time.time - fireRate;

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
        FollowPath();
        UpdateVisuals();
    }

    void CheckEnvironment()
    {
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        else
            isGrounded = false;

        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
            canJump = true;
        }
        else
        {
            coyoteCounter -= Time.fixedDeltaTime;
        }
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

        playerInSight = distanceToPlayer <= visionRange;

        if (playerInSight)
        {
            lastKnownPlayerPosition = player.position;
        }

        // State decision logic
        if (nearestThreat != null && Vector2.Distance(transform.position, nearestThreat.transform.position) < dangerDetectionRange)
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
        else if (distanceToPlayer > preferredDistance * 1.5f)
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
            if (showDebugLogs)
                Debug.Log($"[{gameObject.name}] State change: {currentState} -> {newState}");
            currentState = newState;
            stateTimer = 0f;
            currentPath = null;
        }
    }

    void UpdateAIState()
    {
        stateTimer += Time.fixedDeltaTime;

        // Update path periodically
        if (Time.time - lastPathUpdate > pathUpdateInterval)
        {
            UpdatePath();
            lastPathUpdate = Time.time;
        }

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
        }
    }

    void UpdatePath()
    {
        if (SimplePathfinding.Instance == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("SimplePathfinding not found!");
            return;
        }

        Vector2 targetPos = GetTargetPosition();
        
        if (targetPos == Vector2.zero)
            return;

        currentPath = SimplePathfinding.Instance.FindPath(transform.position, targetPos);
        currentPathIndex = 0;

        if (currentPath != null && currentPath.Count > 0)
        {
            targetWaypoint = currentPath[0];
        }
    }

    Vector2 GetTargetPosition()
    {
        switch (currentState)
        {
            case AIState.Patrolling:
                if (lastKnownPlayerPosition != Vector2.zero)
                    return lastKnownPlayerPosition;
                return (Vector2)transform.position + new Vector2(Random.Range(-5f, 5f), 0);

            case AIState.Hunting:
            case AIState.Attacking:
                if (player != null)
                    return player.position;
                break;

            case AIState.Retreating:
                if (player != null)
                {
                    Vector2 awayDir = ((Vector2)transform.position - (Vector2)player.position).normalized;
                    return (Vector2)transform.position + awayDir * 5f;
                }
                break;

            case AIState.Circling:
                if (player != null)
                {
                    float angle = Time.time * 0.5f;
                    Vector2 offset = new Vector2(Mathf.Cos(angle), 0) * preferredDistance;
                    return (Vector2)player.position + offset;
                }
                break;

            case AIState.Dodging:
                if (threatOrb != null)
                {
                    Vector2 awayDir = ((Vector2)transform.position - (Vector2)threatOrb.transform.position).normalized;
                    return (Vector2)transform.position + awayDir * 3f;
                }
                break;
        }

        return Vector2.zero;
    }

    void PatrolForPlayer()
    {
        // Path handled by UpdatePath
    }

    void HuntPlayer()
    {
        // Path handled by UpdatePath
    }

    void AttackPlayer()
    {
        if (player == null) return;

        float currentDistance = Vector2.Distance(transform.position, player.position);

        // Stop moving if at preferred distance
        if (Mathf.Abs(currentDistance - preferredDistance) < 1f)
        {
            currentPath = null;
        }
    }

    void Retreat()
    {
        // Path handled by UpdatePath
    }

    void CirclePlayer()
    {
        // Path handled by UpdatePath
        if (stateTimer > Random.Range(2f, 4f))
        {
            ChangeState(AIState.Hunting);
        }
    }

    void DodgeOrb()
    {
        if (threatOrb == null || Vector2.Distance(transform.position, threatOrb.transform.position) > dangerDetectionRange)
        {
            threatOrb = null;
            ChangeState(AIState.Hunting);
        }

        if (stateTimer > 1.2f)
        {
            ChangeState(AIState.Hunting);
        }
    }

    void FollowPath()
    {
        if (currentPath == null || currentPath.Count == 0)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // Get current waypoint
        if (currentPathIndex >= currentPath.Count)
        {
            currentPath = null;
            return;
        }

        targetWaypoint = currentPath[currentPathIndex];

        // Check if we need to jump for next waypoint
        if (currentPathIndex + 1 < currentPath.Count && SimplePathfinding.Instance != null)
        {
            Vector2 nextWaypoint = currentPath[currentPathIndex + 1];
            needsJump = SimplePathfinding.Instance.RequiresJump(targetWaypoint, nextWaypoint);
        }

        // Move towards waypoint
        float distanceToWaypoint = Vector2.Distance(transform.position, targetWaypoint);
        
        if (distanceToWaypoint < 0.3f)
        {
            currentPathIndex++;
            if (currentPathIndex >= currentPath.Count)
            {
                currentPath = null;
                return;
            }
            targetWaypoint = currentPath[currentPathIndex];
        }

        // Calculate horizontal movement
        float direction = Mathf.Sign(targetWaypoint.x - transform.position.x);
        float targetSpeed = moveSpeed;

        // Speed modifications based on state
        switch (currentState)
        {
            case AIState.Dodging:
                targetSpeed = Mathf.Min(maxMoveSpeed, moveSpeed * 1.6f);
                break;
            case AIState.Retreating:
                targetSpeed = moveSpeed * 1.2f;
                break;
            case AIState.Attacking:
                targetSpeed = moveSpeed * 0.8f;
                break;
        }

        targetSpeed = Mathf.Min(targetSpeed, maxHorizontalSpeed);
        float desiredVelocityX = direction * targetSpeed;

        float accel = (Mathf.Abs(desiredVelocityX) > Mathf.Abs(rb.linearVelocity.x)) ? acceleration : deceleration;
        float newVelX = Mathf.MoveTowards(rb.linearVelocity.x, desiredVelocityX, accel * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(newVelX, rb.linearVelocity.y);

        // Jump if needed
        if (needsJump && isGrounded && canJump && coyoteCounter > 0f)
        {
            Jump();
            needsJump = false;
        }

        // Also jump if waypoint is significantly above us
        if (targetWaypoint.y > transform.position.y + 1f && isGrounded && canJump && coyoteCounter > 0f)
        {
            Jump();
        }
    }

    void Jump()
    {
        if (!canJump || coyoteCounter <= 0f) return;
        Vector2 v = rb.linearVelocity;
        v.y = jumpForce;
        rb.linearVelocity = v;
        canJump = false;
        coyoteCounter = 0f;

        if (showDebugLogs)
            Debug.Log($"[{gameObject.name}] JUMPED!");
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
        if (player == null || orbPrefab == null || orbSpawnPoint == null)
            return;

        bool canShootInState = (currentState == AIState.Attacking || 
                                currentState == AIState.Hunting || 
                                currentState == AIState.Circling);
        
        if (!canShootInState || !playerInSight)
            return;

        float timeSinceLastShot = Time.time - lastShotTime;
        if (timeSinceLastShot < fireRate)
            return;

        Vector2 targetPoint = PredictPlayerPosition();
        Vector2 shootDir = (targetPoint - (Vector2)orbSpawnPoint.position).normalized;

        float maxError = (1f - aimAccuracy) * 45f;
        float angleError = Random.Range(-maxError, maxError);
        float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg + angleError;
        shootDir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

        Vector2 spawnPos = (Vector2)orbSpawnPoint.position + shootDir * 0.5f;

        GameObject orb = Instantiate(orbPrefab, spawnPos, Quaternion.identity);
        PongOrb orbScript = orb.GetComponent<PongOrb>();
        if (orbScript != null)
        {
            orbScript.owner = gameObject;
            orbScript.SetDirection(shootDir);
        }

        lastShotTime = Time.time;
        
        if (showDebugLogs)
            Debug.Log($"[{gameObject.name}] SHOT ORB!");
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

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        UpdateHealthDisplay();

        Debug.Log($"[{gameObject.name}] took {damage} damage. Health: {currentHealth}");

        aggressionLevel = Mathf.Min(1f, aggressionLevel + 0.1f);

        if (currentHealth <= 0)
        {
            DieEnemy();
        }
    }

    void DieEnemy()
    {
        Debug.Log($"[{gameObject.name}] DIED!");

        StopAllCoroutines();
        gameObject.SetActive(false);

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.EnemyDefeated();
        }

        if (respawn)
        {
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

    void OnDrawGizmos()
    {
        if (!showPathGizmos || currentPath == null)
            return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
        }

        if (currentPath.Count > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetWaypoint, 0.3f);
        }
    }
}
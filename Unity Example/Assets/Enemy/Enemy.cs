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
    Jumping
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
    private float lastShotTime = -999f;

    [Header("Target")]
    public Transform player;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool ignoreVisionChecks = false; // TESTING: Set true to bypass vision system

    [Header("Ground & Obstacle Detection")]
    public Transform groundCheck;
    public Transform frontCheck;
    public Transform ledgeCheck;
    public float groundCheckRadius = 0.2f;
    public float frontCheckDistance = 0.8f;
    public float ledgeCheckDistance = 1f;
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;
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

    [Header("Adaptive Bounds Detection")]
    public bool detectBoundsAtStart = true;
    public float boundsPadding = 0.2f;
    private float minX = float.NegativeInfinity;
    private float maxX = float.PositiveInfinity;

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

        // AUTO-FIND PLAYER IF NOT ASSIGNED
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
                Debug.LogError($"[{gameObject.name}] NO PLAYER FOUND! Make sure player has 'Player' tag!");
            }
        }

        if (detectBoundsAtStart)
            ComputeAdaptiveBounds();

        lastShotTime = Time.time - fireRate;

        StartCoroutine(AIBehaviorUpdate());
        StartCoroutine(CombatUpdate());
        
        if (showDebugLogs)
            Debug.Log($"[{gameObject.name}] Enemy initialized. Player: {(player != null ? player.name : "NULL")}");
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

        Vector2 frontDirection = sr != null && sr.flipX ? Vector2.left : Vector2.right;
        if (frontCheck != null)
        {
            RaycastHit2D wallHit = Physics2D.Raycast(frontCheck.position, frontDirection, frontCheckDistance, obstacleLayer);
            hitWall = wallHit.collider != null;
        }
        else
        {
            hitWall = false;
        }

        if (ledgeCheck != null)
        {
            Vector2 ledgeCheckPos = (Vector2)ledgeCheck.position + frontDirection * ledgeCheckDistance;
            nearLedge = !Physics2D.OverlapCircle(ledgeCheckPos, groundCheckRadius, groundLayer);
        }
        else
        {
            nearLedge = false;
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
        if (player == null)
        {
            if (showDebugLogs && Time.frameCount % 100 == 0)
                Debug.LogWarning($"[{gameObject.name}] Player is NULL in AnalyzeSituation!");
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        PongOrb nearestThreat = FindNearestThreatOrb();

        // SIMPLIFIED VISION CHECK - just check distance first
        bool canSeePlayer = CanSeePlayer();
        playerInSight = canSeePlayer && distanceToPlayer <= visionRange;

        if (showDebugLogs && Time.frameCount % 60 == 0) // Log every 60 frames
        {
            Debug.Log($"[{gameObject.name}] Distance: {distanceToPlayer:F2} | InSight: {playerInSight} | State: {currentState}");
        }

        if (playerInSight)
        {
            lastKnownPlayerPosition = player.position;
        }

        // State decision logic
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
            if (showDebugLogs)
                Debug.Log($"[{gameObject.name}] State change: {currentState} -> {newState}");
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

        if (threatOrb != null && Mathf.Abs(threatOrb.GetDirection().x) > 0.5f && isGrounded && Random.Range(0f, 1f) < obstacleNavigation)
        {
            Jump();
        }

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

    void JumpObstacle()
    {
        if (isGrounded && canJump && coyoteCounter > 0f)
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
        if (!canJump || coyoteCounter <= 0f) return;
        Vector2 v = rb.linearVelocity;
        v.y = jumpForce;
        rb.linearVelocity = v;
        canJump = false;
        coyoteCounter = 0f;
    }

    void ExecuteMovement()
    {
        Vector2 currentPos = transform.position;
        float distanceToTarget = Mathf.Abs(targetPosition.x - currentPos.x);

        float desiredDir = 0f;
        if (distanceToTarget > 0.5f) desiredDir = targetPosition.x > currentPos.x ? 1f : -1f;

        float targetSpeed = moveSpeed;
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
            default:
                targetSpeed = moveSpeed;
                break;
        }

        targetSpeed = Mathf.Min(targetSpeed, maxHorizontalSpeed);
        float desiredVelocityX = desiredDir * targetSpeed;

        float accel = (Mathf.Abs(desiredVelocityX) > Mathf.Abs(rb.linearVelocity.x)) ? acceleration : deceleration;
        float newVelX = Mathf.MoveTowards(rb.linearVelocity.x, desiredVelocityX, accel * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(newVelX, rb.linearVelocity.y);

        if (!float.IsInfinity(minX) && !float.IsInfinity(maxX))
        {
            float edgeThreshold = 0.15f + boundsPadding;
            if (transform.position.x < minX + edgeThreshold)
            {
                targetPosition.x = minX + 1f;
                if (currentState == AIState.Dodging) ChangeState(AIState.Hunting);
                if (rb.linearVelocity.x < 0f) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
            else if (transform.position.x > maxX - edgeThreshold)
            {
                targetPosition.x = maxX - 1f;
                if (currentState == AIState.Dodging) ChangeState(AIState.Hunting);
                if (rb.linearVelocity.x > 0f) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }
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
        if (player == null)
        {
            if (showDebugLogs && Time.frameCount % 100 == 0)
                Debug.LogWarning($"[{gameObject.name}] TryShoot: Player is NULL!");
            return;
        }
        
        if (orbPrefab == null)
        {
            if (showDebugLogs && Time.frameCount % 100 == 0)
                Debug.LogWarning($"[{gameObject.name}] TryShoot: orbPrefab is NULL!");
            return;
        }
        
        if (orbSpawnPoint == null)
        {
            if (showDebugLogs && Time.frameCount % 100 == 0)
                Debug.LogWarning($"[{gameObject.name}] TryShoot: orbSpawnPoint is NULL!");
            return;
        }

        // RELAXED SHOOTING CONDITIONS - allow shooting in more states
        bool canShootInState = (currentState == AIState.Attacking || 
                                currentState == AIState.Hunting || 
                                currentState == AIState.Circling);
        
        if (!canShootInState)
        {
            if (showDebugLogs && Time.frameCount % 100 == 0)
                Debug.Log($"[{gameObject.name}] Not in shooting state. Current: {currentState}");
            return;
        }

        // Relax vision requirement
        if (!playerInSight && aggressionLevel < 0.5f)
        {
            if (showDebugLogs && Time.frameCount % 100 == 0)
                Debug.Log($"[{gameObject.name}] Player not in sight and low aggression");
            return;
        }

        float timeSinceLastShot = Time.time - lastShotTime;
        if (timeSinceLastShot < fireRate)
        {
            if (showDebugLogs && Time.frameCount % 100 == 0)
                Debug.Log($"[{gameObject.name}] On cooldown. Time since shot: {timeSinceLastShot:F2}/{fireRate}");
            return;
        }

        // SHOOT!
        Vector2 targetPoint = PredictPlayerPosition();
        Vector2 shootDir = (targetPoint - (Vector2)orbSpawnPoint.position).normalized;
        if (shootDir == Vector2.zero) shootDir = (player.position - orbSpawnPoint.position).normalized;

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
            Debug.Log($"[{gameObject.name}] SHOT ORB at {shootDir}");
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
        if (distanceToPlayer > visionRange)
        {
            if (showDebugLogs && Time.frameCount % 100 == 0)
                Debug.Log($"[{gameObject.name}] Player too far: {distanceToPlayer:F2} > {visionRange}");
            return false;
        }

        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        
        // IMPORTANT FIX: Check if we even have layers set
        if (groundLayer.value == 0 && obstacleLayer.value == 0)
        {
            // No occlusion layers set, just use distance
            if (showDebugLogs && Time.frameCount % 100 == 0)
                Debug.Log($"[{gameObject.name}] No layer masks set - can see player!");
            return true;
        }

        int occlusionMask = (groundLayer.value | obstacleLayer.value);
        
        // Raycast with slightly offset start position to avoid self-collision
        Vector2 rayStart = (Vector2)transform.position + dirToPlayer * 0.1f;
        RaycastHit2D hit = Physics2D.Raycast(rayStart, dirToPlayer, distanceToPlayer - 0.1f, occlusionMask);

        if (showDebugLogs && Time.frameCount % 100 == 0)
        {
            if (hit.collider != null)
                Debug.Log($"[{gameObject.name}] Vision blocked by: {hit.collider.gameObject.name} (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");
            else
                Debug.Log($"[{gameObject.name}] Clear line of sight to player!");
        }

        if (hit.collider == null) return true;
        if (hit.collider.gameObject == player.gameObject) return true;

        return false;
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
        Debug.Log($"[{gameObject.name}] DISABLED. Health: {currentHealth}");
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
        else
        {
            Debug.LogError("LevelManager.Instance is NULL!");
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

    void ComputeAdaptiveBounds()
    {
        Collider2D[] all = FindObjectsOfType<Collider2D>();
        List<float> xs = new List<float>();

        int layerMaskCombined = (groundLayer.value | obstacleLayer.value);

        foreach (Collider2D c in all)
        {
            if (c == null) continue;
            if (((1 << c.gameObject.layer) & layerMaskCombined) == 0) continue;
            if (c.isTrigger) continue;

            Rigidbody2D attachedRb = c.attachedRigidbody;
            if (attachedRb != null && attachedRb.bodyType != RigidbodyType2D.Static) continue;

            Bounds b = c.bounds;
            xs.Add(b.min.x);
            xs.Add(b.max.x);
        }

        if (xs.Count > 0)
        {
            float minFound = float.MaxValue;
            float maxFound = float.MinValue;
            foreach (float x in xs)
            {
                if (x < minFound) minFound = x;
                if (x > maxFound) maxFound = x;
            }

            minX = minFound - boundsPadding;
            maxX = maxFound + boundsPadding;
            return;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            float camHeight = 2f * cam.orthographicSize;
            float camWidth = camHeight * cam.aspect;
            Vector3 camCenter = cam.transform.position;
            minX = camCenter.x - camWidth * 0.5f - boundsPadding;
            maxX = camCenter.x + camWidth * 0.5f + boundsPadding;
            return;
        }

        minX = float.NegativeInfinity;
        maxX = float.PositiveInfinity;
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

                if (!float.IsInfinity(minX) && !float.IsInfinity(maxX))
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(new Vector3(minX, transform.position.y - 5f, 0f), new Vector3(minX, transform.position.y + 5f, 0f));
                    Gizmos.DrawLine(new Vector3(maxX, transform.position.y - 5f, 0f), new Vector3(maxX, transform.position.y + 5f, 0f));
                }
            }
        }
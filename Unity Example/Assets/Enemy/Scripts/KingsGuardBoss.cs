using UnityEngine;
using System.Collections;

public class KingsGuardBoss : MonoBehaviour
{
    [Header("Boss Info")]
    public string bossName = "Kings Guard";
    
    [Header("Health")]
    public EnemyHealth healthSystem; // Use existing EnemyHealth component
    
    public enum BossPhase { Phase1_Aggressive, Phase2_Defensive, Phase3_Berserk }
    
    [Header("Phase System")]
    public BossPhase currentPhase = BossPhase.Phase1_Aggressive;
    public int phase1HealthThreshold = 35; // Health to enter phase 2
    public int phase2HealthThreshold = 15; // Health to enter phase 3
    
    [Header("References")]
    public Transform player;
    public Rigidbody2D rb;
    public SpriteRenderer sprite;
    public Animator animator;
    
    [Header("Arena Bounds")]
    public float arenaMinX = -10f;
    public float arenaMaxX = 10f;
    
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float phase2SpeedMultiplier = 1.3f;
    public float phase3SpeedMultiplier = 1.6f;
    public float stoppingDistance = 2f;
    
    [Header("Basic Attack")]
    public Transform attackPoint;
    public float attackRange = 3f;
    public int attackDamage = 2;
    public float attackCooldown = 1.2f;
    private float lastAttackTime = -999f;
    
    [Header("Special Attacks")]
    public float dashAttackSpeed = 15f;
    public float dashAttackRange = 8f;
    public int dashAttackDamage = 3;
    public float dashAttackCooldown = 5f;
    private float lastDashAttackTime = -999f;
    
    public int spinAttackDamage = 2;
    public float spinAttackRadius = 4f;
    public float spinAttackCooldown = 7f;
    private float lastSpinAttackTime = -999f;
    
    public GameObject shockwavePrefab;
    public float shockwaveCooldown = 8f;
    private float lastShockwaveTime = -999f;
    
    [Header("Defense")]
    public bool canBlock = true;
    public float blockChance = 0.3f;
    public float blockDuration = 1f;
    public float blockCooldown = 3f;
    private bool isBlocking = false;
    
    [Header("Visuals")]
    public Color phase1Color = Color.white;
    public Color phase2Color = new Color(1f, 0.8f, 0f); // Gold
    public Color phase3Color = new Color(1f, 0f, 0f); // Red
    public Color blockColor = Color.cyan;
    public Color damageColor = Color.red;
    private Color originalColor;
    
    [Header("Audio")]
    public AudioClip basicAttackSound;
    public AudioClip dashAttackSound;
    public AudioClip spinAttackSound;
    public AudioClip blockSound;
    public AudioClip hurtSound;
    public AudioClip phaseChangeSound;
    public AudioClip deathSound;
    private AudioSource audioSource;
    
    [Header("Effects")]
    public GameObject hitEffect;
    public GameObject blockEffect;
    public GameObject phaseChangeEffect;
    public GameObject deathExplosion;
    
    // State
    private bool isDashing = false;
    private bool isSpinning = false;
    private bool isAttacking = false;
    private bool facingRight = true;
    
    void Start()
    {
        // Get or add EnemyHealth component
        if (healthSystem == null)
        {
            healthSystem = GetComponent<EnemyHealth>();
            if (healthSystem == null)
            {
                healthSystem = gameObject.AddComponent<EnemyHealth>();
            }
        }
        
        // Configure health system for boss BEFORE it initializes
        healthSystem.maxHealth = 50;
        healthSystem.hasShield = true;
        healthSystem.blockChanceAtFullHealth = 0.2f;
        healthSystem.blockChanceAtLowHealth = 0.5f;
        healthSystem.destroyOnDeath = false; // We handle death ourselves
        
        // Force health initialization if EnemyHealth hasn't started yet
        if (healthSystem.GetHealth() == 0)
        {
            // Manually set current health via reflection or just wait
            Debug.Log("Waiting for EnemyHealth to initialize...");
        }
        
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) originalColor = sprite.color;
        
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        
        if (animator == null) animator = GetComponent<Animator>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
        
        StartCoroutine(BossIntro());
        StartCoroutine(MonitorHealth());
    }
    
    IEnumerator MonitorHealth()
    {
        // Wait longer for EnemyHealth.Start() to run
        yield return new WaitForSeconds(1f);
        
        if (healthSystem == null)
        {
            Debug.LogError("Health system not found!");
            yield break;
        }
        
        // Initialize lastHealth AFTER waiting
        int lastHealth = healthSystem.GetHealth();
        Debug.Log($"<color=cyan>Boss starting health: {lastHealth}/{healthSystem.GetMaxHealth()}</color>");
        
        if (lastHealth == 0)
        {
            Debug.LogError("Health is still 0! EnemyHealth may not have initialized properly.");
            yield break;
        }
        
        bool hasEnteredPhase2 = false;
        bool hasEnteredPhase3 = false;
        
        while (!healthSystem.IsDead())
        {
            int currentHP = healthSystem.GetHealth();
            
            // Only check phase transitions if health actually decreased
            if (currentHP < lastHealth)
            {
                Debug.Log($"<color=yellow>Health decreased from {lastHealth} to {currentHP}</color>");
                
                // Check phase transitions based on health AND current phase
                if (currentHP <= phase2HealthThreshold && currentPhase == BossPhase.Phase2_Defensive && !hasEnteredPhase3)
                {
                    hasEnteredPhase3 = true;
                    StartCoroutine(EnterPhase3());
                }
                else if (currentHP <= phase1HealthThreshold && currentPhase == BossPhase.Phase1_Aggressive && !hasEnteredPhase2)
                {
                    hasEnteredPhase2 = true;
                    StartCoroutine(EnterPhase2());
                }
                
                lastHealth = currentHP;
            }
            
            yield return new WaitForSeconds(0.2f);
        }
        
        // Health reached 0, boss died
        StartCoroutine(Die());
    }
    
    IEnumerator BossIntro()
    {
        Debug.Log($"<color=yellow>★★★ {bossName} BOSS FIGHT START! ★★★</color>");
        
        // Dramatic entrance
        if (sprite != null)
        {
            sprite.color = Color.black;
            for (int i = 0; i < 10; i++)
            {
                sprite.color = Color.Lerp(Color.black, phase1Color, i / 10f);
                yield return new WaitForSeconds(0.05f);
            }
        }
        
        PlaySound(phaseChangeSound);
        SpawnEffect(phaseChangeEffect, transform.position);
        
        yield return new WaitForSeconds(0.5f);
        
        // Start AI
        StartCoroutine(BossAI());
    }
    
    IEnumerator BossAI()
    {
        while (healthSystem != null && !healthSystem.IsDead())
        {
            if (player == null || isDashing || isSpinning || isBlocking || isAttacking)
            {
                yield return null;
                continue;
            }
            
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            
            // Phase-specific attack decision making
            if (currentPhase == BossPhase.Phase3_Berserk)
            {
                // Phase 3: Aggressive, uses everything
                if (Time.time >= lastSpinAttackTime + spinAttackCooldown && distanceToPlayer < spinAttackRadius)
                {
                    StartCoroutine(SpinAttack());
                }
                else if (Time.time >= lastDashAttackTime + dashAttackCooldown && distanceToPlayer > 4f)
                {
                    StartCoroutine(DashAttack());
                }
                else if (Time.time >= lastShockwaveTime + shockwaveCooldown && distanceToPlayer > 3f)
                {
                    StartCoroutine(ShockwaveAttack());
                }
                else if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + (attackCooldown * 0.7f))
                {
                    StartCoroutine(BasicAttack());
                }
                else if (distanceToPlayer > stoppingDistance)
                {
                    ChasePlayer();
                }
            }
            else if (currentPhase == BossPhase.Phase2_Defensive)
            {
                // Phase 2: Balanced, more defensive
                if (Time.time >= lastDashAttackTime + dashAttackCooldown && distanceToPlayer > 5f)
                {
                    StartCoroutine(DashAttack());
                }
                else if (Time.time >= lastShockwaveTime + shockwaveCooldown && distanceToPlayer > 4f)
                {
                    StartCoroutine(ShockwaveAttack());
                }
                else if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
                {
                    StartCoroutine(BasicAttack());
                }
                else if (distanceToPlayer > stoppingDistance)
                {
                    ChasePlayer();
                }
            }
            else // Phase 1
            {
                // Phase 1: Standard attacks
                if (Time.time >= lastDashAttackTime + dashAttackCooldown && distanceToPlayer > 6f)
                {
                    StartCoroutine(DashAttack());
                }
                else if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
                {
                    StartCoroutine(BasicAttack());
                }
                else if (distanceToPlayer > stoppingDistance)
                {
                    ChasePlayer();
                }
                else
                {
                    StopMoving();
                }
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    void ChasePlayer()
    {
        if (player == null || rb == null) return;
        
        float currentSpeed = moveSpeed;
        if (currentPhase == BossPhase.Phase2_Defensive) currentSpeed *= phase2SpeedMultiplier;
        if (currentPhase == BossPhase.Phase3_Berserk) currentSpeed *= phase3SpeedMultiplier;
        
        float direction = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(direction * currentSpeed, rb.linearVelocity.y);
        
        // Face player
        facingRight = direction > 0;
        if (sprite != null) sprite.flipX = facingRight; // Fixed: removed the '!'
        
        if (animator != null) animator.SetBool("isWalking", true);
    }
    
    void StopMoving()
    {
        if (rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        if (animator != null) animator.SetBool("isWalking", false);
    }
    
    IEnumerator BasicAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        StopMoving();
        
        if (animator != null) animator.SetTrigger("Attack");
        PlaySound(basicAttackSound);
        
        yield return new WaitForSeconds(0.3f); // Wind up
        
        // Check for hit
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth ph = hit.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(attackDamage);
                    SpawnEffect(hitEffect, hit.transform.position);
                    Debug.Log($"<color=red>Boss basic attack hit for {attackDamage} damage!</color>");
                }
            }
        }
        
        yield return new WaitForSeconds(0.5f); // Recovery
        isAttacking = false;
    }
    
    IEnumerator DashAttack()
    {
        isDashing = true;
        lastDashAttackTime = Time.time;
        
        Debug.Log("<color=cyan>★ DASH ATTACK!</color>");
        PlaySound(dashAttackSound);
        
        // Lock direction
        Vector2 dashDirection = new Vector2(facingRight ? 1 : -1, 0);
        
        // Visual telegraph
        if (sprite != null)
        {
            for (int i = 0; i < 3; i++)
            {
                sprite.color = Color.yellow;
                yield return new WaitForSeconds(0.1f);
                sprite.color = originalColor;
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        // DASH!
        float dashTimer = 0f;
        float dashDuration = 0.5f;
        Vector2 startPos = transform.position;
        
        while (dashTimer < dashDuration)
        {
            rb.linearVelocity = dashDirection * dashAttackSpeed;
            
            // Check for player hit during dash
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.5f);
            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    PlayerHealth ph = hit.GetComponent<PlayerHealth>();
                    if (ph != null)
                    {
                        ph.TakeDamage(dashAttackDamage);
                        SpawnEffect(hitEffect, hit.transform.position);
                        Debug.Log($"<color=red>Dash attack hit for {dashAttackDamage} damage!</color>");
                        break;
                    }
                }
            }
            
            dashTimer += Time.deltaTime;
            yield return null;
        }
        
        StopMoving();
        yield return new WaitForSeconds(0.3f);
        isDashing = false;
    }
    
    IEnumerator SpinAttack()
    {
        isSpinning = true;
        lastSpinAttackTime = Time.time;
        StopMoving();
        
        Debug.Log("<color=magenta>★ SPIN ATTACK!</color>");
        PlaySound(spinAttackSound);
        
        if (animator != null) animator.SetTrigger("Spin");
        
        // Spin for 1 second, dealing damage multiple times
        float spinDuration = 1f;
        float damageInterval = 0.2f;
        float timer = 0f;
        float lastDamageTime = 0f;
        
        while (timer < spinDuration)
        {
            // Rotate sprite
            if (sprite != null)
            {
                transform.Rotate(0, 0, 720 * Time.deltaTime);
            }
            
            // Deal damage at intervals
            if (timer - lastDamageTime >= damageInterval)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, spinAttackRadius);
                foreach (Collider2D hit in hits)
                {
                    if (hit.CompareTag("Player"))
                    {
                        PlayerHealth ph = hit.GetComponent<PlayerHealth>();
                        if (ph != null)
                        {
                            ph.TakeDamage(spinAttackDamage);
                            SpawnEffect(hitEffect, hit.transform.position);
                        }
                    }
                }
                lastDamageTime = timer;
            }
            
            timer += Time.deltaTime;
            yield return null;
        }
        
        transform.rotation = Quaternion.identity;
        yield return new WaitForSeconds(0.5f);
        isSpinning = false;
    }
    
    IEnumerator ShockwaveAttack()
    {
        lastShockwaveTime = Time.time;
        StopMoving();
        
        Debug.Log("<color=yellow>★ SHOCKWAVE!</color>");
        
        if (animator != null) animator.SetTrigger("Shockwave");
        
        yield return new WaitForSeconds(0.5f);
        
        // Spawn shockwave projectiles in both directions
        if (shockwavePrefab != null)
        {
            GameObject wave1 = Instantiate(shockwavePrefab, transform.position, Quaternion.identity);
            GameObject wave2 = Instantiate(shockwavePrefab, transform.position, Quaternion.identity);
            
            Shockwave sw1 = wave1.GetComponent<Shockwave>();
            Shockwave sw2 = wave2.GetComponent<Shockwave>();
            
            if (sw1 != null) sw1.Initialize(Vector2.right, attackDamage);
            if (sw2 != null) sw2.Initialize(Vector2.left, attackDamage);
        }
        
        yield return new WaitForSeconds(0.3f);
    }
    
    // TakeDamage is now handled by EnemyHealth component
    // This is just for reference if you need to manually trigger damage
    public void DebugDamage(int amount)
    {
        if (healthSystem != null)
        {
            healthSystem.TakeDamage(amount);
        }
    }
    
    IEnumerator DamageFlash()
    {
        // EnemyHealth handles damage flash, but we can add extra effects here
        if (sprite == null) yield break;
        yield return null;
    }
    
    IEnumerator EnterPhase2()
    {
        currentPhase = BossPhase.Phase2_Defensive;
        StopMoving();
        
        // Increase block chance for phase 2
        if (healthSystem != null)
        {
            healthSystem.blockChanceAtFullHealth = 0.4f;
            healthSystem.blockChanceAtLowHealth = 0.7f;
        }
        
        Debug.Log("<color=yellow>★★ PHASE 2: DEFENSIVE STANCE ★★</color>");
        PlaySound(phaseChangeSound);
        SpawnEffect(phaseChangeEffect, transform.position);
        
        // Screen shake
        StartCoroutine(ScreenShake(0.5f, 0.5f));
        
        // Visual transformation
        if (sprite != null)
        {
            for (int i = 0; i < 20; i++)
            {
                sprite.color = Color.Lerp(phase1Color, phase2Color, i / 20f);
                yield return new WaitForSeconds(0.05f);
            }
        }
        
        yield return new WaitForSeconds(1f);
    }
    
    IEnumerator EnterPhase3()
    {
        currentPhase = BossPhase.Phase3_Berserk;
        StopMoving();
        
        // Lower block chance but boss is faster and more aggressive
        if (healthSystem != null)
        {
            healthSystem.blockChanceAtFullHealth = 0.2f;
            healthSystem.blockChanceAtLowHealth = 0.5f;
        }
        
        Debug.Log("<color=red>★★★ PHASE 3: BERSERK MODE ★★★</color>");
        PlaySound(phaseChangeSound);
        SpawnEffect(phaseChangeEffect, transform.position);
        
        // MASSIVE screen shake
        StartCoroutine(ScreenShake(1f, 1f));
        
        // Visual transformation
        if (sprite != null)
        {
            for (int i = 0; i < 20; i++)
            {
                sprite.color = Color.Lerp(phase2Color, phase3Color, i / 20f);
                transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.3f, i / 20f);
                yield return new WaitForSeconds(0.05f);
            }
        }
        
        yield return new WaitForSeconds(1f);
    }
    
    IEnumerator Die()
    {
        StopMoving();
        StopAllCoroutines();
        
        Debug.Log("<color=red>★★★ KINGS GUARD DEFEATED! ★★★</color>");
        PlaySound(deathSound);
        
        // Death sequence
        if (sprite != null)
        {
            for (int i = 0; i < 15; i++)
            {
                sprite.color = Color.white;
                yield return new WaitForSeconds(0.05f);
                sprite.color = Color.black;
                yield return new WaitForSeconds(0.05f);
            }
        }
        
        // Explosion
        StartCoroutine(ScreenShake(1.5f, 1.5f));
        
        for (int i = 0; i < 8; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 2f;
            if (deathExplosion != null)
            {
                Instantiate(deathExplosion, (Vector2)transform.position + offset, Quaternion.identity);
            }
            else
            {
                GameObject explosion = new GameObject("Explosion");
                explosion.transform.position = (Vector2)transform.position + offset;
                explosion.AddComponent<ProceduralExplosion>();
            }
            yield return new WaitForSeconds(0.1f);
        }
        
        yield return new WaitForSeconds(1.5f);
        
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.EnemyDefeated();
        }
        
        Destroy(gameObject);
    }
    
    IEnumerator ScreenShake(float intensity, float duration)
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam == null) yield break;
        
        Vector3 originalPos = cam.transform.localPosition;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;
            cam.transform.localPosition = originalPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        cam.transform.localPosition = originalPos;
    }
    
    Color GetPhaseColor()
    {
        return currentPhase switch
        {
            BossPhase.Phase1_Aggressive => phase1Color,
            BossPhase.Phase2_Defensive => phase2Color,
            BossPhase.Phase3_Berserk => phase3Color,
            _ => Color.white
        };
    }
    
    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    void SpawnEffect(GameObject effect, Vector3 position)
    {
        if (effect != null)
        {
            Instantiate(effect, position, Quaternion.identity);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Attack range
        Gizmos.color = Color.red;
        if (attackPoint != null)
        {
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
        
        // Spin attack range
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, spinAttackRadius);
        
        // Arena bounds
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(arenaMinX, transform.position.y, 0), 
                       new Vector3(arenaMaxX, transform.position.y, 0));
    }
}
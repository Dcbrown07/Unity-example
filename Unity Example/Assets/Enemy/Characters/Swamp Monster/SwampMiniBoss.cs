using UnityEngine;
using System.Collections;

public class SwampMonsterBoss : MonoBehaviour
{
    [Header("Boss Info")]
    public string bossName = "Swamp Monster";
    public int maxHealth = 30;
    private int currentHealth;
    
    [Header("References")]
    public Transform player;
    public Rigidbody2D rb;
    public SpriteRenderer sprite;
    public Animator animator;
    
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float stoppingDistance = 4f; // Stop this far from player
    public float attackRange = 5f; // Vine whip range
    
    [Header("Vine Whip Attack")]
    public Transform attackPoint; // Position where vine whip hits
    public float whipRange = 5f; // How far the whip reaches
    public int whipDamage = 2;
    public float attackCooldown = 2f;
    public float attackWindup = 0.5f; // Time before damage
    public float attackRecovery = 0.5f; // Time after attack
    private float lastAttackTime = -999f;
    
    [Header("Visuals")]
    public Color normalColor = new Color(0.2f, 0.6f, 0.2f); // Swamp green
    public Color damageColor = Color.red;
    public Color attackColor = new Color(0.4f, 0.8f, 0.2f); // Bright green when attacking
    
    [Header("Audio")]
    public AudioClip roarSound;
    public AudioClip whipSound;
    public AudioClip hurtSound;
    public AudioClip deathSound;
    private AudioSource audioSource;
    
    [Header("Effects")]
    public GameObject hitEffect;
    public GameObject deathExplosion;
    
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;
    
    private bool isAttacking = false;
    private bool isDead = false;
    private bool facingRight = true;
    private Color originalColor;
    
    void Start()
    {
        currentHealth = maxHealth;
        
        // Auto-find components
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
        
        if (sprite != null)
        {
            sprite.color = normalColor;
            originalColor = normalColor;
        }
        
        // Setup rigidbody
        if (rb != null)
        {
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        
        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Auto-find player
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
        
        // Boss intro
        StartCoroutine(BossIntro());
    }
    
    void Update()
    {
        if (isDead || player == null || isAttacking) return;
        
        // Ground check
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
        else
        {
            isGrounded = true;
        }
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // Decide action based on distance
        if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            StartCoroutine(VineWhipAttack());
        }
        else if (distanceToPlayer > stoppingDistance && isGrounded)
        {
            ChasePlayer();
        }
        else
        {
            StopMoving();
        }
        
        // Update animator
        if (animator != null)
        {
            animator.SetBool("isWalking", !isAttacking && distanceToPlayer > stoppingDistance);
        }
    }
    
    void ChasePlayer()
    {
        if (player == null || rb == null) return;
        
        float direction = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        
        // Face player
        facingRight = direction > 0;
        if (sprite != null)
        {
            sprite.flipX = !facingRight; // Flip based on your sprite's default facing
        }
    }
    
    void StopMoving()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }
    
    IEnumerator VineWhipAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        StopMoving();
        
        Debug.Log("<color=green>Swamp Monster: VINE WHIP!</color>");
        
        // Trigger attack animation
        if (animator != null)
        {
            animator.SetTrigger("VineWhip");
        }
        
        // Visual telegraph - color change
        if (sprite != null)
        {
            sprite.color = attackColor;
        }
        
        // Wind-up
        yield return new WaitForSeconds(attackWindup);
        
        // Play whip sound
        PlaySound(whipSound);
        
        // Check for player hit
        Vector2 attackPos = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPos, whipRange);
        
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(whipDamage);
                    SpawnEffect(hitEffect, hit.transform.position);
                    Debug.Log($"<color=red>Vine whip hit player for {whipDamage} damage!</color>");
                }
            }
        }
        
        // Recovery
        yield return new WaitForSeconds(attackRecovery);
        
        // Return to normal color
        if (sprite != null)
        {
            sprite.color = originalColor;
        }
        
        isAttacking = false;
    }
    
    public void TakeDamage(int amount)
    {
        if (isDead) return;
        
        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);
        
        Debug.Log($"<color=orange>{bossName} took {amount} damage! Health: {currentHealth}/{maxHealth}</color>");
        
        // Visual feedback
        StartCoroutine(DamageFlash());
        
        // Audio
        PlaySound(hurtSound);
        
        // Check death
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    IEnumerator DamageFlash()
    {
        if (sprite == null) yield break;
        
        sprite.color = damageColor;
        yield return new WaitForSeconds(0.2f);
        sprite.color = originalColor;
    }
    
    IEnumerator BossIntro()
    {
        Debug.Log($"<color=green>★★ {bossName} APPEARS! ★★</color>");
        
        // Roar
        PlaySound(roarSound);
        
        // Dramatic entrance
        if (sprite != null)
        {
            for (int i = 0; i < 3; i++)
            {
                sprite.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                sprite.color = normalColor;
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        yield return new WaitForSeconds(0.5f);
    }
    
    void Die()
    {
        if (isDead) return;
        isDead = true;
        
        Debug.Log($"<color=red>★★ {bossName} DEFEATED! ★★</color>");
        
        StopMoving();
        StopAllCoroutines();
        
        StartCoroutine(DeathSequence());
    }
    
    IEnumerator DeathSequence()
    {
        PlaySound(deathSound);
        
        // Death flash
        if (sprite != null)
        {
            for (int i = 0; i < 8; i++)
            {
                sprite.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                sprite.color = Color.black;
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        // Spawn explosions
        for (int i = 0; i < 5; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 1.5f;
            
            if (deathExplosion != null)
            {
                Instantiate(deathExplosion, (Vector2)transform.position + offset, Quaternion.identity);
            }
            else
            {
                // Create procedural explosion if no prefab
                GameObject explosion = new GameObject("Explosion");
                explosion.transform.position = (Vector2)transform.position + offset;
                explosion.AddComponent<ProceduralExplosion>();
            }
            
            yield return new WaitForSeconds(0.15f);
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // Notify level manager
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.EnemyDefeated();
        }
        
        Destroy(gameObject);
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
    
    // Public accessors
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercent() => (float)currentHealth / maxHealth;
    
    void OnDrawGizmosSelected()
    {
        // Attack range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Vine whip range
        Gizmos.color = Color.yellow;
        Vector3 whipPos = attackPoint != null ? attackPoint.position : transform.position;
        Gizmos.DrawWireSphere(whipPos, whipRange);
        
        // Stopping distance
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
        
        // Ground check
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
using UnityEngine;
using System.Collections;

public enum EnemyType { Melee, Ranged }
public enum MovementType { Walking, Flying }

public class SimpleEnemy : MonoBehaviour
{
    [Header("=== ENEMY TYPE ===")]
    public EnemyType enemyType = EnemyType.Melee;
    public MovementType movementType = MovementType.Walking;
    public bool canJump = false; // Can jump obstacles
    public bool hasShield = false; // Can block attacks
    public bool dealContactDamage = false; // Touch damage

    [Header("Health")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float preferredDistance = 6f; // For ranged enemies
    public float attackDistance = 2f;

    [Header("Ground (if not flying)")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("Combat")]
    public Transform attackPoint; // Melee weapon tip OR projectile spawn
    public GameObject projectilePrefab; // For ranged
    public int attackDamage = 1;
    public float attackCooldown = 1.5f;
    public float attackRange = 2f; // Melee range
    public float attackDelay = 0.3f; // Melee delay
    private float lastAttackTime = -999f;

    [Header("Shield (if hasShield)")]
    [Range(0f, 1f)] public float blockChance = 0.5f;
    public Color blockColor = Color.cyan;

    [Header("Contact Damage")]
    public int contactDamage = 1;
    public float contactCooldown = 1f;
    private float lastContactTime = -999f;

    [Header("Visuals")]
    public SpriteRenderer sprite;
    public float turnDelay = 0.5f;
    private float lastTurnTime = 0f;
    private bool facingRight = true;

    [Header("References")]
    public Transform player;
    public Animator animator;
    private Rigidbody2D rb;
    private Color originalColor;

    // Helper properties
    private bool IsMelee => enemyType == EnemyType.Melee;
    private bool CanFly => movementType == MovementType.Flying;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) originalColor = sprite.color;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (rb != null)
        {
            rb.gravityScale = CanFly ? 0f : 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    void Update()
    {
        if (player == null) return;

        // Ground check
        if (!CanFly && groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
        }
        else if (CanFly)
        {
            isGrounded = true;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        // Movement
        if (IsMelee)
        {
            if (dist > attackDistance) MoveTowards();
            else StopMoving();
        }
        else // Ranged
        {
            if (dist < 3f) MoveAway();
            else if (dist > preferredDistance + 2f) MoveTowards();
            else StopMoving();
        }

        // Attack
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            if (IsMelee && dist <= attackDistance)
            {
                MeleeAttack();
            }
            else if (!IsMelee && dist >= 3f && dist <= preferredDistance + 2f)
            {
                RangedAttack();
            }
        }

        // Turn to face player
        if (Time.time >= lastTurnTime + turnDelay)
        {
            bool shouldFaceRight = player.position.x > transform.position.x;
            if (shouldFaceRight != facingRight)
            {
                facingRight = shouldFaceRight;
                if (sprite != null) sprite.flipX = shouldFaceRight;
                lastTurnTime = Time.time;
            }
        }
    }

    void MoveTowards()
    {
        if (rb == null || player == null) return;

        if (CanFly)
        {
            Vector2 dir = (player.position - transform.position).normalized;
            rb.linearVelocity = dir * moveSpeed;
        }
        else if (isGrounded)
        {
            float dir = Mathf.Sign(player.position.x - transform.position.x);
            rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
        }
    }

    void MoveAway()
    {
        if (rb == null || player == null) return;

        if (CanFly)
        {
            Vector2 dir = (transform.position - player.position).normalized;
            rb.linearVelocity = dir * moveSpeed;
        }
        else if (isGrounded)
        {
            float dir = -Mathf.Sign(player.position.x - transform.position.x);
            rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
        }
    }

    void StopMoving()
    {
        if (rb == null) return;
        if (CanFly) rb.linearVelocity = Vector2.zero;
        else rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    void MeleeAttack()
    {
        lastAttackTime = Time.time;
        StartCoroutine(MeleeAttackDelay());
    }

    IEnumerator MeleeAttackDelay()
    {
        yield return new WaitForSeconds(attackDelay);

        Vector2 pos = attackPoint ? (Vector2)attackPoint.position : (Vector2)transform.position;
        
        // Check both directions
        RaycastHit2D[] hits = Physics2D.RaycastAll(pos, Vector2.right, attackRange);
        bool found = CheckHitsForPlayer(hits);
        
        if (!found)
        {
            hits = Physics2D.RaycastAll(pos, Vector2.left, attackRange);
            CheckHitsForPlayer(hits);
        }
    }

    bool CheckHitsForPlayer(RaycastHit2D[] hits)
    {
        foreach (RaycastHit2D h in hits)
        {
            if (h.collider.gameObject != gameObject && h.collider.CompareTag("Player"))
            {
                DamagePlayer(h.collider.gameObject);
                return true;
            }
        }
        return false;
    }

    void RangedAttack()
    {
        if (projectilePrefab == null || attackPoint == null) return;

        lastAttackTime = Time.time;

        // Shoot in the direction the enemy is facing with some randomness
        Vector2 dir;
        if (facingRight)
        {
            // Shooting right - add random spread
            dir = new Vector2(1f, Random.Range(-0.3f, 0.3f)).normalized;
        }
        else
        {
            // Shooting left - add random spread
            dir = new Vector2(-1f, Random.Range(-0.3f, 0.3f)).normalized;
        }
        
        GameObject proj = Instantiate(projectilePrefab, attackPoint.position, Quaternion.identity);

        PongOrb orb = proj.GetComponent<PongOrb>();
        if (orb != null)
        {
            orb.owner = gameObject;
            orb.SetDirection(dir);
            orb.damageAmount = attackDamage;
        }
    }

    void DamagePlayer(GameObject playerObj)
    {
        PlayerHealth ph = playerObj.GetComponent<PlayerHealth>();
        if (ph != null) ph.TakeDamage(attackDamage);
    }

    public void TakeDamage(int amount, Vector2 attackDir = default)
    {
        // Check shield block
        if (hasShield && Random.value < blockChance)
        {
            Block();
            return;
        }

        currentHealth -= amount;
        StartCoroutine(DamageFlash());

        if (currentHealth <= 0) Die();
    }

    void Block()
    {
        Debug.Log($"{gameObject.name} blocked!");
        StartCoroutine(BlockFlash());
    }

    IEnumerator BlockFlash()
    {
        if (sprite == null) yield break;
        sprite.color = blockColor;
        yield return new WaitForSeconds(0.2f);
        sprite.color = originalColor;
    }

    IEnumerator DamageFlash()
    {
        if (sprite == null) yield break;
        sprite.color = Color.red;
        yield return new WaitForSeconds(0.2f);
        sprite.color = originalColor;
    }

    void Die()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.EnemyDefeated();
        }
        Destroy(gameObject);
    }

    void OnCollisionStay2D(Collision2D col)
    {
        if (!dealContactDamage) return;
        if (Time.time < lastContactTime + contactCooldown) return;

        if (col.gameObject.CompareTag("Player"))
        {
            PlayerHealth ph = col.gameObject.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(contactDamage);
                lastContactTime = Time.time;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, IsMelee ? attackDistance : preferredDistance);
    }
}
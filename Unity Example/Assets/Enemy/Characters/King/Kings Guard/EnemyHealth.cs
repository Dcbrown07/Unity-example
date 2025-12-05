using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health")]
    public int maxHealth = 10;
    
    [SerializeField] private int currentHealth;

    [Header("Shield Mechanic")]
    [Tooltip("Enable shield blocking system")]
    public bool hasShield = true;
    
    [Tooltip("Block chance at full health")]
    [Range(0f, 1f)]
    public float blockChanceAtFullHealth = 0.3f;
    
    [Tooltip("Block chance at low health (1 HP) - INCREASES as health drops")]
    [Range(0f, 1f)]
    public float blockChanceAtLowHealth = 0.9f;
    
    [Tooltip("Direction range for blocking (degrees from forward)")]
    [Range(0f, 180f)]
    public float blockAngle = 90f;

    [Header("Visual Feedback")]
    public SpriteRenderer spriteRenderer;
    
    [Tooltip("Color when blocking")]
    public Color blockColor = new Color(0.5f, 0.5f, 1f, 1f);
    
    [Tooltip("Color when taking damage")]
    public Color damageColor = Color.red;
    
    [Tooltip("Flash duration")]
    public float flashDuration = 0.2f;

    [Header("Shield Visual (Optional)")]
    [Tooltip("Shield sprite renderer to show/hide")]
    public SpriteRenderer shieldSprite;
    
    [Tooltip("Show shield briefly when blocking")]
    public bool flashShieldOnBlock = true;

    [Header("Health Display")]
    [Tooltip("Parent for health bar pips")]
    public Transform healthBarParent;
    
    [Tooltip("Health pip prefab")]
    public GameObject healthPipPrefab;
    
    private GameObject[] healthPips;

    [Header("Death Settings")]
    [Tooltip("Destroy enemy on death")]
    public bool destroyOnDeath = true;
    
    [Tooltip("Delay before destroying")]
    public float deathDelay = 1f;

    [Header("Audio (Optional)")]
    public AudioClip blockSound;
    public AudioClip damageSound;
    public AudioClip deathSound;
    private AudioSource audioSource;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Color originalColor;
    private bool isDead = false;
    private bool isBlocking = false;

    void Start()
    {
        currentHealth = maxHealth;
        
        // Auto-find sprite renderer
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (blockSound != null || damageSound != null || deathSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Setup health bar
        SetupHealthBar();

        // Hide shield initially
        if (shieldSprite != null)
        {
            shieldSprite.enabled = false;
        }

        if (showDebugLogs)
            Debug.Log($"{gameObject.name} health initialized: {currentHealth}/{maxHealth}");
    }

    void SetupHealthBar()
    {
        if (healthBarParent == null || healthPipPrefab == null) return;

        // Clear existing pips
        foreach (Transform child in healthBarParent)
        {
            Destroy(child.gameObject);
        }

        // Create pips
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
            {
                healthPips[i].SetActive(i < currentHealth);
            }
        }
    }

    public void TakeDamage(int amount, Vector2 attackDirection)
    {
        if (isDead) return;

        // Check if shield blocks the attack
        if (hasShield && ShouldBlock(attackDirection))
        {
            BlockAttack();
            return;
        }

        // Take damage
        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);

        if (showDebugLogs)
            Debug.Log($"<color=orange>{gameObject.name} took {amount} damage! Health: {currentHealth}/{maxHealth}</color>");

        UpdateHealthDisplay();

        // Visual feedback
        StartCoroutine(DamageFlash());

        // Audio
        if (audioSource != null && damageSound != null)
        {
            audioSource.PlayOneShot(damageSound);
        }

        // Check death
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Overload for backwards compatibility (no direction)
    public void TakeDamage(int amount)
    {
        TakeDamage(amount, Vector2.zero);
    }

    bool ShouldBlock(Vector2 attackDirection)
    {
        if (!hasShield) return false;

        // Calculate current block chance based on health
        float healthPercent = (float)currentHealth / maxHealth;
        float currentBlockChance = Mathf.Lerp(blockChanceAtLowHealth, blockChanceAtFullHealth, healthPercent);

        // Check if attack is from the front
        bool isFromFront = true;
        if (attackDirection != Vector2.zero)
        {
            // Get enemy's forward direction
            Vector2 facingDirection = spriteRenderer != null && spriteRenderer.flipX ? Vector2.left : Vector2.right;
            
            // Calculate angle between facing direction and attack direction
            float angle = Vector2.Angle(facingDirection, -attackDirection);
            
            isFromFront = angle <= blockAngle;

            if (showDebugLogs)
                Debug.Log($"Attack angle: {angle:F1}° (Block angle: {blockAngle}°) - {(isFromFront ? "FRONT" : "SIDE/BACK")}");
        }

        // Only block if from front AND random chance succeeds
        bool blocked = isFromFront && Random.value < currentBlockChance;

        if (showDebugLogs)
        {
            Debug.Log($"Block check - Health: {healthPercent:P0}, Chance: {currentBlockChance:P0}, From Front: {isFromFront}, Result: {(blocked ? "BLOCKED" : "HIT")}");
        }

        return blocked;
    }

    void BlockAttack()
    {
        isBlocking = true;

        if (showDebugLogs)
            Debug.Log($"<color=cyan>★ {gameObject.name} BLOCKED the attack!</color>");

        // Visual feedback
        StartCoroutine(BlockFlash());

        // Audio
        if (audioSource != null && blockSound != null)
        {
            audioSource.PlayOneShot(blockSound);
        }

        isBlocking = false;
    }

    IEnumerator DamageFlash()
    {
        if (spriteRenderer == null) yield break;

        Color original = spriteRenderer.color;
        
        // Flash damage color multiple times for better visibility
        for (int i = 0; i < 2; i++)
        {
            spriteRenderer.color = damageColor;
            yield return new WaitForSeconds(flashDuration / 2);
            spriteRenderer.color = original;
            yield return new WaitForSeconds(flashDuration / 2);
        }
        
        spriteRenderer.color = original;
    }

    IEnumerator BlockFlash()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = blockColor;
        }

        // Show shield sprite
        if (shieldSprite != null && flashShieldOnBlock)
        {
            shieldSprite.enabled = true;
        }

        yield return new WaitForSeconds(flashDuration);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        // Hide shield sprite
        if (shieldSprite != null && flashShieldOnBlock)
        {
            shieldSprite.enabled = false;
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (showDebugLogs)
            Debug.Log($"<color=red>{gameObject.name} died!</color>");

        // Audio
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        // Disable AI
        SimpleEnemyAI ai = GetComponent<SimpleEnemyAI>();
        if (ai != null)
        {
            ai.enabled = false;
        }

        // Disable collider
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Notify level manager
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.EnemyDefeated();
        }

        if (destroyOnDeath)
        {
            Destroy(gameObject, deathDelay);
        }
    }

    // Public accessors
    public int GetHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public bool IsDead() => isDead;
    public float GetHealthPercent() => (float)currentHealth / maxHealth;

    // Gizmo to show block angle
    void OnDrawGizmosSelected()
    {
        if (!hasShield) return;

        Vector2 facingDir = spriteRenderer != null && spriteRenderer.flipX ? Vector2.left : Vector2.right;
        
        // Draw block arc
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        
        Vector3 pos = transform.position;
        float radius = 2f;
        
        // Draw arc showing block angle
        for (float angle = -blockAngle; angle <= blockAngle; angle += 10f)
        {
            float rad1 = (Mathf.Atan2(facingDir.y, facingDir.x) + angle * Mathf.Deg2Rad);
            float rad2 = (Mathf.Atan2(facingDir.y, facingDir.x) + (angle + 10f) * Mathf.Deg2Rad);
            
            Vector3 p1 = pos + new Vector3(Mathf.Cos(rad1), Mathf.Sin(rad1)) * radius;
            Vector3 p2 = pos + new Vector3(Mathf.Cos(rad2), Mathf.Sin(rad2)) * radius;
            
            Gizmos.DrawLine(pos, p1);
            Gizmos.DrawLine(p1, p2);
        }
    }
}
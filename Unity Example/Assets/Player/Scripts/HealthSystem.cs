using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health")]
    public int maxHealth = 5;
    
    [Tooltip("Current health (read only)")]
    [SerializeField] private int currentHealth;

    [Header("Invincibility")]
    [Tooltip("Time invincible after taking damage")]
    public float invincibilityDuration = 1f;
    
    [Tooltip("Flash speed during invincibility")]
    public float flashSpeed = 0.1f;
    
    private bool isInvincible = false;

    [Header("Visual Feedback")]
    public SpriteRenderer spriteRenderer;
    
    [Tooltip("Color to flash when taking damage")]
    public Color damageColor = Color.red;
    
    [Tooltip("How long damage flash lasts")]
    public float damageFlashDuration = 0.2f;

    [Header("Health Display")]
    [Tooltip("Parent transform for health UI pips")]
    public Transform healthBarParent;
    
    [Tooltip("Prefab for each health pip")]
    public GameObject healthPipPrefab;
    
    private GameObject[] healthPips;

    [Header("Death Settings")]
    [Tooltip("Delay before respawning after death")]
    public float deathDelay = 1.5f;
    
    [Tooltip("Use LevelManager to restart, or reload scene directly")]
    public bool useLevelManager = true;

    [Header("Death Animation (Mario Style)")]
    [Tooltip("Enable Mario-style fall death")]
    public bool enableDeathAnimation = true;
    
    [Tooltip("Upward jump force on death")]
    public float deathJumpForce = 10f;
    
    [Tooltip("How long to freeze time/camera on death")]
    public float deathFreezeTime = 0.5f;

    [Header("Audio (Optional)")]
    public AudioClip damageSound;
    public AudioClip deathSound;
    public AudioClip healSound;
    private AudioSource audioSource;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Color originalColor;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        
        // Auto-find sprite renderer if not assigned
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
        if (audioSource == null && (damageSound != null || deathSound != null || healSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Setup health bar
        SetupHealthBar();

        if (showDebugLogs)
            Debug.Log($"Player health initialized: {currentHealth}/{maxHealth}");
    }

    void SetupHealthBar()
    {
        if (healthBarParent == null || healthPipPrefab == null) return;

        // Clear existing pips
        foreach (Transform child in healthBarParent)
        {
            Destroy(child.gameObject);
        }

        // Create new pips
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

    public void TakeDamage(int amount)
    {
        if (isDead || isInvincible) return;

        // CHECK FOR PARRY FIRST!
        PlayerCombat2D combat = GetComponent<PlayerCombat2D>();
        if (combat != null && combat.TryParry(Vector2.zero))
        {
            // Parry successful! No damage taken
            if (showDebugLogs)
                Debug.Log("<color=cyan>★ DAMAGE PARRIED!</color>");
            return;
        }

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);

        if (showDebugLogs)
            Debug.Log($"<color=orange>Player took {amount} damage! Health: {currentHealth}/{maxHealth}</color>");

        // Update health display
        UpdateHealthDisplay();

        // Visual feedback
        StartCoroutine(DamageFlash());

        // Audio feedback
        if (audioSource != null && damageSound != null)
        {
            audioSource.PlayOneShot(damageSound);
        }

        // Start invincibility
        if (currentHealth > 0)
        {
            StartCoroutine(InvincibilityCoroutine());
        }

        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator DamageFlash()
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.color = damageColor;
        yield return new WaitForSeconds(damageFlashDuration);
        spriteRenderer.color = originalColor;
    }

    IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        float elapsedTime = 0f;

        // Flash sprite during invincibility
        while (elapsedTime < invincibilityDuration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
            }
            
            yield return new WaitForSeconds(flashSpeed);
            elapsedTime += flashSpeed;
        }

        // Make sure sprite is visible at the end
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = originalColor;
        }

        isInvincible = false;
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        int oldHealth = currentHealth;
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        int actualHealing = currentHealth - oldHealth;

        if (actualHealing > 0)
        {
            if (showDebugLogs)
                Debug.Log($"<color=green>Player healed {actualHealing} HP! Health: {currentHealth}/{maxHealth}</color>");

            UpdateHealthDisplay();

            // Audio feedback
            if (audioSource != null && healSound != null)
            {
                audioSource.PlayOneShot(healSound);
            }
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (showDebugLogs)
            Debug.Log("<color=red>Player died!</color>");

        // Stop invincibility flashing
        StopAllCoroutines();
        
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        // Notify mana system to stop pulsing
        PlayerMana manaSystem = GetComponent<PlayerMana>();
        if (manaSystem != null)
        {
            manaSystem.OnPlayerDeath();
        }

        // Audio feedback
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        // Disable player controls
        var playerController = GetComponent<MonoBehaviour>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // Disable combat
        var playerCombat = GetComponent<PlayerCombat2D>();
        if (playerCombat != null)
        {
            playerCombat.enabled = false;
        }

        if (enableDeathAnimation)
        {
            StartCoroutine(MarioStyleDeath());
        }
        else
        {
            // Reset after delay
            Invoke(nameof(ResetLevel), deathDelay);
        }
    }

    System.Collections.IEnumerator MarioStyleDeath()
    {
        // Freeze time briefly
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(deathFreezeTime);
        Time.timeScale = 1f;

        // Disable collisions
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Get or add rigidbody
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        // Jump up then fall
        rb.gravityScale = 2f;
        rb.linearVelocity = new Vector2(0, deathJumpForce);

        // Wait for player to fall off screen
        yield return new WaitForSeconds(deathDelay);

        // Reset
        ResetLevel();
    }

    void ResetLevel()
    {
        if (useLevelManager && LevelManager.Instance != null)
        {
            LevelManager.Instance.RestartCurrentLevel();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    // Public methods for external access
    public int GetHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public bool IsDead() => isDead;
    public bool IsInvincible() => isInvincible;

    public void SetHealth(int newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        UpdateHealthDisplay();
    }

    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        SetupHealthBar();
    }

    // Reset for respawning
    public void ResetHealth()
    {
        isDead = false;
        isInvincible = false;
        currentHealth = maxHealth;
        UpdateHealthDisplay();
        
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = originalColor;
        }
    }
}
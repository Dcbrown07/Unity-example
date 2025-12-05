using UnityEngine;
using System.Collections;

public class LaserBeam : MonoBehaviour
{
    [Header("Laser Settings")]
    public float warningTime = 1f;
    public float activeTime = 2f;
    public int damage = 1;
    public float damageCooldown = 0.5f;
    public GameObject boss; // Reference to the boss that spawned this laser
    
    [Header("Visual Colors")]
    public Color warningColor = new Color(1f, 1f, 0f, 0.3f); // Yellow transparent
    public Color activeColor = new Color(1f, 0f, 0f, 0.8f); // Red semi-transparent
    
    [Header("Animation")]
    public Animator animator;
    public string warningAnimationTrigger = "Warning";
    public string activeAnimationTrigger = "Active";
    
    private SpriteRenderer sprite;
    private BoxCollider2D laserCollider;
    private bool isActive = false;
    private float lastDamageTime = -999f;
    
    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        laserCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
        
        if (sprite == null)
        {
            sprite = gameObject.AddComponent<SpriteRenderer>();
        }
        
        if (laserCollider == null)
        {
            laserCollider = gameObject.AddComponent<BoxCollider2D>();
            laserCollider.isTrigger = true;
        }
        
        // Set collider to match the laser beam size
        laserCollider.size = new Vector2(1f, 1f);
        laserCollider.offset = Vector2.zero; // Centered on the laser sprite
        
        // Ignore collision with boss
        if (boss != null)
        {
            Collider2D bossCollider = boss.GetComponent<Collider2D>();
            if (bossCollider != null)
            {
                Physics2D.IgnoreCollision(laserCollider, bossCollider, true);
            }
        }
        
        StartCoroutine(LaserSequence());
    }
    
    IEnumerator LaserSequence()
    {
        // WARNING PHASE - Yellow, no damage, pulsing
        if (sprite != null)
        {
            sprite.color = warningColor;
        }
        
        if (laserCollider != null)
        {
            laserCollider.enabled = false; // No collision during warning
        }
        
        // Trigger warning animation if animator exists
        if (animator != null && !string.IsNullOrEmpty(warningAnimationTrigger))
        {
            animator.SetTrigger(warningAnimationTrigger);
        }
        
        // Pulsing warning effect
        float elapsedTime = 0f;
        while (elapsedTime < warningTime)
        {
            if (sprite != null)
            {
                float pulse = Mathf.PingPong(Time.time * 3f, 1f);
                sprite.color = Color.Lerp(warningColor, new Color(1f, 0.5f, 0f, 0.5f), pulse);
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        Debug.Log("Laser warning complete!");
        
        // ACTIVE PHASE - Red, deals damage
        if (sprite != null)
        {
            sprite.color = activeColor;
        }
        
        if (laserCollider != null)
        {
            laserCollider.enabled = true; // Enable collision
        }
        
        // Trigger active animation if animator exists
        if (animator != null && !string.IsNullOrEmpty(activeAnimationTrigger))
        {
            animator.SetTrigger(activeAnimationTrigger);
        }
        
        isActive = true;
        Debug.Log("Laser active!");
        
        yield return new WaitForSeconds(activeTime);
        
        // Destroy laser
        Destroy(gameObject);
    }
    
    void OnTriggerStay2D(Collider2D other)
    {
        if (!isActive) return;
        
        if (other.CompareTag("Player"))
        {
            if (Time.time >= lastDamageTime + damageCooldown)
            {
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                    lastDamageTime = Time.time;
                    Debug.Log($"Laser hit player for {damage} damage!");
                }
            }
        }
    }
    
    void OnDrawGizmos()
    {
        // Draw the laser hitbox in editor AND in play mode
        if (laserCollider != null)
        {
            Gizmos.color = isActive ? Color.red : Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(laserCollider.offset, laserCollider.size);
            
            // Also draw a filled version so it's visible
            Gizmos.color = isActive ? new Color(1, 0, 0, 0.3f) : new Color(1, 1, 0, 0.3f);
            Gizmos.DrawCube(laserCollider.offset, laserCollider.size);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw even more detail when selected
        if (laserCollider != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(laserCollider.offset, laserCollider.size);
        }
    }
}
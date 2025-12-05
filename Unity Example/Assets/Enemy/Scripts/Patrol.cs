using UnityEngine;

public class PatrolEnemy : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float patrolDistance = 5f;
    
    [Header("Combat")]
    public int contactDamage = 1;
    public float damageCooldown = 1f;
    
    [Header("Visuals")]
    public Animator animator;
    public SpriteRenderer sprite;
    
    private Vector2 startPosition;
    private bool movingRight = true;
    private float lastDamageTime = -999f;
    
    void Start()
    {
        startPosition = transform.position;
        
        if (animator == null) animator = GetComponent<Animator>();
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();
        
        // Debug check for animator
        if (animator != null)
        {
            Debug.Log("Animator found! Make sure you have a bool parameter called 'isWalking' in your Animator Controller");
        }
        else
        {
            Debug.LogWarning("No Animator found on " + gameObject.name);
        }
    }
    
    void Update()
    {
        // Move back and forth
        if (movingRight)
        {
            transform.Translate(Vector2.right * moveSpeed * Time.deltaTime);
            if (sprite != null) sprite.flipX = true; // Face right (flipped because sprite faces left by default)
            
            // Check if reached right boundary
            if (transform.position.x >= startPosition.x + patrolDistance)
            {
                movingRight = false;
            }
        }
        else
        {
            transform.Translate(Vector2.left * moveSpeed * Time.deltaTime);
            if (sprite != null) sprite.flipX = false; // Face left (not flipped - sprite's default direction)
            
            // Check if reached left boundary
            if (transform.position.x <= startPosition.x - patrolDistance)
            {
                movingRight = true;
            }
        }
        
        // Set walk animation
        if (animator != null)
        {
            try
            {
                animator.SetBool("isWalking", true);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Animation parameter 'isWalking' not found! Create a bool parameter called 'isWalking' in your Animator Controller. Error: " + e.Message);
                animator = null; // Stop trying after first error
            }
        }
    }
    
    void OnCollisionStay2D(Collision2D collision)
    {
        // Check if it's the player
        if (collision.gameObject.CompareTag("Player"))
        {
            // Check cooldown
            if (Time.time >= lastDamageTime + damageCooldown)
            {
                PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(contactDamage);
                    lastDamageTime = Time.time;
                    Debug.Log($"Enemy damaged player for {contactDamage}!");
                }
            }
        }
    }
    
    // Also support trigger colliders
    void OnTriggerStay2D(Collider2D other)
    {
        // Check if it's the player
        if (other.CompareTag("Player"))
        {
            // Check cooldown
            if (Time.time >= lastDamageTime + damageCooldown)
            {
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(contactDamage);
                    lastDamageTime = Time.time;
                    Debug.Log($"Enemy damaged player for {contactDamage}!");
                }
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw patrol range
        Vector2 center = Application.isPlaying ? startPosition : (Vector2)transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(center + Vector2.left * patrolDistance, center + Vector2.right * patrolDistance);
        Gizmos.DrawWireSphere(center + Vector2.left * patrolDistance, 0.3f);
        Gizmos.DrawWireSphere(center + Vector2.right * patrolDistance, 0.3f);
    }
}
using UnityEngine;
using System.Collections;

public class LaserBeam : MonoBehaviour
{
    [Header("Laser Settings")]
    public float warningTime = 1f;
    public float activeTime = 2f;
    public int damage = 1;
    public float damageCooldown = 0.5f;
    
    [Header("Visual Colors")]
    public Color warningColor = new Color(1f, 1f, 0f, 0.5f); // Yellow transparent
    public Color activeColor = new Color(1f, 0f, 0f, 1f); // Red solid
    
    private SpriteRenderer sprite;
    private BoxCollider2D laserCollider;
    private bool isActive = false;
    private float lastDamageTime = -999f;
    
    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        laserCollider = GetComponent<BoxCollider2D>();
        
        if (sprite == null)
        {
            sprite = gameObject.AddComponent<SpriteRenderer>();
        }
        
        if (laserCollider == null)
        {
            laserCollider = gameObject.AddComponent<BoxCollider2D>();
            laserCollider.isTrigger = true;
        }
        
        StartCoroutine(LaserSequence());
    }
    
    IEnumerator LaserSequence()
    {
        // WARNING PHASE - Yellow, no damage
        if (sprite != null)
        {
            sprite.color = warningColor;
        }
        
        if (laserCollider != null)
        {
            laserCollider.enabled = false; // No collision during warning
        }
        
        Debug.Log("Laser warning!");
        yield return new WaitForSeconds(warningTime);
        
        // ACTIVE PHASE - Red, deals damage
        if (sprite != null)
        {
            sprite.color = activeColor;
        }
        
        if (laserCollider != null)
        {
            laserCollider.enabled = true; // Enable collision
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
}
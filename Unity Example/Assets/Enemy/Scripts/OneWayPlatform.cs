using UnityEngine;

public class OneWayPlatform : MonoBehaviour
{
    [Header("Platform Settings")]
    [Tooltip("The platform collider")]
    public PlatformEffector2D platformEffector;
    
    [Header("Drop Through Settings")]
    [Tooltip("Key to hold to drop through platform")]
    public KeyCode dropThroughKey = KeyCode.S;
    
    [Tooltip("How long to disable collision when dropping through")]
    public float dropThroughTime = 0.5f;
    
    private bool playerOnPlatform = false;
    private GameObject player;
    
    void Start()
    {
        // Auto-setup the platform effector
        if (platformEffector == null)
        {
            platformEffector = GetComponent<PlatformEffector2D>();
        }
        
        if (platformEffector == null)
        {
            platformEffector = gameObject.AddComponent<PlatformEffector2D>();
        }
        
        // Configure effector for one-way platform
        platformEffector.useOneWay = true;
        platformEffector.surfaceArc = 180f; // Only solid from top
        
        // Make sure we have a collider
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            BoxCollider2D boxCol = gameObject.AddComponent<BoxCollider2D>();
        }
        
        // Set collider to use platform effector
        col = GetComponent<Collider2D>();
        col.usedByEffector = true;
    }
    
    void Update()
    {
        // Check if player wants to drop through (just press down while on platform)
        if (playerOnPlatform && Input.GetKeyDown(dropThroughKey))
        {
            StartCoroutine(DisableCollision());
        }
    }
    
    System.Collections.IEnumerator DisableCollision()
    {
        if (player == null) yield break;
        
        Collider2D platformCollider = GetComponent<Collider2D>();
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        
        if (platformCollider != null && playerCollider != null)
        {
            // Ignore collision temporarily
            Physics2D.IgnoreCollision(playerCollider, platformCollider, true);
            
            yield return new WaitForSeconds(dropThroughTime);
            
            // Re-enable collision
            Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerOnPlatform = true;
            player = collision.gameObject;
        }
    }
    
    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerOnPlatform = false;
            player = null;
        }
    }
}
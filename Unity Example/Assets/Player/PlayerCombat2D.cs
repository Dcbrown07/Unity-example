using UnityEngine;
using System.Collections;

public class PlayerCombat2D : MonoBehaviour
{
    [Header("References")]
    public Transform wand; // Wand transform
    public SpriteRenderer wandSprite; // For flipping
    public GameObject fireballPrefab; // Fireball prefab

    [Header("Fireball Settings")]
    public float fireballForce = 10f;

    [Header("Wand Orbit Settings")]
    public float wandDistance = 1.5f;      // Distance from player
    public float orbitSpeed = 8f;          // How fast wand moves to target angle
    
    [Header("Collision Settings")]
    public LayerMask collisionMask;        // Assign walls/floor layer here
    public float collisionRadius = 0.2f;

    [Header("Parry Settings")]
    public float parryWindow = 0.3f;
    private bool isParrying = false;

    private Camera cam;
    private float currentAngle = 0f; // Current wand angle around player

    void Start()
    {
        cam = Camera.main;
        
        // Initialize wand position
        if (wand != null)
        {
            Vector2 initialPos = (Vector2)transform.position + Vector2.up * wandDistance;
            wand.position = initialPos;
            currentAngle = 90f; // Start pointing up
        }
    }

    void Update()
    {
        HandleWandOrbit();
        HandleShooting();

        if (Input.GetMouseButtonDown(1))
        {
            StartCoroutine(Parry());
        }
    }

    void HandleWandOrbit()
    {
        if (wand == null || cam == null) return;

        // Get mouse position relative to screen center
        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Vector3 mouseOffset = mouseScreenPos - screenCenter;
        
        // Convert screen offset to world offset
        Vector3 worldOffset = cam.ScreenToWorldPoint(screenCenter + mouseOffset) - cam.ScreenToWorldPoint(screenCenter);
        
        Vector2 playerPos = transform.position;
        
        // Calculate target angle from offset direction
        Vector2 mouseDirection = worldOffset.normalized;
        if (mouseDirection.magnitude < 0.1f) mouseDirection = Vector2.up; // Default direction
        
        float targetAngle = Mathf.Atan2(mouseDirection.y, mouseDirection.x) * Mathf.Rad2Deg;
        
        // Smoothly rotate to target angle
        currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, orbitSpeed * Time.deltaTime);
        
        // Convert angle to direction
        Vector2 direction = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), 
                                       Mathf.Sin(currentAngle * Mathf.Deg2Rad));
        
        // Calculate target position
        Vector2 targetPos = playerPos + direction * wandDistance;
        
        // Check for collisions and adjust position if needed
        RaycastHit2D hit = Physics2D.CircleCast(playerPos, collisionRadius, direction, wandDistance, collisionMask);
        if (hit.collider != null)
        {
            // Move wand just before the collision point
            float safeDistance = hit.distance - 0.1f;
            targetPos = playerPos + direction * Mathf.Max(safeDistance, 0.5f);
        }
        
        // Set wand position
        wand.position = targetPos;
        
        // Rotate wand to face away from player (pointing outward)
        wand.rotation = Quaternion.Euler(0, 0, currentAngle);
        
        // Flip sprite based on which side of player the wand is on
        if (wandSprite != null)
        {
            wandSprite.flipY = currentAngle > 90f && currentAngle < 270f;
        }
    }

    void HandleShooting()
    {
        if (Input.GetMouseButtonDown(0)) // Left click
        {
            if (fireballPrefab == null || wand == null) return;

            // Use the same offset method for consistent shooting
            Vector3 mouseScreenPos = Input.mousePosition;
            Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            Vector3 mouseOffset = mouseScreenPos - screenCenter;
            Vector3 worldOffset = cam.ScreenToWorldPoint(screenCenter + mouseOffset) - cam.ScreenToWorldPoint(screenCenter);
            
            Vector2 wandPos = wand.position;
            Vector2 shootDirection = worldOffset.normalized;
            
            // Spawn fireball slightly in front of wand
            Vector2 spawnPos = wandPos + shootDirection * 0.3f;
            
            GameObject fireball = Instantiate(fireballPrefab, spawnPos, Quaternion.identity);
            
            // Set up collision ignoring
            SetupFireballCollisions(fireball);
            
            // Launch fireball
            Rigidbody2D rb = fireball.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.AddForce(shootDirection * fireballForce, ForceMode2D.Impulse);
            }
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        // Simple and reliable method for following cameras
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(cam.transform.position.z);
        return cam.ScreenToWorldPoint(mousePos);
    }

    void SetupFireballCollisions(GameObject fireball)
    {
        Collider2D fireballCollider = fireball.GetComponent<Collider2D>();
        if (fireballCollider == null) return;
        
        // Ignore collision with player
        Collider2D playerCollider = GetComponent<Collider2D>();
        if (playerCollider != null)
            Physics2D.IgnoreCollision(fireballCollider, playerCollider);
        
        // Ignore collision with wand
        Collider2D wandCollider = wand.GetComponent<Collider2D>();
        if (wandCollider != null)
            Physics2D.IgnoreCollision(fireballCollider, wandCollider);
    }

    IEnumerator Parry()
    {
        isParrying = true;
        Debug.Log("Parry started!");
        
        // You can add visual/audio effects here
        
        yield return new WaitForSeconds(parryWindow);
        
        isParrying = false;
        Debug.Log("Parry ended.");
    }

    public bool IsParrying()
    {
        return isParrying;
    }

    // Optional: Visualize the orbit radius in scene view
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, wandDistance);
            
            if (wand != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, wand.position);
            }
        }
    }
}
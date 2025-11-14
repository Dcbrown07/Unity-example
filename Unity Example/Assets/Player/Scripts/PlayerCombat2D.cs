using UnityEngine;
using System.Collections;

public class PlayerCombat2D : MonoBehaviour
{
    [Header("References")]
    public Camera cam;              // Exposed camera reference
    public Transform wand;          // Wand transform
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

    private float currentAngle = 0f; // Current wand angle around player

    void Start()
    {
        // Initialize wand position
        if (wand != null)
        {
            Vector2 initialPos = (Vector2)transform.position + Vector2.up * wandDistance;
            wand.position = initialPos;
            currentAngle = 90f; // Start pointing up
        }

        // Safety check for camera
        if (cam == null)
        {
            Debug.LogError("Camera not assigned in PlayerCombat2D! Please assign it in the prefab.");
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

        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Vector3 mouseOffset = mouseScreenPos - screenCenter;
        
        Vector3 worldOffset = cam.ScreenToWorldPoint(screenCenter + mouseOffset) 
                            - cam.ScreenToWorldPoint(screenCenter);
        
        Vector2 playerPos = transform.position;
        Vector2 mouseDirection = worldOffset.normalized;
        if (mouseDirection.magnitude < 0.1f) mouseDirection = Vector2.up;

        float targetAngle = Mathf.Atan2(mouseDirection.y, mouseDirection.x) * Mathf.Rad2Deg;
        currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, orbitSpeed * Time.deltaTime);

        Vector2 direction = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), 
                                        Mathf.Sin(currentAngle * Mathf.Deg2Rad));
        Vector2 targetPos = playerPos + direction * wandDistance;

        RaycastHit2D hit = Physics2D.CircleCast(playerPos, collisionRadius, direction, wandDistance, collisionMask);
        if (hit.collider != null)
        {
            float safeDistance = hit.distance - 0.1f;
            targetPos = playerPos + direction * Mathf.Max(safeDistance, 0.5f);
        }

        wand.position = targetPos;
        wand.rotation = Quaternion.Euler(0, 0, currentAngle);

        if (wandSprite != null)
        {
            wandSprite.flipY = currentAngle > 90f && currentAngle < 270f;
        }
    }

    void HandleShooting()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (fireballPrefab == null || wand == null || cam == null) return;

            Vector3 mouseScreenPos = Input.mousePosition;
            Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            Vector3 mouseOffset = mouseScreenPos - screenCenter;
            Vector3 worldOffset = cam.ScreenToWorldPoint(screenCenter + mouseOffset) 
                                - cam.ScreenToWorldPoint(screenCenter);

            Vector2 wandPos = wand.position;
            Vector2 shootDirection = worldOffset.normalized;
            Vector2 spawnPos = wandPos + shootDirection * 0.3f;

            GameObject fireball = Instantiate(fireballPrefab, spawnPos, Quaternion.identity);
            SetupFireballCollisions(fireball);

            Rigidbody2D rb = fireball.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.AddForce(shootDirection * fireballForce, ForceMode2D.Impulse);
            }
        }
    }

    void SetupFireballCollisions(GameObject fireball)
    {
        Collider2D fireballCollider = fireball.GetComponent<Collider2D>();
        if (fireballCollider == null) return;

        Collider2D playerCollider = GetComponent<Collider2D>();
        if (playerCollider != null)
            Physics2D.IgnoreCollision(fireballCollider, playerCollider);

        if (wand != null)
        {
            Collider2D wandCollider = wand.GetComponent<Collider2D>();
            if (wandCollider != null)
                Physics2D.IgnoreCollision(fireballCollider, wandCollider);
        }
    }

    IEnumerator Parry()
    {
        isParrying = true;
        Debug.Log("Parry started!");
        yield return new WaitForSeconds(parryWindow);
        isParrying = false;
        Debug.Log("Parry ended.");
    }

    public bool IsParrying() => isParrying;

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

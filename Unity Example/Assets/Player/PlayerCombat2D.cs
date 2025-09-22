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
    public float wandFollowSpeed = 10f;    // Smoothness
    public float wandLag = 0.1f;           // How much the wand lags behind mouse

    [Header("Collision Settings")]
    public LayerMask collisionMask;        // Assign walls/floor layer here
    public float collisionRadius = 0.3f;

    [Header("Parry Settings")]
    public float parryWindow = 0.3f;
    private bool isParrying = false;

    private Camera cam;
    private Vector2 wandVelocity; // for SmoothDamp

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        HandleShooting();
        HandleWandOrbit();

        if (Input.GetMouseButtonDown(1))
        {
            StartCoroutine(Parry());
        }
    }

    void HandleShooting()
    {
        if (Input.GetMouseButtonDown(0)) // Left click
        {
            if (fireballPrefab == null || wand == null) return;

            Vector3 mousePos3D = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos = mousePos3D;

            // Direction from wand tip to mouse
            Vector2 wandTipPos = wand.position;
            Vector2 direction = (mousePos - wandTipPos).normalized;

            // Spawn slightly in front of wand tip
            Vector2 spawnPos = wandTipPos + direction * 0.2f;

            GameObject fireball = Instantiate(fireballPrefab, spawnPos, Quaternion.identity);

            // Ignore player collision
            Collider2D orbCollider = fireball.GetComponent<Collider2D>();
            Collider2D playerCollider = GetComponent<Collider2D>();
            if (orbCollider != null && playerCollider != null)
                Physics2D.IgnoreCollision(orbCollider, playerCollider);

            // Ignore wand collision
            Collider2D wandCollider = wand.GetComponent<Collider2D>();
            if (orbCollider != null && wandCollider != null)
                Physics2D.IgnoreCollision(orbCollider, wandCollider);

            Rigidbody2D rb = fireball.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero; // reset velocity
                rb.AddForce(direction * fireballForce, ForceMode2D.Impulse);
            }
        }
    }

    void HandleWandOrbit()
    {
        if (wand == null) return;

        Vector3 mousePos3D = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos = mousePos3D;
        Vector2 playerPos = transform.position;

        // Direction from player to mouse
        Vector2 dir = (mousePos - playerPos).normalized;

        // Target wand position (orbit around player)
        Vector2 targetPos = playerPos + dir * wandDistance;

        // Collision check so wand doesn't go into walls/floor
        RaycastHit2D hit = Physics2D.CircleCast(playerPos, collisionRadius, dir, wandDistance, collisionMask);
        if (hit.collider != null)
        {
            targetPos = hit.point - dir * 0.1f;
        }

        // Smooth movement with lag effect
        wand.position = Vector2.SmoothDamp(wand.position, targetPos, ref wandVelocity, wandLag);

        // Calculate rotation toward mouse but clamp it around upward direction
        Vector2 lookDir = (mousePos - (Vector2)wand.position).normalized;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        angle = Mathf.Clamp(angle, -75f, 75f);
        wand.rotation = Quaternion.Lerp(wand.rotation, Quaternion.Euler(0, 0, angle), Time.deltaTime * wandFollowSpeed);

        // Flip wand sprite visually based on mouse side
        if (wandSprite != null)
            wandSprite.flipY = mousePos.x < playerPos.x;
    }

    IEnumerator Parry()
    {
        isParrying = true;
        Debug.Log("Parry started!");
        yield return new WaitForSeconds(parryWindow);
        isParrying = false;
        Debug.Log("Parry ended.");
    }

    public bool IsParrying()
    {
        return isParrying;
    }
}

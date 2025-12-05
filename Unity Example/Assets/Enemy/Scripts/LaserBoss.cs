using UnityEngine;
using System.Collections;

public class LaserBoss : MonoBehaviour
{
    [Header("Movement Boundaries")]
    public float minX = -8f;
    public float maxX = 8f;
    public float minY = 2f;
    public float maxY = 8f;
    
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float changeDirectionTime = 2f;
    private float nextDirectionChange;
    private Vector2 moveDirection;
    
    [Header("Laser Attacks")]
    public GameObject laserBeamPrefab; // Your laser sprite prefab
    public GameObject warningIndicatorPrefab; // Optional: visual warning line
    public Transform laserSpawnPoint; // Single point to shoot from (like mouth/eye)
    public float laserAttackCooldown = 3f;
    public float laserWarningTime = 1f; // Time before laser activates
    public float laserActiveTime = 2f; // How long laser stays active
    public int laserDamage = 1;
    public float laserWidth = 0.5f; // Width of the laser beam
    public float laserLength = 15f; // Length of the laser beam
    
    public enum AttackPattern { TrackPlayer, Sweep, Cross, Spray }
    
    [Header("Attack Patterns")]
    public AttackPattern currentPattern = AttackPattern.TrackPlayer;
    public bool randomizePatterns = true;
    
    [Header("Player Tracking")]
    public Transform player;
    
    [Header("Health")]
    public int maxHealth = 10;
    
    private int currentHealth;
    
    [Header("Visuals")]
    public SpriteRenderer sprite;
    public Animator animator;
    public Color damageFlashColor = Color.red;
    private Color originalColor;
    
    private float nextLaserAttack;
    private bool isAttacking = false;
    
    void Start()
    {
        currentHealth = maxHealth;
        nextDirectionChange = Time.time + changeDirectionTime;
        nextLaserAttack = Time.time + laserAttackCooldown;
        
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) originalColor = sprite.color;
        if (animator == null) animator = GetComponent<Animator>();
        
        // Find player
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
        
        // Random starting direction
        moveDirection = Random.insideUnitCircle.normalized;
    }
    
    void Update()
    {
        if (!isAttacking)
        {
            FlyAround();
        }
        
        // Check if time to attack
        if (Time.time >= nextLaserAttack && !isAttacking)
        {
            StartCoroutine(LaserAttack());
        }
    }
    
    void FlyAround()
    {
        // Move in current direction
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime);
        
        // Keep within boundaries
        Vector3 pos = transform.position;
        bool hitBoundary = false;
        
        if (pos.x < minX)
        {
            pos.x = minX;
            moveDirection.x = Mathf.Abs(moveDirection.x); // Bounce right
            hitBoundary = true;
        }
        else if (pos.x > maxX)
        {
            pos.x = maxX;
            moveDirection.x = -Mathf.Abs(moveDirection.x); // Bounce left
            hitBoundary = true;
        }
        
        if (pos.y < minY)
        {
            pos.y = minY;
            moveDirection.y = Mathf.Abs(moveDirection.y); // Bounce up
            hitBoundary = true;
        }
        else if (pos.y > maxY)
        {
            pos.y = maxY;
            moveDirection.y = -Mathf.Abs(moveDirection.y); // Bounce down
            hitBoundary = true;
        }
        
        transform.position = pos;
        
        // Change direction randomly
        if (Time.time >= nextDirectionChange || hitBoundary)
        {
            moveDirection = Random.insideUnitCircle.normalized;
            nextDirectionChange = Time.time + changeDirectionTime;
        }
        
        // Flip sprite based on movement
        if (sprite != null && moveDirection.x != 0)
        {
            sprite.flipX = moveDirection.x < 0;
        }
    }
    
    IEnumerator LaserAttack()
    {
        isAttacking = true;
        
        // Choose pattern
        if (randomizePatterns)
        {
            currentPattern = (AttackPattern)Random.Range(0, 4);
            Debug.Log($"Randomly chose pattern: {currentPattern}");
        }
        
        Debug.Log($"Boss using {currentPattern} laser pattern!");
        
        // Execute pattern
        switch (currentPattern)
        {
            case AttackPattern.TrackPlayer:
                yield return StartCoroutine(TrackPlayerAttack());
                break;
            case AttackPattern.Sweep:
                yield return StartCoroutine(SweepAttack());
                break;
            case AttackPattern.Cross:
                yield return StartCoroutine(CrossAttack());
                break;
            case AttackPattern.Spray:
                yield return StartCoroutine(SprayAttack());
                break;
        }
        
        // Reset attack cooldown
        nextLaserAttack = Time.time + laserAttackCooldown;
        isAttacking = false;
    }
    
    IEnumerator TrackPlayerAttack()
    {
        if (player == null) yield break;
        
        // Aim at player
        Vector2 spawnPos = laserSpawnPoint != null ? laserSpawnPoint.position : transform.position;
        Vector2 direction = (player.position - (Vector3)spawnPos).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Offset spawn position away from boss in the direction of the laser
        Vector2 offset = direction * 2f; // Move 2 units away from boss
        spawnPos += offset;
        
        SpawnLaser(spawnPos, angle, laserLength, laserWidth);
        
        yield return new WaitForSeconds(laserWarningTime + laserActiveTime);
    }
    
    IEnumerator SweepAttack()
    {
        // Sweep from left to right
        for (int i = 0; i < 5; i++)
        {
            Vector2 spawnPos = laserSpawnPoint != null ? laserSpawnPoint.position : transform.position;
            float angle = -60f + (i * 30f); // -60 to 60 degrees
            
            // Offset spawn position
            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            spawnPos += direction * 2f;
            
            SpawnLaser(spawnPos, angle, laserLength, laserWidth);
            yield return new WaitForSeconds(0.3f);
        }
        
        yield return new WaitForSeconds(laserWarningTime + laserActiveTime);
    }
    
    IEnumerator CrossAttack()
    {
        Vector2 spawnPos = laserSpawnPoint != null ? laserSpawnPoint.position : transform.position;
        
        // Horizontal (right)
        SpawnLaser(spawnPos + Vector2.right * 2f, 0f, laserLength, laserWidth);
        // Horizontal (left)
        SpawnLaser(spawnPos + Vector2.left * 2f, 180f, laserLength, laserWidth);
        // Vertical (down)
        SpawnLaser(spawnPos + Vector2.down * 2f, -90f, laserLength, laserWidth);
        // Vertical (up)
        SpawnLaser(spawnPos + Vector2.up * 2f, 90f, laserLength, laserWidth);
        
        yield return new WaitForSeconds(laserWarningTime + laserActiveTime);
    }
    
    IEnumerator SprayAttack()
    {
        // Rapid fire in random directions
        for (int i = 0; i < 8; i++)
        {
            Vector2 spawnPos = laserSpawnPoint != null ? laserSpawnPoint.position : transform.position;
            float angle = Random.Range(0f, 360f);
            
            // Offset spawn position
            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            spawnPos += direction * 2f;
            
            SpawnLaser(spawnPos, angle, laserLength, laserWidth);
            yield return new WaitForSeconds(0.2f);
        }
        
        yield return new WaitForSeconds(laserWarningTime + laserActiveTime);
    }
    
    void SpawnLaser(Vector2 position, float angleDegrees, float length, float width)
    {
        if (laserBeamPrefab == null)
        {
            Debug.LogError("Laser beam prefab not assigned!");
            return;
        }
        
        GameObject laser = Instantiate(laserBeamPrefab, position, Quaternion.Euler(0, 0, angleDegrees));
        laser.transform.localScale = new Vector3(length, width, 1);
        
        LaserBeam laserScript = laser.GetComponent<LaserBeam>();
        if (laserScript != null)
        {
            laserScript.warningTime = laserWarningTime;
            laserScript.activeTime = laserActiveTime;
            laserScript.damage = laserDamage;
            laserScript.boss = gameObject; // Pass boss reference
        }
    }
    
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        StartCoroutine(DamageFlash());
        
        Debug.Log($"Boss health: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    IEnumerator DamageFlash()
    {
        if (sprite == null) yield break;
        sprite.color = damageFlashColor;
        yield return new WaitForSeconds(0.2f);
        sprite.color = originalColor;
    }
    
    void Die()
    {
        Debug.Log("Boss defeated!");
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.EnemyDefeated();
        }
        Destroy(gameObject);
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw battle area boundaries
        Gizmos.color = Color.yellow;
        Vector3 bottomLeft = new Vector3(minX, minY, 0);
        Vector3 bottomRight = new Vector3(maxX, minY, 0);
        Vector3 topRight = new Vector3(maxX, maxY, 0);
        Vector3 topLeft = new Vector3(minX, maxY, 0);
        
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
        
        // Draw laser spawn point
        if (laserSpawnPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(laserSpawnPoint.position, 0.3f);
        }
    }
}
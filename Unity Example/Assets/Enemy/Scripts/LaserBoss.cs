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
    public Transform[] laserSpawnPoints; // Multiple points to spawn lasers from
    public float laserAttackCooldown = 3f;
    public float laserWarningTime = 1f; // Time before laser activates
    public float laserActiveTime = 2f; // How long laser stays active
    public int laserDamage = 1;
    
    public enum AttackPattern { Horizontal, Vertical, Cross, Random }
    
    [Header("Attack Patterns")]
    public AttackPattern currentPattern = AttackPattern.Horizontal;
    public bool randomizePatterns = true;
    
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
        }
        
        Debug.Log($"Boss using {currentPattern} laser pattern!");
        
        // Stop moving during attack
        Vector3 attackPosition = transform.position;
        
        // Spawn lasers based on pattern
        switch (currentPattern)
        {
            case AttackPattern.Horizontal:
                SpawnHorizontalLasers();
                break;
            case AttackPattern.Vertical:
                SpawnVerticalLasers();
                break;
            case AttackPattern.Cross:
                SpawnCrossLasers();
                break;
            case AttackPattern.Random:
                SpawnRandomLasers();
                break;
        }
        
        // Wait for attack to finish
        yield return new WaitForSeconds(laserWarningTime + laserActiveTime);
        
        // Reset attack cooldown
        nextLaserAttack = Time.time + laserAttackCooldown;
        isAttacking = false;
    }
    
    void SpawnHorizontalLasers()
    {
        // Spawn 3 horizontal lasers at different Y positions
        for (int i = 0; i < 3; i++)
        {
            float yPos = minY + ((maxY - minY) / 4f) * (i + 1);
            SpawnLaser(new Vector3(0, yPos, 0), Vector3.zero, new Vector3(maxX - minX, 0.5f, 1));
        }
    }
    
    void SpawnVerticalLasers()
    {
        // Spawn 3 vertical lasers at different X positions
        for (int i = 0; i < 3; i++)
        {
            float xPos = minX + ((maxX - minX) / 4f) * (i + 1);
            SpawnLaser(new Vector3(xPos, (minY + maxY) / 2f, 0), new Vector3(0, 0, 90), new Vector3(maxY - minY, 0.5f, 1));
        }
    }
    
    void SpawnCrossLasers()
    {
        // Horizontal laser
        SpawnLaser(new Vector3(0, (minY + maxY) / 2f, 0), Vector3.zero, new Vector3(maxX - minX, 0.5f, 1));
        // Vertical laser
        SpawnLaser(new Vector3(0, (minY + maxY) / 2f, 0), new Vector3(0, 0, 90), new Vector3(maxY - minY, 0.5f, 1));
    }
    
    void SpawnRandomLasers()
    {
        // Spawn 5 random lasers
        for (int i = 0; i < 5; i++)
        {
            bool isHorizontal = Random.value > 0.5f;
            
            if (isHorizontal)
            {
                float yPos = Random.Range(minY, maxY);
                SpawnLaser(new Vector3(0, yPos, 0), Vector3.zero, new Vector3(maxX - minX, 0.5f, 1));
            }
            else
            {
                float xPos = Random.Range(minX, maxX);
                SpawnLaser(new Vector3(xPos, (minY + maxY) / 2f, 0), new Vector3(0, 0, 90), new Vector3(maxY - minY, 0.5f, 1));
            }
        }
    }
    
    void SpawnLaser(Vector3 position, Vector3 rotation, Vector3 scale)
    {
        if (laserBeamPrefab == null)
        {
            Debug.LogError("Laser beam prefab not assigned!");
            return;
        }
        
        GameObject laser = Instantiate(laserBeamPrefab, position, Quaternion.Euler(rotation));
        laser.transform.localScale = scale;
        
        LaserBeam laserScript = laser.GetComponent<LaserBeam>();
        if (laserScript != null)
        {
            laserScript.warningTime = laserWarningTime;
            laserScript.activeTime = laserActiveTime;
            laserScript.damage = laserDamage;
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
    }
}
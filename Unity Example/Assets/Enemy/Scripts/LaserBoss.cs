using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LaserBoss : MonoBehaviour
{
    [Header("Arena Boundaries (where lasers spawn)")]
    public float arenaMinX = -10f;
    public float arenaMaxX = 10f;
    public float arenaMinY = -5f;
    public float arenaMaxY = 5f;
    
    [Header("Boss Movement")]
    public float moveSpeed = 3f;
    public float changeDirectionTime = 2f;
    private float nextDirectionChange;
    private Vector2 moveDirection;
    
    [Header("Laser Maze Settings")]
    public GameObject laserBeamPrefab;
    public float laserAttackCooldown = 6f; // More time between attacks
    public float laserWarningTime = 2f; // More warning time
    public float laserActiveTime = 2f; // Less active time
    public int laserDamage = 1;
    public float laserWidth = 0.3f; // Thinner lasers
    
    [Header("Laser Patterns")]
    public int horizontalLaserCount = 5; // Fewer lasers
    public int verticalLaserCount = 5;
    public bool randomizePatterns = true;
    
    [Header("Debug")]
    public bool showLaserPreview = true; // Show where lasers will spawn in editor
    
    public enum MazePattern { Horizontal, Vertical, Grid, RandomMaze }
    public MazePattern currentPattern = MazePattern.Horizontal;
    
    [Header("Player Tracking")]
    public Transform player;
    
    [Header("Health")]
    public int maxHealth = 20;
    private int currentHealth;
    
    [Header("Vulnerable Phase")]
    public int attacksToDodge = 3; // Number of attacks player must dodge
    private int attacksDodged = 0;
    public float vulnerableTime = 5f; // How long boss is vulnerable
    public bool isVulnerable = false;
    
    [Header("Visuals")]
    public SpriteRenderer sprite;
    public Color damageFlashColor = Color.red;
    public Color vulnerableColor = Color.green; // Green when vulnerable
    public Color invulnerableColor = Color.gray; // Gray when invulnerable
    private Color originalColor;
    
    [Header("Damage Feedback")]
    public GameObject damageTextPrefab; // Optional: floating damage numbers
    public float damagePopupOffset = 1f;
    public bool screenShakeOnDamage = true;
    public float shakeIntensity = 0.3f;
    
    private float nextLaserAttack;
    private bool isAttacking = false;
    private List<GameObject> activeLasers = new List<GameObject>();
    
    void Start()
    {
        currentHealth = maxHealth;
        nextDirectionChange = Time.time + changeDirectionTime;
        nextLaserAttack = Time.time + laserAttackCooldown;
        
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            originalColor = sprite.color;
            sprite.color = invulnerableColor; // Start invulnerable
        }
        
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
        
        moveDirection = Random.insideUnitCircle.normalized;
        
        Debug.Log($"<color=yellow>Boss spawned! Dodge {attacksToDodge} laser attacks to make boss vulnerable!</color>");
    }
    
    void Update()
    {
        if (!isAttacking)
        {
            FlyAround();
        }
        
        if (Time.time >= nextLaserAttack && !isAttacking)
        {
            StartCoroutine(LaserMazeAttack());
        }
    }
    
    void FlyAround()
    {
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime);
        
        Vector3 pos = transform.position;
        bool hitBoundary = false;
        
        // Keep boss within arena
        if (pos.x < arenaMinX)
        {
            pos.x = arenaMinX;
            moveDirection.x = Mathf.Abs(moveDirection.x);
            hitBoundary = true;
        }
        else if (pos.x > arenaMaxX)
        {
            pos.x = arenaMaxX;
            moveDirection.x = -Mathf.Abs(moveDirection.x);
            hitBoundary = true;
        }
        
        if (pos.y < arenaMinY)
        {
            pos.y = arenaMinY;
            moveDirection.y = Mathf.Abs(moveDirection.y);
            hitBoundary = true;
        }
        else if (pos.y > arenaMaxY)
        {
            pos.y = arenaMaxY;
            moveDirection.y = -Mathf.Abs(moveDirection.y);
            hitBoundary = true;
        }
        
        transform.position = pos;
        
        if (Time.time >= nextDirectionChange || hitBoundary)
        {
            moveDirection = Random.insideUnitCircle.normalized;
            nextDirectionChange = Time.time + changeDirectionTime;
        }
        
        if (sprite != null && moveDirection.x != 0)
        {
            sprite.flipX = moveDirection.x < 0;
        }
    }
    
    IEnumerator LaserMazeAttack()
    {
        isAttacking = true;
        
        // Choose random pattern
        if (randomizePatterns)
        {
            currentPattern = (MazePattern)Random.Range(0, 4);
        }
        
        Debug.Log($"<color=yellow>Creating {currentPattern} laser maze!</color>");
        
        // Clear old lasers
        foreach (GameObject laser in activeLasers)
        {
            if (laser != null) Destroy(laser);
        }
        activeLasers.Clear();
        
        // Create the laser maze pattern
        switch (currentPattern)
        {
            case MazePattern.Horizontal:
                CreateHorizontalMaze();
                break;
            case MazePattern.Vertical:
                CreateVerticalMaze();
                break;
            case MazePattern.Grid:
                CreateGridMaze();
                break;
            case MazePattern.RandomMaze:
                CreateRandomMaze();
                break;
        }
        
        // Track if player got hit during this attack
        bool playerWasHit = false;
        float attackStartTime = Time.time;
        
        // Wait for warning + active time and check if player was hit
        while (Time.time < attackStartTime + laserWarningTime + laserActiveTime)
        {
            // Check if any laser hit the player
            foreach (GameObject laser in activeLasers)
            {
                if (laser != null)
                {
                    LaserBeam laserScript = laser.GetComponent<LaserBeam>();
                    if (laserScript != null && laserScript.hasHitPlayer)
                    {
                        playerWasHit = true;
                        break;
                    }
                }
            }
            yield return null;
        }
        
        // Check if player dodged successfully
        if (!playerWasHit)
        {
            attacksDodged++;
            Debug.Log($"<color=cyan>Player dodged! {attacksDodged}/{attacksToDodge} attacks dodged</color>");
            
            if (attacksDodged >= attacksToDodge && !isVulnerable)
            {
                StartCoroutine(BecomeVulnerable());
            }
        }
        else
        {
            Debug.Log("<color=red>Player was hit! Dodge counter reset.</color>");
            attacksDodged = 0;
        }
        
        // Clear lasers
        foreach (GameObject laser in activeLasers)
        {
            if (laser != null) Destroy(laser);
        }
        activeLasers.Clear();
        
        nextLaserAttack = Time.time + laserAttackCooldown;
        isAttacking = false;
    }
    
    IEnumerator BecomeVulnerable()
    {
        isVulnerable = true;
        attacksDodged = 0;
        
        Debug.Log($"<color=green>★ BOSS IS VULNERABLE! Attack now! ({vulnerableTime}s)</color>");
        
        // Change color to green
        if (sprite != null)
        {
            sprite.color = vulnerableColor;
        }
        
        // Pulse effect
        float pulseTimer = 0f;
        while (pulseTimer < vulnerableTime)
        {
            if (sprite != null && isVulnerable)
            {
                float pulse = Mathf.PingPong(Time.time * 3f, 1f);
                sprite.color = Color.Lerp(vulnerableColor, Color.white, pulse * 0.5f);
            }
            pulseTimer += Time.deltaTime;
            yield return null;
        }
        
        // Return to invulnerable
        isVulnerable = false;
        if (sprite != null)
        {
            sprite.color = invulnerableColor;
        }
        
        Debug.Log("<color=gray>Boss is invulnerable again!</color>");
    }
    
    void CreateHorizontalMaze()
    {
        float arenaHeight = arenaMaxY - arenaMinY;
        float arenaWidth = arenaMaxX - arenaMinX;
        
        // Leave MORE gaps - 40% of lasers
        List<int> skipIndices = new List<int>();
        int gapsToLeave = Mathf.Max(2, horizontalLaserCount * 2 / 5); // 40% gaps
        for (int i = 0; i < gapsToLeave; i++)
        {
            int randomIndex = Random.Range(0, horizontalLaserCount);
            if (!skipIndices.Contains(randomIndex))
            {
                skipIndices.Add(randomIndex);
            }
        }
        
        for (int i = 0; i < horizontalLaserCount; i++)
        {
            if (skipIndices.Contains(i)) continue;
            
            float t = i / (float)(horizontalLaserCount - 1);
            float yPos = Mathf.Lerp(arenaMinY, arenaMaxY, t);
            float xCenter = (arenaMinX + arenaMaxX) / 2f;
            
            SpawnLaser(new Vector2(xCenter, yPos), 0f, arenaWidth);
        }
    }
    
    void CreateVerticalMaze()
    {
        float arenaHeight = arenaMaxY - arenaMinY;
        float arenaWidth = arenaMaxX - arenaMinX;
        
        // Leave MORE gaps
        List<int> skipIndices = new List<int>();
        int gapsToLeave = Mathf.Max(2, verticalLaserCount * 2 / 5);
        for (int i = 0; i < gapsToLeave; i++)
        {
            int randomIndex = Random.Range(0, verticalLaserCount);
            if (!skipIndices.Contains(randomIndex))
            {
                skipIndices.Add(randomIndex);
            }
        }
        
        for (int i = 0; i < verticalLaserCount; i++)
        {
            if (skipIndices.Contains(i)) continue;
            
            float t = i / (float)(verticalLaserCount - 1);
            float xPos = Mathf.Lerp(arenaMinX, arenaMaxX, t);
            float yCenter = (arenaMinY + arenaMaxY) / 2f;
            
            SpawnLaser(new Vector2(xPos, yCenter), 90f, arenaHeight);
        }
    }
    
    void CreateGridMaze()
    {
        float arenaHeight = arenaMaxY - arenaMinY;
        float arenaWidth = arenaMaxX - arenaMinX;
        
        // Even fewer lasers for grid pattern
        int hCount = Mathf.Max(3, horizontalLaserCount / 2);
        int vCount = Mathf.Max(3, verticalLaserCount / 2);
        
        // Leave 2-3 gaps in each direction
        List<int> skipH = new List<int>();
        for (int i = 0; i < 2; i++)
        {
            skipH.Add(Random.Range(0, hCount));
        }
        
        List<int> skipV = new List<int>();
        for (int i = 0; i < 2; i++)
        {
            skipV.Add(Random.Range(0, vCount));
        }
        
        // Horizontal lines
        for (int i = 0; i < hCount; i++)
        {
            if (skipH.Contains(i)) continue;
            
            float t = i / (float)(hCount - 1);
            float yPos = Mathf.Lerp(arenaMinY, arenaMaxY, t);
            float xCenter = (arenaMinX + arenaMaxX) / 2f;
            
            SpawnLaser(new Vector2(xCenter, yPos), 0f, arenaWidth);
        }
        
        // Vertical lines
        for (int i = 0; i < vCount; i++)
        {
            if (skipV.Contains(i)) continue;
            
            float t = i / (float)(vCount - 1);
            float xPos = Mathf.Lerp(arenaMinX, arenaMaxX, t);
            float yCenter = (arenaMinY + arenaMaxY) / 2f;
            
            SpawnLaser(new Vector2(xPos, yCenter), 90f, arenaHeight);
        }
    }
    
    void CreateRandomMaze()
    {
        float arenaHeight = arenaMaxY - arenaMinY;
        float arenaWidth = arenaMaxX - arenaMinX;
        
        // Fewer random lasers
        int totalLasers = Mathf.Max(6, (horizontalLaserCount + verticalLaserCount) / 2);
        
        for (int i = 0; i < totalLasers; i++)
        {
            bool isHorizontal = Random.value > 0.5f;
            
            if (isHorizontal)
            {
                float yPos = Random.Range(arenaMinY, arenaMaxY);
                float xCenter = (arenaMinX + arenaMaxX) / 2f;
                
                SpawnLaser(new Vector2(xCenter, yPos), 0f, arenaWidth);
            }
            else
            {
                float xPos = Random.Range(arenaMinX, arenaMaxX);
                float yCenter = (arenaMinY + arenaMaxY) / 2f;
                
                SpawnLaser(new Vector2(xPos, yCenter), 90f, arenaHeight);
            }
        }
    }
    
    void SpawnLaser(Vector2 position, float angleDegrees, float length)
    {
        if (laserBeamPrefab == null)
        {
            Debug.LogError("Laser beam prefab not assigned!");
            return;
        }
        
        GameObject laser = Instantiate(laserBeamPrefab, position, Quaternion.Euler(0, 0, angleDegrees));
        laser.transform.localScale = new Vector3(length, laserWidth, 1);
        
        LaserBeam laserScript = laser.GetComponent<LaserBeam>();
        if (laserScript != null)
        {
            laserScript.warningTime = laserWarningTime;
            laserScript.activeTime = laserActiveTime;
            laserScript.damage = laserDamage;
            laserScript.boss = gameObject;
        }
        
        activeLasers.Add(laser);
    }
    
    public void TakeDamage(int amount)
    {
        Debug.Log($"<color=cyan>TakeDamage called! isVulnerable={isVulnerable}, amount={amount}</color>");
        
        // Only take damage when vulnerable
        if (!isVulnerable)
        {
            Debug.Log("<color=yellow>Boss is invulnerable! Dodge lasers first!</color>");
            // Visual feedback that it's invulnerable
            StartCoroutine(InvulnerableBounce());
            return;
        }
        
        currentHealth -= amount;
        
        // Visual feedback
        StartCoroutine(DamageFlash());
        StartCoroutine(DamageRecoil());
        
        // Spawn floating damage text
        if (damageTextPrefab != null)
        {
            Vector3 popupPos = transform.position + Vector3.up * damagePopupOffset;
            GameObject damageText = Instantiate(damageTextPrefab, popupPos, Quaternion.identity);
            // You can set the text if it has a TextMesh component
            TextMesh tm = damageText.GetComponent<TextMesh>();
            if (tm != null) tm.text = $"-{amount}";
        }
        
        // Screen shake
        if (screenShakeOnDamage)
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindObjectOfType<Camera>();
            if (cam != null)
            {
                StartCoroutine(ScreenShake(cam));
            }
            else
            {
                Debug.LogWarning("No camera found for screen shake!");
            }
        }
        
        Debug.Log($"<color=orange>★ BOSS DAMAGED! -{amount} HP | Health: {currentHealth}/{maxHealth}</color>");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    IEnumerator DamageFlash()
    {
        if (sprite == null) yield break;
        
        // Flash red multiple times
        for (int i = 0; i < 3; i++)
        {
            sprite.color = damageFlashColor;
            yield return new WaitForSeconds(0.1f);
            sprite.color = vulnerableColor;
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    IEnumerator DamageRecoil()
    {
        // Boss recoils backwards slightly
        Vector3 originalPos = transform.position;
        Vector3 recoilPos = originalPos + (Vector3)moveDirection * -0.5f;
        
        float elapsed = 0f;
        float duration = 0.2f;
        
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(originalPos, recoilPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Snap back
        elapsed = 0f;
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(recoilPos, originalPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = originalPos;
    }
    
    IEnumerator InvulnerableBounce()
    {
        // Small bounce to show it's invulnerable
        if (sprite == null) yield break;
        
        Vector3 originalScale = transform.localScale;
        Vector3 bounceScale = originalScale * 1.1f;
        
        float elapsed = 0f;
        float duration = 0.1f;
        
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(originalScale, bounceScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        elapsed = 0f;
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(bounceScale, originalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    IEnumerator ScreenShake(Camera cam)
    {
        if (cam == null) yield break;
        
        Vector3 originalPos = cam.transform.localPosition;
        float elapsed = 0f;
        float duration = 0.3f;
        
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float y = Random.Range(-1f, 1f) * shakeIntensity;
            
            cam.transform.localPosition = originalPos + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        cam.transform.localPosition = originalPos;
    }
    
    [Header("Death Effects")]
    public GameObject deathExplosion; // Optional: explosion prefab
    public float deathShakeIntensity = 1f;
    public float deathShakeDuration = 1f;
    public AudioClip deathSound;
    
    void Die()
    {
        Debug.Log("<color=red>★★★ BOSS DEFEATED! ★★★</color>");
        
        // MASSIVE explosion effect
        StartCoroutine(DeathSequence());
    }
    
    IEnumerator DeathSequence()
    {
        // Stop all attacks
        isAttacking = true; // Prevent new attacks
        
        // Flash rapidly
        if (sprite != null)
        {
            for (int i = 0; i < 10; i++)
            {
                sprite.color = Color.white;
                yield return new WaitForSeconds(0.05f);
                sprite.color = Color.red;
                yield return new WaitForSeconds(0.05f);
            }
        }
        
        // Play death sound
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }
        
        // MASSIVE CAMERA SHAKE
        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam != null)
        {
            StartCoroutine(MassiveScreenShake(cam));
        }
        
        // Spawn multiple explosions
        if (deathExplosion != null)
        {
            // Spawn 5 explosions around the boss using assigned prefab
            for (int i = 0; i < 5; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 2f;
                Instantiate(deathExplosion, (Vector2)transform.position + offset, Quaternion.identity);
                yield return new WaitForSeconds(0.1f);
            }
        }
        else
        {
            // Create procedural explosions with code
            for (int i = 0; i < 5; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 2f;
                GameObject explosion = new GameObject("Explosion");
                explosion.transform.position = (Vector2)transform.position + offset;
                explosion.AddComponent<ProceduralExplosion>();
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        // Wait for shake to finish
        yield return new WaitForSeconds(deathShakeDuration);
        
        // Clear all lasers
        foreach (GameObject laser in activeLasers)
        {
            if (laser != null) Destroy(laser);
        }
        
        // Notify level manager
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.EnemyDefeated();
        }
        
        // Destroy boss
        Destroy(gameObject);
    }
    
    IEnumerator MassiveScreenShake(Camera cam)
    {
        if (cam == null) yield break;
        
        Vector3 originalPos = cam.transform.localPosition;
        float elapsed = 0f;
        
        while (elapsed < deathShakeDuration)
        {
            // Shake gets more intense over time
            float intensity = deathShakeIntensity * (1f + elapsed / deathShakeDuration);
            
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;
            
            cam.transform.localPosition = originalPos + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        cam.transform.localPosition = originalPos;
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"<color=magenta>Boss hit by: {other.gameObject.name}</color>");
        
        // Check for player projectiles
        if (other.CompareTag("PlayerProjectile") || other.CompareTag("Projectile"))
        {
            // Try to get damage amount from projectile
            int damageAmount = 1;
            
            // Check for various projectile script types
            var pongOrb = other.GetComponent<PongOrb>();
            if (pongOrb != null)
            {
                damageAmount = pongOrb.damageAmount;
                Destroy(other.gameObject);
            }
            
            TakeDamage(damageAmount);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw arena boundaries
        Gizmos.color = Color.cyan;
        Vector3 bottomLeft = new Vector3(arenaMinX, arenaMinY, 0);
        Vector3 bottomRight = new Vector3(arenaMaxX, arenaMinY, 0);
        Vector3 topRight = new Vector3(arenaMaxX, arenaMaxY, 0);
        Vector3 topLeft = new Vector3(arenaMinX, arenaMaxY, 0);
        
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
        
        // Preview laser pattern in editor
        if (showLaserPreview)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            float arenaWidth = arenaMaxX - arenaMinX;
            float arenaHeight = arenaMaxY - arenaMinY;
            
            if (currentPattern == MazePattern.Horizontal || currentPattern == MazePattern.Grid)
            {
                for (int i = 0; i < horizontalLaserCount; i++)
                {
                    float t = (i + 1) / (float)(horizontalLaserCount + 1);
                    float yPos = Mathf.Lerp(arenaMinY, arenaMaxY, t);
                    Vector3 start = new Vector3(arenaMinX, yPos, 0);
                    Vector3 end = new Vector3(arenaMaxX, yPos, 0);
                    Gizmos.DrawLine(start, end);
                }
            }
            
            if (currentPattern == MazePattern.Vertical || currentPattern == MazePattern.Grid)
            {
                for (int i = 0; i < verticalLaserCount; i++)
                {
                    float t = (i + 1) / (float)(verticalLaserCount + 1);
                    float xPos = Mathf.Lerp(arenaMinX, arenaMaxX, t);
                    Vector3 start = new Vector3(xPos, arenaMinY, 0);
                    Vector3 end = new Vector3(xPos, arenaMaxY, 0);
                    Gizmos.DrawLine(start, end);
                }
            }
        }
    }
}
using UnityEngine;
using System.Collections;

public class PlayerCombat2D : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public Transform wand;
    public SpriteRenderer wandSprite;
    public GameObject fireballPrefab;
    public SpriteRenderer playerSprite;

    [Header("Wand Settings")]
    public bool hasWand = false;
    public bool showWandVisuals = true;

    [Header("Fireball Settings")]
    public float fireballForce = 10f;
    public int fireballDamage = 1; // Base damage for fireballs
    public float manaCostPerShot = 20f;
    public float fireRate = 0.2f; // Time between shots
    private float nextFireTime = 0f;
    
    [Header("Charge Shot")]
    public bool canChargeShot = true;
    public float chargeTime = 1f;
    public float maxChargeMultiplier = 3f;
    public Color chargeColor = new Color(1f, 0.5f, 0f);
    private float currentCharge = 0f;
    private bool isCharging = false;

    [Header("Wand Orbit Settings")]
    public float wandDistance = 1.5f;
    public float orbitSpeed = 8f;
    public float wandKickback = 0.3f; // Recoil when shooting
    private float currentKickback = 0f;
    
    [Header("Collision Settings")]
    public LayerMask collisionMask;
    public float collisionRadius = 0.2f;

    [Header("Parry Settings")]
    public float parryWindow = 0.3f;
    public float parryCooldown = 1f;
    public float parrySlowmoScale = 0.3f;
    public float parrySlowmoDuration = 0.2f;
    private bool isParrying = false;
    private float lastParryTime = -999f;
    
    [Header("Dash Settings")]
    public bool canDash = true;
    public float dashSpeed = 20f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1f;
    public int dashMana = 15;
    private bool isDashing = false;
    private float lastDashTime = -999f;
    
    [Header("Melee Attack")]
    public bool canMelee = true;
    public float meleeRange = 2f;
    public int meleeDamage = 3;
    public float meleeCooldown = 0.5f;
    public LayerMask enemyLayer;
    private float lastMeleeTime = -999f;

    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip chargeSound;
    public AudioClip parrySound;
    public AudioClip dashSound;
    public AudioClip meleeSound;
    private AudioSource audioSource;

    [Header("Screen Shake")]
    public bool screenShakeOnShoot = true;
    public float shootShakeIntensity = 0.1f;
    public float chargeShakeIntensity = 0.3f;
    public float meleeShakeIntensity = 0.15f;

    [Header("Effects")]
    public GameObject muzzleFlashPrefab;
    public GameObject chargeEffectPrefab;
    public GameObject dashTrailPrefab;

    private float currentAngle = 0f;
    private PlayerMana manaSystem;
    private Rigidbody2D rb;
    private GameObject activeChargeEffect;
    private Color originalWandColor;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        if (wand != null)
        {
            Vector2 initialPos = (Vector2)transform.position + Vector2.up * wandDistance;
            wand.position = initialPos;
            currentAngle = 90f;
            
            if (wandSprite != null)
            {
                originalWandColor = wandSprite.color;
            }
        }

        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) cam = FindObjectOfType<Camera>();
        }

        if (playerSprite == null)
        {
            playerSprite = GetComponent<SpriteRenderer>();
        }

        manaSystem = GetComponent<PlayerMana>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        UpdateWandVisibility();
    }

    void Update()
    {
        if (hasWand)
        {
            HandleWandOrbit();
            HandleShooting();
            HandleCharging();
            HandleMelee();
        }

        HandleParry();
        HandleDash();
        
        // Reduce kickback
        currentKickback = Mathf.Lerp(currentKickback, 0f, 10f * Time.deltaTime);
    }

    void UpdateWandVisibility()
    {
        if (wand != null)
        {
            wand.gameObject.SetActive(hasWand && showWandVisuals);
        }
    }

    void HandleWandOrbit()
    {
        if (wand == null || cam == null) return;

        // Get mouse world position
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        
        Vector2 playerPos = transform.position;
        Vector2 directionToMouse = ((Vector2)mouseWorldPos - playerPos).normalized;
        
        // Calculate angle
        float targetAngle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;
        currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, orbitSpeed * Time.deltaTime);

        Vector2 direction = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), 
                                        Mathf.Sin(currentAngle * Mathf.Deg2Rad));
        
        // Apply kickback
        float effectiveDistance = wandDistance - currentKickback;
        Vector2 targetPos = playerPos + direction * effectiveDistance;

        RaycastHit2D hit = Physics2D.CircleCast(playerPos, collisionRadius, direction, effectiveDistance, collisionMask);
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

    void HandleCharging()
    {
        if (!canChargeShot) return;
        
        // Start charging
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            if (!isCharging)
            {
                isCharging = true;
                currentCharge = 0f;
                
                // Spawn charge effect
                if (chargeEffectPrefab != null && wand != null)
                {
                    activeChargeEffect = Instantiate(chargeEffectPrefab, wand.position, Quaternion.identity, wand);
                }
                else if (wand != null)
                {
                    // Create inline charge effect without separate script
                    activeChargeEffect = new GameObject("ChargeEffect");
                    activeChargeEffect.transform.parent = wand;
                    activeChargeEffect.transform.localPosition = Vector3.zero;
                    StartCoroutine(AnimateChargeEffect(activeChargeEffect));
                }
            }
            
            // Charge up
            currentCharge += Time.deltaTime / chargeTime;
            currentCharge = Mathf.Clamp01(currentCharge);
            
            // Visual feedback
            if (wandSprite != null)
            {
                wandSprite.color = Color.Lerp(originalWandColor, chargeColor, currentCharge);
            }
            
            // Sound (pitch increases with charge)
            if (currentCharge > 0.1f && chargeSound != null && !audioSource.isPlaying)
            {
                audioSource.pitch = 0.5f + currentCharge * 0.5f;
                audioSource.PlayOneShot(chargeSound);
            }
        }
    }

    void HandleShooting()
    {
        if (!hasWand) return;

        // Release charged shot
        if (Input.GetMouseButtonUp(0) && isCharging)
        {
            ShootFireball(1f + currentCharge * (maxChargeMultiplier - 1f), currentCharge >= 1f);
            
            isCharging = false;
            currentCharge = 0f;
            
            if (wandSprite != null)
            {
                wandSprite.color = originalWandColor;
            }
            
            if (activeChargeEffect != null)
            {
                Destroy(activeChargeEffect);
            }
        }
        // Quick tap for normal shot (only if not charging)
        else if (Input.GetMouseButtonUp(0) && !isCharging && Time.time >= nextFireTime)
        {
            // Check if it was a quick tap (released within 0.1s)
            ShootFireball(1f, false);
        }
    }

    void ShootFireball(float powerMultiplier, bool isCharged)
    {
        if (fireballPrefab == null || wand == null || cam == null) return;

        float manaCost = manaCostPerShot * powerMultiplier;
        if (manaSystem != null && !manaSystem.UseMana(manaCost))
        {
            return;
        }

        nextFireTime = Time.time + fireRate;

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

        // Scale fireball based on charge
        if (isCharged)
        {
            fireball.transform.localScale *= 1f + powerMultiplier * 0.5f;
        }

        Rigidbody2D rb = fireball.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(shootDirection * fireballForce * powerMultiplier, ForceMode2D.Impulse);
        }

        // Increase fireball damage if charged
        PongOrb orb = fireball.GetComponent<PongOrb>();
        if (orb != null)
        {
            // Set base damage first
            orb.damageAmount = fireballDamage;
            
            // Then multiply if charged
            if (isCharged)
            {
                orb.damageAmount = Mathf.RoundToInt(fireballDamage * powerMultiplier);
            }
        }

        // Effects
        PlaySound(shootSound);
        currentKickback = wandKickback * powerMultiplier;
        
        // Muzzle flash
        if (muzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, spawnPos, Quaternion.identity);
            Destroy(flash, 0.2f);
        }
        else
        {
            // Create procedural muzzle flash
            StartCoroutine(ProceduralMuzzleFlash(spawnPos, shootDirection));
        }

        // Screen shake
        if (screenShakeOnShoot)
        {
            float intensity = isCharged ? chargeShakeIntensity : shootShakeIntensity;
            StartCoroutine(ScreenShake(intensity, 0.1f));
        }

        Debug.Log($"<color=orange>Fired {(isCharged ? "CHARGED" : "normal")} shot! Power: {powerMultiplier:F1}x</color>");
    }

    void HandleMelee()
    {
        if (!canMelee) return;
        
        if (Input.GetKeyDown(KeyCode.E) && Time.time >= lastMeleeTime + meleeCooldown)
        {
            lastMeleeTime = Time.time;
            StartCoroutine(MeleeAttack());
        }
    }

    IEnumerator MeleeAttack()
    {
        Debug.Log("<color=cyan>MELEE ATTACK!</color>");
        PlaySound(meleeSound);
        
        // Get attack direction
        Vector2 attackDir = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), 
                                        Mathf.Sin(currentAngle * Mathf.Deg2Rad));
        
        // Visual: Slash effect
        StartCoroutine(SlashEffect(wand.position, attackDir));
        
        // Damage enemies in range
        Collider2D[] hits = Physics2D.OverlapCircleAll(wand.position, meleeRange, enemyLayer);
        int hitCount = 0;
        
        foreach (Collider2D hit in hits)
        {
            // Try different enemy types
            var enemyHealth = hit.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(meleeDamage, attackDir);
                hitCount++;
            }
            
            var simpleEnemy = hit.GetComponent<SimpleEnemy>();
            if (simpleEnemy != null)
            {
                simpleEnemy.TakeDamage(meleeDamage, attackDir);
                hitCount++;
            }
            
            var laserBoss = hit.GetComponent<LaserBoss>();
            if (laserBoss != null)
            {
                laserBoss.TakeDamage(meleeDamage);
                hitCount++;
            }
            
            var kingsGuard = hit.GetComponent<KingsGuardBoss>();
            if (kingsGuard != null)
            {
                kingsGuard.DebugDamage(meleeDamage);
                hitCount++;
            }
        }
        
        if (hitCount > 0)
        {
            Debug.Log($"<color=yellow>Melee hit {hitCount} enemies!</color>");
            StartCoroutine(ScreenShake(meleeShakeIntensity, 0.15f));
        }
        
        yield return null;
    }

    void HandleParry()
    {
        if (Input.GetMouseButtonDown(1) && Time.time >= lastParryTime + parryCooldown)
        {
            lastParryTime = Time.time;
            StartCoroutine(Parry());
        }
    }

    IEnumerator Parry()
    {
        isParrying = true;
        PlaySound(parrySound);
        
        // Visual flash
        if (playerSprite != null)
        {
            StartCoroutine(ParryFlash());
        }
        
        // Particle burst
        StartCoroutine(ParryBurst(transform.position));
        
        Debug.Log("<color=cyan>★ PARRY!</color>");
        
        yield return new WaitForSeconds(parryWindow);
        isParrying = false;
    }

    IEnumerator ParryFlash()
    {
        Color original = playerSprite.color;
        for (int i = 0; i < 3; i++)
        {
            playerSprite.color = Color.cyan;
            yield return new WaitForSeconds(0.05f);
            playerSprite.color = original;
            yield return new WaitForSeconds(0.05f);
        }
    }

    IEnumerator ParryBurst(Vector3 position)
    {
        for (int i = 0; i < 8; i++)
        {
            GameObject particle = new GameObject("ParryParticle");
            particle.transform.position = position;
            
            SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(4, 4);
            for (int p = 0; p < 16; p++) tex.SetPixel(p % 4, p / 4, Color.cyan);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            
            Vector2 dir = Random.insideUnitCircle.normalized;
            StartCoroutine(MoveAndFadeParticle(particle, dir, 3f, 0.5f));
        }
        yield return null;
    }

    void HandleDash()
    {
        if (!canDash) return;
        
        if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) && 
            Time.time >= lastDashTime + dashCooldown && !isDashing)
        {
            // Check mana
            if (manaSystem != null && !manaSystem.UseMana(dashMana))
            {
                return;
            }
            
            lastDashTime = Time.time;
            StartCoroutine(Dash());
        }
    }

    IEnumerator Dash()
    {
        isDashing = true;
        PlaySound(dashSound);
        
        // Get dash direction (movement keys or mouse direction)
        Vector2 dashDir = Vector2.zero;
        
        if (Input.GetKey(KeyCode.A)) dashDir.x -= 1;
        if (Input.GetKey(KeyCode.D)) dashDir.x += 1;
        if (Input.GetKey(KeyCode.W)) dashDir.y += 1;
        if (Input.GetKey(KeyCode.S)) dashDir.y -= 1;
        
        // If no input, dash towards mouse
        if (dashDir.magnitude < 0.1f)
        {
            Vector2 mouseDir = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), 
                                          Mathf.Sin(currentAngle * Mathf.Deg2Rad));
            dashDir = mouseDir;
        }
        
        dashDir.Normalize();
        
        // Dash trail
        StartCoroutine(DashTrail());
        
        // Invincibility during dash (disable collisions briefly)
        Collider2D col = GetComponent<Collider2D>();
        bool wasColliding = false;
        if (col != null)
        {
            wasColliding = !col.isTrigger;
            col.isTrigger = true;
        }
        
        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            if (rb != null)
            {
                rb.linearVelocity = dashDir * dashSpeed;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Re-enable collisions
        if (col != null && wasColliding)
        {
            col.isTrigger = false;
        }
        
        isDashing = false;
    }

    IEnumerator DashTrail()
    {
        float duration = dashDuration + 0.2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            if (playerSprite != null)
            {
                GameObject trail = new GameObject("DashTrail");
                trail.transform.position = transform.position;
                trail.transform.localScale = transform.localScale;
                
                SpriteRenderer sr = trail.AddComponent<SpriteRenderer>();
                sr.sprite = playerSprite.sprite;
                sr.color = new Color(0.5f, 0.5f, 1f, 0.5f);
                sr.sortingLayerName = playerSprite.sortingLayerName;
                sr.sortingOrder = playerSprite.sortingOrder - 1;
                
                StartCoroutine(FadeOutSprite(sr, 0.3f));
            }
            
            elapsed += 0.05f;
            yield return new WaitForSeconds(0.05f);
        }
    }

    IEnumerator FadeOutSprite(SpriteRenderer sr, float duration)
    {
        Color start = sr.color;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            sr.color = Color.Lerp(start, Color.clear, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Destroy(sr.gameObject);
    }

    IEnumerator SlashEffect(Vector3 position, Vector2 direction)
    {
        for (int i = 0; i < 5; i++)
        {
            GameObject slash = new GameObject("Slash");
            slash.transform.position = position + (Vector3)(direction * i * 0.3f);
            
            SpriteRenderer sr = slash.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(8, 8);
            for (int p = 0; p < 64; p++) tex.SetPixel(p % 8, p / 8, Color.yellow);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f));
            
            Destroy(slash, 0.2f);
        }
        yield return null;
    }

    IEnumerator ProceduralMuzzleFlash(Vector3 position, Vector2 direction)
    {
        GameObject flash = new GameObject("MuzzleFlash");
        flash.transform.position = position;
        
        SpriteRenderer sr = flash.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(16, 16);
        for (int p = 0; p < 256; p++) 
        {
            float dist = Vector2.Distance(new Vector2(p % 16, p / 16), new Vector2(8, 8));
            Color col = dist < 8 ? new Color(1f, 0.8f, 0f, 1f - dist / 8f) : Color.clear;
            tex.SetPixel(p % 16, p / 16, col);
        }
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        
        yield return new WaitForSeconds(0.1f);
        Destroy(flash);
    }

    IEnumerator MoveAndFadeParticle(GameObject particle, Vector2 direction, float speed, float lifetime)
    {
        SpriteRenderer sr = particle.GetComponent<SpriteRenderer>();
        Color start = sr.color;
        float elapsed = 0f;
        
        while (elapsed < lifetime)
        {
            particle.transform.Translate(direction * speed * Time.deltaTime);
            sr.color = Color.Lerp(start, Color.clear, elapsed / lifetime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Destroy(particle);
    }
    
    IEnumerator AnimateChargeEffect(GameObject effect)
    {
        // Create orbiting particles
        int particleCount = 8;
        GameObject[] particles = new GameObject[particleCount];
        float[] angles = new float[particleCount];
        
        for (int i = 0; i < particleCount; i++)
        {
            particles[i] = new GameObject("ChargeParticle");
            particles[i].transform.parent = effect.transform;
            
            SpriteRenderer sr = particles[i].AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(6, 6);
            for (int p = 0; p < 36; p++)
            {
                float dist = Vector2.Distance(new Vector2(p % 6, p / 6), new Vector2(3, 3));
                tex.SetPixel(p % 6, p / 6, dist < 3 ? chargeColor : Color.clear);
            }
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 6, 6), new Vector2(0.5f, 0.5f));
            
            angles[i] = (360f / particleCount) * i;
        }
        
        // Animate particles orbiting
        while (effect != null)
        {
            for (int i = 0; i < particleCount; i++)
            {
                if (particles[i] == null) continue;
                
                angles[i] += 360f * Time.deltaTime;
                float rad = angles[i] * Mathf.Deg2Rad;
                
                Vector2 pos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * 0.3f;
                particles[i].transform.localPosition = pos;
            }
            yield return null;
        }
    }

    IEnumerator ScreenShake(float intensity, float duration)
    {
        if (cam == null) yield break;
        
        Vector3 originalPos = cam.transform.localPosition;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;
            cam.transform.localPosition = originalPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        cam.transform.localPosition = originalPos;
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void GiveWand()
    {
        hasWand = true;
        UpdateWandVisibility();
        Debug.Log("<color=yellow>★ Player obtained the wand!</color>");
    }

    public void RemoveWand()
    {
        hasWand = false;
        UpdateWandVisibility();
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

    public bool IsParrying() => isParrying;
    public bool IsDashing() => isDashing;

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
                
                // Melee range
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(wand.position, meleeRange);
            }
        }
    }
}
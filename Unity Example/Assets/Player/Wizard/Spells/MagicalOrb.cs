using UnityEngine;

public enum OrbType { Fire, Ice }

public class PongOrb : MonoBehaviour
{
    [Header("Orb")]
    public float speed = 8f;
    public OrbType currentType = OrbType.Fire;
    public GameObject owner;
    public float lifetime = 20f;
    public float bounceSpeedMultiplier = 1.08f;
    public float postBounceLifetime = 6f;

    [Header("Audio")]
    public AudioClip bounceSfx;
    public AudioClip hitEnemySfx;
    public AudioClip hitPlayerSfx;
    public AudioClip parrySfx;
    private AudioSource audioSource;

    [Header("Visuals")]
    public float spinSpeed = 720f;
    public TrailRenderer trail;

    [Header("Impact FX")]
    public GameObject hitPlayerEffect;
    public GameObject hitEnemyEffect;
    public GameObject parryEffect;

    [Header("Damage")]
    public int damageAmount = 1;

    private Vector2 direction = Vector2.right;
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private bool usePhysics = true;
    private bool hasBounced = false;
    private float spawnTime;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        UpdateColor();
        spawnTime = Time.time;
        Destroy(gameObject, lifetime);

        if (rb != null && rb.linearVelocity.magnitude > 0.1f)
        {
            usePhysics = true;
            direction = rb.linearVelocity.normalized;
        }
        else
        {
            usePhysics = false;
        }

        if (trail != null)
        {
            trail.Clear();
        }

        // Ignore collision with owner
        if (owner != null)
        {
            Collider2D ownerCollider = owner.GetComponent<Collider2D>();
            Collider2D myCollider = GetComponent<Collider2D>();
            if (ownerCollider != null && myCollider != null)
            {
                Physics2D.IgnoreCollision(myCollider, ownerCollider, true);
            }
        }

        UpdateVisuals();
    }

    void Update()
    {
        if (!usePhysics)
        {
            transform.Translate(direction * speed * Time.deltaTime);
        }
        else if (rb != null && rb.linearVelocity.magnitude > 0.1f)
        {
            direction = rb.linearVelocity.normalized;
        }

        transform.Rotate(Vector3.forward, spinSpeed * Time.deltaTime);
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (direction.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            if (sr != null)
            {
                sr.flipY = angle > 90f || angle < -90f;
            }
        }
    }

    public void SetDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude == 0) dir = Vector2.right;
        direction = dir.normalized;

        // Always set the velocity, regardless of usePhysics flag
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
            usePhysics = true; // Enable physics mode when we set direction
        }

        UpdateVisuals();
    }

    public Vector2 GetDirection() => direction;

    public void ReverseDirection()
    {
        direction = -direction;

        if (usePhysics && rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        UpdateVisuals();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject == owner)
        {
            var playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null && hasBounced && currentType == OrbType.Fire)
            {
                playerHealth.TakeDamage(damageAmount);
                PlaySfx(hitPlayerSfx);
                SpawnEffect(hitPlayerEffect);
                Destroy(gameObject);
                return;
            }
            return;
        }

        var playerCombat = collision.gameObject.GetComponent<PlayerCombat2D>();
        if (playerCombat != null)
        {
            if (playerCombat.IsParrying())
            {
                ReverseDirection();
                owner = collision.gameObject;
                hasBounced = true;
                lifetime = Mathf.Min(lifetime, Time.time - spawnTime + postBounceLifetime);
                if (trail != null) trail.emitting = true;
                PlaySfx(parrySfx);
                SpawnEffect(parryEffect);
                return;
            }
            else
            {
                var hitPlayerHealth = collision.gameObject.GetComponent<PlayerHealth>();
                if (hitPlayerHealth != null)
                {
                    hitPlayerHealth.TakeDamage(damageAmount);
                    PlaySfx(hitPlayerSfx);
                    SpawnEffect(hitPlayerEffect);
                }
                Destroy(gameObject);
                return;
            }
        }

        // Check for EnemyHealth (Kings Guard)
        var enemyHealth = collision.gameObject.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            Vector2 attackDirection = (transform.position - collision.transform.position).normalized;
            enemyHealth.TakeDamage(damageAmount, attackDirection);
            PlaySfx(hitEnemySfx);
            SpawnEffect(hitEnemyEffect);
            Destroy(gameObject);
            return;
        }

        // Check for SimpleEnemy (new simple enemies)
        var simpleEnemy = collision.gameObject.GetComponent<SimpleEnemy>();
        if (simpleEnemy != null)
        {
            Vector2 attackDirection = (transform.position - collision.transform.position).normalized;
            simpleEnemy.TakeDamage(damageAmount, attackDirection);
            PlaySfx(hitEnemySfx);
            SpawnEffect(hitEnemyEffect);
            Destroy(gameObject);
            return;
        }

        // Check for LaserBoss
        var laserBoss = collision.gameObject.GetComponent<LaserBoss>();
        if (laserBoss != null)
        {
            laserBoss.TakeDamage(damageAmount);
            PlaySfx(hitEnemySfx);
            SpawnEffect(hitEnemyEffect);
            Destroy(gameObject);
            return;
        }

        // Bounce off walls
        if (collision.contacts != null && collision.contacts.Length > 0)
        {
            Vector2 normal = collision.contacts[0].normal;
            direction = Vector2.Reflect(direction, normal).normalized;
            speed *= bounceSpeedMultiplier;
            if (usePhysics && rb != null)
            {
                rb.linearVelocity = direction * speed;
            }

            hasBounced = true;
            float elapsed = Time.time - spawnTime;
            if (elapsed < postBounceLifetime)
            {
                Destroy(gameObject, postBounceLifetime);
            }

            UpdateVisuals();
            ToggleType();
            UpdateColor();
            PlaySfx(bounceSfx);
            if (trail != null) trail.emitting = true;
        }
        else
        {
            ReverseDirection();
            PlaySfx(bounceSfx);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == owner && !hasBounced)
        {
            return;
        }

        var playerCombat = other.GetComponent<PlayerCombat2D>();
        if (playerCombat != null)
        {
            if (playerCombat.IsParrying())
            {
                ReverseDirection();
                owner = other.gameObject;
                hasBounced = true;
                lifetime = Mathf.Min(lifetime, Time.time - spawnTime + postBounceLifetime);
                if (trail != null) trail.emitting = true;
                PlaySfx(parrySfx);
                SpawnEffect(parryEffect);
                return;
            }
            else
            {
                var hitPlayerHealth = other.GetComponent<PlayerHealth>();
                if (hitPlayerHealth != null)
                {
                    hitPlayerHealth.TakeDamage(damageAmount);
                    PlaySfx(hitPlayerSfx);
                    SpawnEffect(hitPlayerEffect);
                }
                Destroy(gameObject);
                return;
            }
        }

        // Check for EnemyHealth (Kings Guard)
        var enemyHealth = other.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            Vector2 attackDirection = (transform.position - other.transform.position).normalized;
            enemyHealth.TakeDamage(damageAmount, attackDirection);
            PlaySfx(hitEnemySfx);
            SpawnEffect(hitEnemyEffect);
            Destroy(gameObject);
            return;
        }

        // Check for SimpleEnemy (new simple enemies)
        var simpleEnemy = other.GetComponent<SimpleEnemy>();
        if (simpleEnemy != null)
        {
            Vector2 attackDirection = (transform.position - other.transform.position).normalized;
            simpleEnemy.TakeDamage(damageAmount, attackDirection);
            PlaySfx(hitEnemySfx);
            SpawnEffect(hitEnemyEffect);
            Destroy(gameObject);
            return;
        }

        // Check for LaserBoss
        var laserBoss = other.GetComponent<LaserBoss>();
        if (laserBoss != null)
        {
            laserBoss.TakeDamage(damageAmount);
            PlaySfx(hitEnemySfx);
            SpawnEffect(hitEnemyEffect);
            Destroy(gameObject);
            return;
        }
    }

    void ToggleType()
    {
        currentType = (currentType == OrbType.Fire) ? OrbType.Ice : OrbType.Fire;
    }

    void UpdateColor()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;
        sr.color = (currentType == OrbType.Fire) ? Color.red : Color.cyan;
    }

    void PlaySfx(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void SpawnEffect(GameObject effectPrefab)
    {
        if (effectPrefab != null)
        {
            Instantiate(effectPrefab, transform.position, Quaternion.identity);
        }
    }
}
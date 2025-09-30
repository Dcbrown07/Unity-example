using UnityEngine;

public enum OrbType { Fire, Ice }

public class PongOrb : MonoBehaviour
{
    [Header("Orb")]
    public float speed = 8f;
    public OrbType currentType = OrbType.Fire;
    public GameObject owner;            
    public float lifetime = 20f;

    [Header("Audio")]
    public AudioClip bounceSfx;
    public AudioClip hitEnemySfx;
    public AudioClip hitPlayerSfx;
    public AudioClip parrySfx;
    private AudioSource audioSource;

    // internal state
    private Vector2 direction = Vector2.right;
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private bool usePhysics = true; 

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
        
        UpdateVisuals();
    }

    void Update()
    {
        if (!usePhysics)
        {
            transform.Translate(direction * speed * Time.deltaTime);
        }
        else if (rb != null)
        {
            if (rb.linearVelocity.magnitude > 0.1f)
            {
                direction = rb.linearVelocity.normalized;
            }
        }
        
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
        
        if (usePhysics && rb != null)
        {
            rb.linearVelocity = direction * speed;
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
        if (collision.gameObject == owner) return;

        var playerCombat = collision.gameObject.GetComponent<PlayerCombat2D>();
        if (playerCombat != null)
        {
            if (playerCombat.IsParrying())
            {
                ReverseDirection();
                owner = collision.gameObject;
                PlaySfx(parrySfx);
                Debug.Log("Orb parried by player and reflected.");
                return;
            }
            else
            {
                PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(1);
                    PlaySfx(hitPlayerSfx);
                    Debug.Log("Orb hit player and dealt damage.");
                }
                
                Destroy(gameObject);
                return;
            }
        }

        var enemy = collision.gameObject.GetComponent<EnemyController>();
        if (enemy != null)
        {
            enemy.TakeDamage(1);
            PlaySfx(hitEnemySfx);
            Debug.Log("Orb hit enemy: " + collision.gameObject.name);
            Destroy(gameObject);
            return;
        }

        if (collision.contacts != null && collision.contacts.Length > 0)
        {
            Vector2 normal = collision.contacts[0].normal;
            direction = Vector2.Reflect(direction, normal).normalized;

            if (usePhysics && rb != null)
            {
                rb.linearVelocity = direction * speed;
            }

            UpdateVisuals();
            ToggleType();
            UpdateColor();
            PlaySfx(bounceSfx);

            Debug.Log("Orb bounced off: " + collision.gameObject.name + " new dir: " + direction);
        }
        else
        {
            ReverseDirection();
            PlaySfx(bounceSfx);
            Debug.Log("Orb collision without contacts; reversed as fallback.");
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
}

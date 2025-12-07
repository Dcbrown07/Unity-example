using UnityEngine;
using System.Collections;

public class LaserBeam : MonoBehaviour
{
    [Header("Laser Settings")]
    public float warningTime = 2f;
    public float activeTime = 2f;
    public int damage = 1;
    public float damageCooldown = 0.5f;
    public GameObject boss;
    
    [Header("Visual Colors")]
    public Color warningColor = new Color(1f, 0f, 0f, 0.3f);
    public Color activeColor = new Color(1f, 0f, 0f, 1f);
    public float flashSpeed = 5f;
    
    [Header("Audio")]
    public AudioClip warningSound;
    public AudioClip activateSound;
    public AudioClip hitSound;
    private AudioSource audioSource;
    
    [Header("Effects")]
    public bool screenShakeOnActivate = true;
    public float shakeIntensity = 0.15f;
    public GameObject activateParticles;
    
    private SpriteRenderer sprite;
    private PolygonCollider2D laserCollider;
    private bool isActive = false;
    private float lastDamageTime = -999f;
    public bool hasHitPlayer = false;
    
    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        laserCollider = GetComponent<PolygonCollider2D>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        if (sprite == null)
        {
            sprite = gameObject.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            sprite.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }
        
        if (laserCollider == null)
        {
            laserCollider = gameObject.AddComponent<PolygonCollider2D>();
            laserCollider.isTrigger = true;
        }
        
        // Ignore collision with boss
        if (boss != null)
        {
            Collider2D bossCollider = boss.GetComponent<Collider2D>();
            if (bossCollider != null)
            {
                Physics2D.IgnoreCollision(laserCollider, bossCollider, true);
            }
        }
        
        StartCoroutine(LaserSequence());
    }
    
    IEnumerator LaserSequence()
    {
        // WARNING PHASE
        if (laserCollider != null)
        {
            laserCollider.enabled = false;
        }
        
        // Play warning sound
        PlaySound(warningSound);
        
        Debug.Log("<color=yellow>⚠ Laser warning - FLASHING!</color>");
        
        // Flash warning
        float elapsedTime = 0f;
        while (elapsedTime < warningTime)
        {
            if (sprite != null)
            {
                float alpha = Mathf.PingPong(Time.time * flashSpeed, 0.6f);
                sprite.color = new Color(1f, 0f, 0f, alpha);
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // ACTIVATE!
        Debug.Log("<color=red>⚡ Laser ACTIVE - DANGER!</color>");
        
        if (sprite != null)
        {
            sprite.color = activeColor;
        }
        
        if (laserCollider != null)
        {
            laserCollider.enabled = true;
        }
        
        isActive = true;
        
        // Play activate sound
        PlaySound(activateSound);
        
        // Screen shake
        if (screenShakeOnActivate)
        {
            StartCoroutine(ScreenShake());
        }
        
        // Spawn particles
        if (activateParticles != null)
        {
            Instantiate(activateParticles, transform.position, transform.rotation);
        }
        
        // Pulse effect while active
        StartCoroutine(ActivePulse());
        
        yield return new WaitForSeconds(activeTime);
        
        // Laser ends
        Debug.Log("<color=green>Laser deactivated</color>");
        Destroy(gameObject);
    }
    
    IEnumerator ActivePulse()
    {
        float timer = 0f;
        while (isActive && timer < activeTime)
        {
            if (sprite != null)
            {
                float intensity = Mathf.PingPong(Time.time * 8f, 0.3f);
                sprite.color = Color.Lerp(activeColor, Color.white, intensity);
            }
            timer += Time.deltaTime;
            yield return null;
        }
    }
    
    IEnumerator ScreenShake()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam == null) yield break;
        
        Vector3 originalPos = cam.transform.localPosition;
        float elapsed = 0f;
        float duration = 0.2f;
        
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
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;
        
        if (other.CompareTag("Player"))
        {
            hasHitPlayer = true;
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                lastDamageTime = Time.time;
                PlaySound(hitSound);
                Debug.Log($"<color=red>⚡ LASER HIT PLAYER for {damage} damage!</color>");
            }
        }
    }
    
    void OnTriggerStay2D(Collider2D other)
    {
        if (!isActive) return;
        
        if (other.CompareTag("Player"))
        {
            if (Time.time >= lastDamageTime + damageCooldown)
            {
                hasHitPlayer = true;
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                    lastDamageTime = Time.time;
                    PlaySound(hitSound);
                    Debug.Log($"<color=red>⚡ LASER BURNING PLAYER for {damage} damage!</color>");
                }
            }
        }
    }
    
    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    void OnDrawGizmos()
    {
        PolygonCollider2D col = GetComponent<PolygonCollider2D>();
        if (col != null && col.points.Length > 0)
        {
            Gizmos.color = isActive ? Color.red : new Color(1, 1, 0, 0.5f);
            
            for (int i = 0; i < col.points.Length; i++)
            {
                Vector2 p1 = transform.TransformPoint(col.points[i]);
                Vector2 p2 = transform.TransformPoint(col.points[(i + 1) % col.points.Length]);
                Gizmos.DrawLine(p1, p2);
            }
        }
    }
}
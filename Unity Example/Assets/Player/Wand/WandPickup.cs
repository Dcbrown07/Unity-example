using UnityEngine;

public class WandPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    [Tooltip("Automatically pick up on contact or require button press")]
    public bool autoPickup = true;
    
    [Tooltip("Key to press for pickup (if not auto)")]
    public KeyCode pickupKey = KeyCode.E;

    [Header("Visual Feedback")]
    [Tooltip("Particle effect on pickup")]
    public GameObject pickupEffect;
    
    [Tooltip("Rotate the wand pickup")]
    public bool rotateWand = true;
    
    [Tooltip("Rotation speed")]
    public float rotationSpeed = 90f;
    
    [Tooltip("Bob up and down")]
    public bool bobUpDown = true;
    
    [Tooltip("Bob height")]
    public float bobHeight = 0.3f;
    
    [Tooltip("Bob speed")]
    public float bobSpeed = 2f;
    
    [Header("Glow Effect")]
    [Tooltip("Enable glowing outline")]
    public bool enableGlow = true;
    
    [Tooltip("Glow color")]
    public Color glowColor = new Color(1f, 0.8f, 0f, 0.6f); // Golden glow
    
    [Tooltip("Glow pulse speed")]
    public float glowPulseSpeed = 2f;
    
    [Tooltip("Glow size multiplier")]
    public float glowSize = 1.3f;
    
    [Tooltip("Number of glow rings")]
    public int glowRings = 2;
    
    private GameObject[] glowObjects;

    [Header("UI Prompt")]
    [Tooltip("Show 'Press E to pick up' text")]
    public GameObject pickupPrompt;

    [Header("Audio")]
    public AudioClip pickupSound;

    private Vector3 startPosition;
    private bool playerInRange = false;
    private bool pickedUp = false;

    void Start()
    {
        startPosition = transform.position;
        
        if (pickupPrompt != null)
        {
            pickupPrompt.SetActive(false);
        }
        
        // Create glow effect
        if (enableGlow)
        {
            CreateGlowEffect();
        }
    }

    void Update()
    {
        if (pickedUp) return;

        // Visual effects
        if (rotateWand)
        {
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }

        if (bobUpDown)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
        
        // Update glow effect
        if (enableGlow && glowObjects != null)
        {
            UpdateGlowEffect();
        }

        // Pickup input
        if (playerInRange && !autoPickup && Input.GetKeyDown(pickupKey))
        {
            PickupWand();
        }
    }
    
    void CreateGlowEffect()
    {
        glowObjects = new GameObject[glowRings];
        
        for (int i = 0; i < glowRings; i++)
        {
            GameObject glow = new GameObject($"WandGlow_{i}");
            glow.transform.parent = transform;
            glow.transform.localPosition = Vector3.zero;
            
            SpriteRenderer glowSr = glow.AddComponent<SpriteRenderer>();
            
            // Create circular glow sprite
            int resolution = 64;
            Texture2D glowTex = new Texture2D(resolution, resolution);
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float maxDist = resolution / 2f;
                    
                    // Create ring effect
                    float ringThickness = 4f;
                    float ringRadius = maxDist - (i * 6f) - 4f;
                    float distFromRing = Mathf.Abs(distance - ringRadius);
                    
                    float alpha = 0f;
                    if (distFromRing < ringThickness)
                    {
                        alpha = 1f - (distFromRing / ringThickness);
                        alpha *= 0.6f; // Max opacity
                    }
                    
                    Color pixelColor = new Color(glowColor.r, glowColor.g, glowColor.b, alpha * glowColor.a);
                    glowTex.SetPixel(x, y, pixelColor);
                }
            }
            
            glowTex.Apply();
            glowSr.sprite = Sprite.Create(glowTex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 100f);
            glowSr.sortingOrder = -1; // Behind the wand
            
            glowObjects[i] = glow;
        }
    }
    
    void UpdateGlowEffect()
    {
        // Pulse effect
        float pulse = (Mathf.Sin(Time.time * glowPulseSpeed) + 1f) * 0.5f; // 0 to 1
        float scale = glowSize + (pulse * 0.3f);
        
        for (int i = 0; i < glowObjects.Length; i++)
        {
            if (glowObjects[i] != null)
            {
                // Each ring scales differently for layered effect
                float ringScale = scale + (i * 0.1f);
                glowObjects[i].transform.localScale = Vector3.one * ringScale;
                
                // Fade alpha with pulse
                SpriteRenderer sr = glowObjects[i].GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Color color = glowColor;
                    color.a = glowColor.a * (0.4f + pulse * 0.6f);
                    sr.color = color;
                }
                
                // Counter-rotate some rings for cool effect
                if (i % 2 == 0)
                {
                    glowObjects[i].transform.Rotate(Vector3.forward, rotationSpeed * 0.5f * Time.deltaTime);
                }
                else
                {
                    glowObjects[i].transform.Rotate(Vector3.forward, -rotationSpeed * 0.3f * Time.deltaTime);
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (pickedUp) return;

        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            if (autoPickup)
            {
                PickupWand();
            }
            else
            {
                // Show prompt
                if (pickupPrompt != null)
                {
                    pickupPrompt.SetActive(true);
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            
            if (pickupPrompt != null)
            {
                pickupPrompt.SetActive(false);
            }
        }
    }

    void PickupWand()
    {
        if (pickedUp) return;

        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // Give wand to player
        PlayerCombat2D combat = player.GetComponent<PlayerCombat2D>();
        if (combat != null)
        {
            combat.GiveWand();
            pickedUp = true;

            // Visual effect
            if (pickupEffect != null)
            {
                Instantiate(pickupEffect, transform.position, Quaternion.identity);
            }

            // Audio
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }

            // Hide prompt
            if (pickupPrompt != null)
            {
                pickupPrompt.SetActive(false);
            }

            Debug.Log("Player picked up the wand!");

            // Destroy pickup
            Destroy(gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw pickup range
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            if (col is CircleCollider2D circle)
            {
                Gizmos.DrawWireSphere(transform.position, circle.radius);
            }
            else if (col is BoxCollider2D box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.offset, box.size);
            }
        }
    }
}
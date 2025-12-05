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

        // Pickup input
        if (playerInRange && !autoPickup && Input.GetKeyDown(pickupKey))
        {
            PickupWand();
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
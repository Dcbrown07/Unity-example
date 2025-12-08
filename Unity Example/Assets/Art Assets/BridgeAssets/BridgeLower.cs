using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Drawbridge : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("How close player needs to be to trigger")]
    public float activationDistance = 5f;
    
    [Tooltip("Player transform (auto-finds if not set)")]
    public Transform player;
    
    [Header("Animation")]
    [Tooltip("Animator component on bridge")]
    public Animator animator;
    
    [Tooltip("Name of the trigger/bool parameter in animator")]
    public string animationTriggerName = "Lower";
    
    [Tooltip("Use SetTrigger (true) or SetBool (false)")]
    public bool useTrigger = true;
    
    [Tooltip("Vertical offset to apply after animation (if bridge is too high)")]
    public float verticalOffset = 0f;
    
    [Tooltip("Apply offset smoothly over time")]
    public bool smoothOffset = true;
    
    [Tooltip("How fast to apply offset")]
    public float offsetSpeed = 2f;
    
    [Header("Options")]
    [Tooltip("Only activate once")]
    public bool oneTimeUse = true;
    
    [Header("Audio")]
    public AudioClip activationSound;
    private AudioSource audioSource;
    
    [Header("Debug")]
    public bool showDebugGizmos = true;
    
    private bool hasActivated = false;
    private bool isApplyingOffset = false;
    private Vector3 targetPosition;
    private Vector3 originalPosition;

    void Start()
    {
        // Auto-find player
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
        
        // Auto-find animator
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && activationSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        originalPosition = transform.position;
    }

    void Update()
    {
        // Check for player proximity
        if (!hasActivated && player != null && animator != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            
            // Check if player is in range
            if (distance <= activationDistance)
            {
                TriggerBridge();
            }
        }
        
        // Apply offset smoothly
        if (isApplyingOffset && smoothOffset)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, offsetSpeed * Time.deltaTime);
            
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isApplyingOffset = false;
            }
        }
    }
    
    void TriggerBridge()
    {
        hasActivated = true;
        
        Debug.Log("<color=green>Bridge activated!</color>");
        
        // Trigger animation
        if (useTrigger)
        {
            animator.SetTrigger(animationTriggerName);
        }
        else
        {
            animator.SetBool(animationTriggerName, true);
        }
        
        // Play sound
        if (audioSource != null && activationSound != null)
        {
            audioSource.PlayOneShot(activationSound);
        }
        
        // Apply offset if set
        if (Mathf.Abs(verticalOffset) > 0.01f)
        {
            targetPosition = originalPosition + new Vector3(0, verticalOffset, 0);
            if (smoothOffset)
            {
                isApplyingOffset = true;
            }
            else
            {
                transform.position = targetPosition;
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // Draw activation range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationDistance);
        
        // Draw line to player if in range
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            if (distance <= activationDistance)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, player.position);
            }
        }
    }
}
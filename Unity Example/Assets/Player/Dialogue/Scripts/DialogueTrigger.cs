using UnityEngine;
using System.Collections.Generic;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Content")]
    [Tooltip("Lines of dialogue to display")]
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();

    [Header("Trigger Settings")]
    [Tooltip("Trigger automatically on enter or require button press")]
    public bool autoTrigger = true;
    
    [Tooltip("Key to trigger dialogue (if not auto)")]
    public KeyCode triggerKey = KeyCode.E;
    
    [Tooltip("Only trigger once, then disable")]
    public bool triggerOnce = true;
    
    [Tooltip("Destroy trigger after use")]
    public bool destroyAfterTrigger = false;

    [Header("UI Prompt")]
    [Tooltip("Show 'Press E to talk' prompt")]
    public GameObject interactPrompt;

    [Header("Conditions")]
    [Tooltip("Only trigger if player has wand")]
    public bool requiresWand = false;
    
    [Tooltip("Delay before dialogue can trigger (seconds)")]
    public float triggerDelay = 0f;

    private bool hasTriggered = false;
    private bool playerInRange = false;
    private float enableTime;

    void Start()
    {
        enableTime = Time.time;
        
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }

        // Validation
        if (dialogueLines.Count == 0)
        {
            Debug.LogWarning($"DialogueTrigger on {gameObject.name} has no dialogue lines!");
        }
    }

    void Update()
    {
        if (playerInRange && !autoTrigger && !hasTriggered)
        {
            if (Input.GetKeyDown(triggerKey))
            {
                TriggerDialogue();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            // Check if enough time has passed
            if (Time.time < enableTime + triggerDelay)
            {
                return;
            }

            // Check wand requirement
            if (requiresWand)
            {
                PlayerCombat2D combat = other.GetComponent<PlayerCombat2D>();
                if (combat == null || !HasWand(combat))
                {
                    return;
                }
            }

            if (autoTrigger && !hasTriggered)
            {
                TriggerDialogue();
            }
            else if (!autoTrigger && interactPrompt != null)
            {
                interactPrompt.SetActive(true);
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(false);
            }
        }
    }

    bool HasWand(PlayerCombat2D combat)
    {
        // Use reflection or make hasWand public
        // For now, assume if they can shoot, they have wand
        return true; // You'll need to expose hasWand as public in PlayerCombat2D
    }

    void TriggerDialogue()
    {
        if (hasTriggered && triggerOnce) return;
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager not found in scene!");
            return;
        }

        if (dialogueLines.Count == 0)
        {
            Debug.LogWarning("No dialogue lines to display!");
            return;
        }

        // Start dialogue
        DialogueManager.Instance.StartDialogue(dialogueLines);

        hasTriggered = true;

        // Hide prompt
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }

        // Destroy or disable
        if (destroyAfterTrigger)
        {
            Destroy(gameObject, 0.5f);
        }
        else if (triggerOnce)
        {
            GetComponent<Collider2D>().enabled = false;
        }
    }

    void OnDrawGizmos()
    {
        // Draw trigger zone
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            if (col is BoxCollider2D box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.offset, box.size);
            }
            else if (col is CircleCollider2D circle)
            {
                Gizmos.DrawWireSphere(transform.position, circle.radius);
            }
        }
    }
}
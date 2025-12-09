using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [TextArea(3, 10)]
    public string[] dialogueLines; // Array of text lines
    
    [Tooltip("Trigger once and then disable")]
    public bool triggerOnce = true;
    
    [Tooltip("Require key press to advance (E key)")]
    public bool requireKeyPress = true;
    
    [Tooltip("Auto-advance time per line (if not requiring key press)")]
    public float autoAdvanceTime = 3f;
    
    [Tooltip("Pause player movement during dialogue")]
    public bool pausePlayer = false; // Default to FALSE so player can keep moving
    
    [Header("Visual Settings")]
    [Tooltip("Optional: Use a custom dialogue box prefab (leave empty to auto-generate)")]
    public GameObject customDialogueBoxPrefab;
    
    [Tooltip("Name of Text component in custom prefab (default: 'DialogueText')")]
    public string textComponentName = "DialogueText";
    
    public Color textBoxColor = new Color(0f, 0f, 0f, 0.8f);
    public Color textColor = Color.white;
    public int fontSize = 24;
    
    private bool hasTriggered = false;
    private bool isShowingDialogue = false;
    private GameObject dialogueBox;
    private Text dialogueText;
    private int currentLine = 0;
    private GameObject player;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            player = other.gameObject;
            ShowDialogue();
            
            if (triggerOnce)
            {
                hasTriggered = true;
            }
        }
    }

    void ShowDialogue()
    {
        if (isShowingDialogue || dialogueLines.Length == 0) return;
        
        isShowingDialogue = true;
        currentLine = 0;
        
        // Pause player if needed
        if (pausePlayer && player != null)
        {
            var controller = player.GetComponent<PlayerController2D>();
            if (controller != null) controller.enabled = false;
        }
        
        CreateDialogueBox();
        DisplayCurrentLine();
    }

    void CreateDialogueBox()
    {
        // Find or create canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("DialogueCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Use custom prefab if provided
        if (customDialogueBoxPrefab != null)
        {
            dialogueBox = Instantiate(customDialogueBoxPrefab, canvas.transform);
            
            // Find text component by name
            Transform textTransform = dialogueBox.transform.Find(textComponentName);
            if (textTransform != null)
            {
                dialogueText = textTransform.GetComponent<Text>();
            }
            
            // If not found by name, try to find any Text component
            if (dialogueText == null)
            {
                dialogueText = dialogueBox.GetComponentInChildren<Text>();
            }
            
            if (dialogueText == null)
            {
                Debug.LogError($"Could not find Text component in custom dialogue box! Looking for: '{textComponentName}'");
            }
            
            return;
        }
        
        // Otherwise, create default dialogue box
        dialogueBox = new GameObject("DialogueBox");
        dialogueBox.transform.SetParent(canvas.transform, false);
        
        RectTransform boxRect = dialogueBox.AddComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.1f, 0.1f);
        boxRect.anchorMax = new Vector2(0.9f, 0.3f);
        boxRect.offsetMin = Vector2.zero;
        boxRect.offsetMax = Vector2.zero;
        
        Image boxImage = dialogueBox.AddComponent<Image>();
        boxImage.color = textBoxColor;
        
        // Create text
        GameObject textObj = new GameObject("DialogueText");
        textObj.transform.SetParent(dialogueBox.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.05f, 0.1f);
        textRect.anchorMax = new Vector2(0.95f, 0.9f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        dialogueText = textObj.AddComponent<Text>();
        dialogueText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        dialogueText.fontSize = fontSize;
        dialogueText.color = textColor;
        dialogueText.alignment = TextAnchor.MiddleLeft;
        
        // Add prompt text if using key press
        if (requireKeyPress)
        {
            GameObject promptObj = new GameObject("PromptText");
            promptObj.transform.SetParent(dialogueBox.transform, false);
            
            RectTransform promptRect = promptObj.AddComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.8f, 0.05f);
            promptRect.anchorMax = new Vector2(0.95f, 0.15f);
            promptRect.offsetMin = Vector2.zero;
            promptRect.offsetMax = Vector2.zero;
            
            Text promptText = promptObj.AddComponent<Text>();
            promptText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            promptText.fontSize = fontSize - 6;
            promptText.color = new Color(1f, 1f, 1f, 0.7f);
            promptText.alignment = TextAnchor.MiddleRight;
            promptText.text = "Press E";
        }
    }

    void DisplayCurrentLine()
    {
        if (currentLine < dialogueLines.Length && dialogueText != null)
        {
            dialogueText.text = dialogueLines[currentLine];
            
            if (!requireKeyPress)
            {
                StartCoroutine(AutoAdvance());
            }
        }
        else
        {
            EndDialogue();
        }
    }

    void Update()
    {
        if (isShowingDialogue && requireKeyPress && Input.GetKeyDown(KeyCode.E))
        {
            NextLine();
        }
    }

    void NextLine()
    {
        currentLine++;
        
        if (currentLine < dialogueLines.Length)
        {
            DisplayCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }

    IEnumerator AutoAdvance()
    {
        yield return new WaitForSeconds(autoAdvanceTime);
        NextLine();
    }

    void EndDialogue()
    {
        isShowingDialogue = false;
        
        if (dialogueBox != null)
        {
            Destroy(dialogueBox);
        }
        
        // Resume player
        if (pausePlayer && player != null)
        {
            var controller = player.GetComponent<PlayerController2D>();
            if (controller != null) controller.enabled = true;
        }
    }

    void OnDrawGizmos()
    {
        // Show trigger area in editor
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.DrawCube(transform.position, col.bounds.size);
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class DialogueLine
{
    [TextArea(2, 5)]
    public string text;
    
    [Tooltip("Character name (optional)")]
    public string speaker = "";
    
    [Tooltip("How long to display this line (0 = wait for input)")]
    public float displayTime = 0f;
}

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI References")]
    [Tooltip("Main dialogue panel")]
    public GameObject dialoguePanel;
    
    [Tooltip("Text component for dialogue")]
    public Text dialogueText;
    
    [Tooltip("Text component for speaker name")]
    public Text speakerText;
    
    [Tooltip("Continue button/indicator")]
    public GameObject continueIndicator;

    [Header("Settings")]
    [Tooltip("Key to advance dialogue")]
    public KeyCode advanceKey = KeyCode.Space;
    
    [Tooltip("Enable typewriter effect")]
    public bool useTypewriter = true;
    
    [Tooltip("Characters per second for typewriter")]
    public float typewriterSpeed = 30f;
    
    [Tooltip("Pause game during dialogue")]
    public bool pauseGameDuringDialogue = false;

    [Header("Audio")]
    public AudioClip dialogueStartSound;
    public AudioClip dialogueAdvanceSound;
    public AudioClip dialogueEndSound;
    
    private AudioSource audioSource;

    private Queue<DialogueLine> dialogueQueue;
    private bool isDialogueActive = false;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        dialogueQueue = new Queue<DialogueLine>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Start()
    {
        // Hide dialogue UI at start
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    void Update()
    {
        if (isDialogueActive && Input.GetKeyDown(advanceKey))
        {
            if (isTyping)
            {
                // Skip typing animation
                CompleteTyping();
            }
            else
            {
                // Show next line
                DisplayNextLine();
            }
        }
    }

    public void StartDialogue(List<DialogueLine> lines)
    {
        if (isDialogueActive) return;

        isDialogueActive = true;
        dialogueQueue.Clear();

        foreach (DialogueLine line in lines)
        {
            dialogueQueue.Enqueue(line);
        }

        // Show dialogue panel
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        // Pause game if enabled
        if (pauseGameDuringDialogue)
        {
            Time.timeScale = 0f;
        }

        // Play start sound
        if (audioSource != null && dialogueStartSound != null)
        {
            audioSource.PlayOneShot(dialogueStartSound);
        }

        DisplayNextLine();
    }

    void DisplayNextLine()
    {
        if (dialogueQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = dialogueQueue.Dequeue();

        // Set speaker name
        if (speakerText != null)
        {
            speakerText.text = line.speaker;
            speakerText.gameObject.SetActive(!string.IsNullOrEmpty(line.speaker));
        }

        // Display text
        if (useTypewriter)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            typingCoroutine = StartCoroutine(TypeText(line));
        }
        else
        {
            dialogueText.text = line.text;
            isTyping = false;
            
            if (line.displayTime > 0)
            {
                StartCoroutine(AutoAdvance(line.displayTime));
            }
        }

        // Play advance sound
        if (audioSource != null && dialogueAdvanceSound != null)
        {
            audioSource.PlayOneShot(dialogueAdvanceSound);
        }

        // Show/hide continue indicator
        if (continueIndicator != null)
        {
            continueIndicator.SetActive(line.displayTime <= 0);
        }
    }

    IEnumerator TypeText(DialogueLine line)
    {
        isTyping = true;
        dialogueText.text = "";
        
        if (continueIndicator != null)
        {
            continueIndicator.SetActive(false);
        }

        foreach (char letter in line.text.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSecondsRealtime(1f / typewriterSpeed);
        }

        isTyping = false;
        
        if (continueIndicator != null && line.displayTime <= 0)
        {
            continueIndicator.SetActive(true);
        }

        // Auto advance if time is set
        if (line.displayTime > 0)
        {
            yield return StartCoroutine(AutoAdvance(line.displayTime));
        }
    }

    IEnumerator AutoAdvance(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        DisplayNextLine();
    }

    void CompleteTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // Show full text immediately
        if (dialogueQueue.Count > 0 || dialogueText.text.Length < 100)
        {
            // Complete current line
            isTyping = false;
            
            if (continueIndicator != null)
            {
                continueIndicator.SetActive(true);
            }
        }
    }

    void EndDialogue()
    {
        isDialogueActive = false;

        // Hide dialogue panel
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // Unpause game
        if (pauseGameDuringDialogue)
        {
            Time.timeScale = 1f;
        }

        // Play end sound
        if (audioSource != null && dialogueEndSound != null)
        {
            audioSource.PlayOneShot(dialogueEndSound);
        }

        Debug.Log("Dialogue ended");
    }

    public bool IsDialogueActive() => isDialogueActive;
}
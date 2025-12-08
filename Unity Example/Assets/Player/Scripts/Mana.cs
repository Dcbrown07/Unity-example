using UnityEngine;
using UnityEngine.UI;

public class PlayerMana : MonoBehaviour
{
    [Header("Mana Settings")]
    [Tooltip("Maximum mana")]
    public float maxMana = 100f;
    
    [Tooltip("Starting mana")]
    public float startingMana = 100f;
    
    [Tooltip("Mana regeneration per second")]
    public float manaRegenRate = 10f;
    
    [Tooltip("Delay before mana regeneration starts after casting")]
    public float regenDelay = 1f;

    [Header("Low Mana Warning")]
    [Tooltip("Mana percentage to trigger low warning")]
    [Range(0f, 1f)]
    public float lowManaThreshold = 0.3f;
    
    [Tooltip("Enable pulsing glow when low on mana")]
    public bool enableLowManaPulse = true;
    
    [Tooltip("Pulse speed when low on mana")]
    public float pulseSpeed = 3f;
    
    [Tooltip("Color when low on mana")]
    public Color lowManaColor = new Color(0.3f, 0.3f, 1f, 1f); // Blue tint

    [Header("Mana UI")]
    [Tooltip("UI Slider for mana bar")]
    public Slider manaBar;
    
    [Tooltip("Show mana as percentage text")]
    public Text manaText;
    
    [Tooltip("Optional: Image to change color based on mana")]
    public Image manaBarFill;

    [Header("Visual Feedback")]
    [Tooltip("Color when out of mana")]
    public Color outOfManaColor = Color.red;
    
    [Tooltip("Flash duration when out of mana")]
    public float flashDuration = 0.2f;
    
    public SpriteRenderer playerSprite;
    
    [Header("Wand Visual")]
    [Tooltip("Wand sprite to glow when mana is available")]
    public SpriteRenderer wandSprite;
    
    [Tooltip("Glow intensity when full mana")]
    public float maxGlowIntensity = 1.5f;

    [Header("Audio")]
    public AudioClip outOfManaSound;
    public AudioClip lowManaWarningSound;
    private AudioSource audioSource;

    private float currentMana;
    private float lastCastTime = -999f;
    private Color originalColor;
    private Color wandOriginalColor;
    private bool lowManaWarningPlayed = false;
    private bool isDead = false;

    void Start()
    {
        currentMana = startingMana;
        
        if (playerSprite != null)
        {
            originalColor = playerSprite.color;
        }

        if (wandSprite != null)
        {
            wandOriginalColor = wandSprite.color;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (outOfManaSound != null || lowManaWarningSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        UpdateManaUI();
    }

    void Update()
    {
        if (isDead) return; // Don't update mana when dead

        // Regenerate mana
        if (currentMana < maxMana && Time.time >= lastCastTime + regenDelay)
        {
            currentMana += manaRegenRate * Time.deltaTime;
            currentMana = Mathf.Min(currentMana, maxMana);
            UpdateManaUI();
            
            // Reset low mana warning when recovered
            if (GetManaPercent() > lowManaThreshold)
            {
                lowManaWarningPlayed = false;
            }
        }

        // Visual feedback for low mana
        if (enableLowManaPulse && GetManaPercent() <= lowManaThreshold && currentMana > 0)
        {
            // Pulse player sprite
            if (playerSprite != null)
            {
                float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0 to 1
                playerSprite.color = Color.Lerp(originalColor, lowManaColor, pulse * 0.5f);
            }

            // Play warning sound once
            if (!lowManaWarningPlayed && audioSource != null && lowManaWarningSound != null)
            {
                audioSource.PlayOneShot(lowManaWarningSound);
                lowManaWarningPlayed = true;
            }
        }
        else if (playerSprite != null && GetManaPercent() > lowManaThreshold)
        {
            playerSprite.color = originalColor;
        }

        // Wand glow based on mana (brightness only, preserve original color)
        if (wandSprite != null)
        {
            float manaPercent = GetManaPercent();
            // Darken wand when low on mana instead of changing color
            float brightness = Mathf.Lerp(0.5f, 1f, manaPercent); // 50% to 100% brightness
            wandSprite.color = wandOriginalColor * brightness;
        }
    }

    public bool UseMana(float amount)
    {
        if (currentMana >= amount)
        {
            currentMana -= amount;
            lastCastTime = Time.time;
            UpdateManaUI();
            return true;
        }
        else
        {
            // Not enough mana
            OnOutOfMana();
            return false;
        }
    }
    
    public bool HasMana(float amount)
    {
        return currentMana >= amount;
    }

    void OnOutOfMana()
    {
        Debug.Log("<color=yellow>Out of mana!</color>");
        
        // Play sound
        if (audioSource != null && outOfManaSound != null)
        {
            audioSource.PlayOneShot(outOfManaSound);
        }

        // Flash player red
        if (playerSprite != null)
        {
            StartCoroutine(FlashOutOfMana());
        }
    }

    System.Collections.IEnumerator FlashOutOfMana()
    {
        playerSprite.color = outOfManaColor;
        yield return new WaitForSeconds(flashDuration);
        playerSprite.color = originalColor;
    }

    void UpdateManaUI()
    {
        float manaPercent = GetManaPercent();

        if (manaBar != null)
        {
            manaBar.value = manaPercent;
        }

        if (manaText != null)
        {
            manaText.text = $"Mana: {Mathf.Ceil(currentMana)}/{maxMana}";
        }

        // Change mana bar color based on amount
        if (manaBarFill != null)
        {
            if (manaPercent <= 0f)
            {
                manaBarFill.color = Color.red;
            }
            else if (manaPercent <= lowManaThreshold)
            {
                manaBarFill.color = Color.Lerp(Color.red, Color.yellow, manaPercent / lowManaThreshold);
            }
            else
            {
                manaBarFill.color = Color.Lerp(Color.yellow, Color.cyan, (manaPercent - lowManaThreshold) / (1f - lowManaThreshold));
            }
        }
    }

    public float GetMana() => currentMana;
    public float GetMaxMana() => maxMana;
    public float GetManaPercent() => currentMana / maxMana;
    
    public void AddMana(float amount)
    {
        currentMana += amount;
        currentMana = Mathf.Min(currentMana, maxMana);
        UpdateManaUI();
    }

    public void OnPlayerDeath()
    {
        isDead = true;
        
        // Reset sprite to normal color when dead
        if (playerSprite != null)
        {
            playerSprite.color = originalColor;
        }
    }
}
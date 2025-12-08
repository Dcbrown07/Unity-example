using UnityEngine;
using System.Collections;

public class PlayerVisibility : MonoBehaviour
{
    [Header("Player Glow")]
    [Tooltip("Enable glowing outline around player")]
    public bool enableGlow = true;
    
    [Tooltip("Glow color (increase alpha if not visible)")]
    public Color glowColor = new Color(0.8f, 0.9f, 1f, 0.8f); // Brighter default
    
    [Tooltip("Glow pulse speed")]
    public float glowPulseSpeed = 2f;
    
    [Tooltip("Glow size")]
    public float glowSize = 1.5f; // Bigger by default
    
    [Tooltip("Number of glow layers (more = softer)")]
    public int glowLayers = 2;
    
    [Header("Highlight When Hit")]
    [Tooltip("Flash brighter when taking damage")]
    public bool flashOnDamage = true;
    
    [Tooltip("Flash color")]
    public Color damageFlashColor = new Color(1f, 0.3f, 0.3f, 0.8f); // Red
    
    [Tooltip("Flash duration")]
    public float flashDuration = 0.2f;
    
    [Header("Dash Trail Enhancement")]
    [Tooltip("Enhance dash visibility")]
    public bool enhanceDashTrail = true;
    
    [Tooltip("Dash glow color")]
    public Color dashGlowColor = new Color(0.5f, 1f, 1f, 0.7f); // Cyan
    
    private GameObject[] glowObjects;
    private SpriteRenderer playerSprite;
    private bool isFlashing = false;
    private PlayerCombat2D combat;
    private PlayerController2D controller;
    
    void Start()
    {
        playerSprite = GetComponent<SpriteRenderer>();
        combat = GetComponent<PlayerCombat2D>();
        controller = GetComponent<PlayerController2D>();
        
        if (enableGlow)
        {
            CreatePlayerGlow();
        }
    }
    
    void Update()
    {
        if (enableGlow && glowObjects != null)
        {
            UpdatePlayerGlow();
        }
    }
    
    void CreatePlayerGlow()
    {
        if (playerSprite == null) return;
        
        glowObjects = new GameObject[glowLayers];
        
        for (int i = 0; i < glowLayers; i++)
        {
            GameObject glow = new GameObject($"PlayerGlow_{i}");
            glow.transform.SetParent(transform, false);
            glow.transform.localPosition = Vector3.zero;
            glow.transform.localRotation = Quaternion.identity;
            
            SpriteRenderer glowSr = glow.AddComponent<SpriteRenderer>();
            
            // Create soft circular glow
            int resolution = 64;
            Texture2D glowTex = new Texture2D(resolution, resolution);
            glowTex.filterMode = FilterMode.Bilinear;
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float maxDist = resolution / 2f;
                    
                    // Soft gradient from center
                    float alpha = 1f - (distance / maxDist);
                    alpha = Mathf.Clamp01(alpha);
                    alpha = Mathf.Pow(alpha, 1.5f); // Gentler falloff
                    
                    // Make it more visible
                    alpha *= (i == 0) ? 0.8f : 0.5f;
                    
                    Color pixelColor = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
                    glowTex.SetPixel(x, y, pixelColor);
                }
            }
            
            glowTex.Apply();
            
            // Create sprite with proper pixels per unit
            glowSr.sprite = Sprite.Create(
                glowTex, 
                new Rect(0, 0, resolution, resolution), 
                new Vector2(0.5f, 0.5f), 
                32f // Pixels per unit - adjust to match your player
            );
            
            glowSr.sortingLayerName = playerSprite.sortingLayerName;
            glowSr.sortingOrder = playerSprite.sortingOrder - 1 - i;
            glowSr.color = glowColor;
            
            glowObjects[i] = glow;
            
            Debug.Log($"Created player glow layer {i} - Check if visible in scene view!");
        }
    }
    
    void UpdatePlayerGlow()
    {
        if (glowObjects == null || glowObjects.Length == 0) return;
        
        // Get current glow color based on state
        Color currentGlowColor = glowColor;
        float pulseIntensity = 1f;
        
        // Flash red when taking damage
        if (isFlashing)
        {
            currentGlowColor = damageFlashColor;
            pulseIntensity = 2f;
        }
        // Glow cyan when dashing
        else if (controller != null && controller.IsDashing())
        {
            currentGlowColor = dashGlowColor;
            pulseIntensity = 1.5f;
        }
        // Glow brighter when parrying
        else if (combat != null && combat.IsParrying())
        {
            currentGlowColor = new Color(0f, 1f, 1f, 0.9f); // Bright cyan
            pulseIntensity = 1.8f;
        }
        
        // Pulse effect
        float pulse = (Mathf.Sin(Time.time * glowPulseSpeed * pulseIntensity) + 1f) * 0.5f;
        float scale = glowSize + (pulse * 0.3f);
        
        for (int i = 0; i < glowObjects.Length; i++)
        {
            if (glowObjects[i] == null) continue;
            
            // Scale with layer offset
            float layerScale = scale + (i * 0.2f);
            glowObjects[i].transform.localScale = Vector3.one * layerScale;
            
            // Update color and alpha
            SpriteRenderer sr = glowObjects[i].GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color color = currentGlowColor;
                float layerAlpha = currentGlowColor.a * (0.6f + pulse * 0.4f);
                
                // Outer layers are dimmer
                if (i > 0)
                {
                    layerAlpha *= 0.7f;
                }
                
                color.a = layerAlpha;
                sr.color = color;
            }
        }
    }
    
    // Call this from PlayerHealth when taking damage
    public void TriggerDamageFlash()
    {
        if (flashOnDamage && !isFlashing)
        {
            StartCoroutine(DamageFlash());
        }
    }
    
    IEnumerator DamageFlash()
    {
        isFlashing = true;
        yield return new WaitForSeconds(flashDuration);
        isFlashing = false;
    }
    
    // Public methods for external control
    public void SetGlowColor(Color color)
    {
        glowColor = color;
    }
    
    public void SetGlowEnabled(bool enabled)
    {
        enableGlow = enabled;
        
        if (glowObjects != null)
        {
            foreach (GameObject glow in glowObjects)
            {
                if (glow != null)
                {
                    glow.SetActive(enabled);
                }
            }
        }
    }
}
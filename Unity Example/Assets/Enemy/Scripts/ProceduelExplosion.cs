using UnityEngine;
using System.Collections;

public class ProceduralExplosion : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float explosionDuration = 1f;
    public float maxSize = 3f;
    public int particleCount = 20;
    public Color startColor = new Color(1f, 0.5f, 0f, 1f); // Orange
    public Color endColor = new Color(1f, 0f, 0f, 0f); // Red transparent
    
    void Start()
    {
        StartCoroutine(Explode());
    }
    
    IEnumerator Explode()
    {
        // Create main flash
        GameObject flash = CreateFlash();
        
        // Create particles
        for (int i = 0; i < particleCount; i++)
        {
            CreateParticle();
        }
        
        // Destroy after duration
        yield return new WaitForSeconds(explosionDuration);
        Destroy(gameObject);
    }
    
    GameObject CreateFlash()
    {
        GameObject flash = new GameObject("Flash");
        flash.transform.parent = transform;
        flash.transform.localPosition = Vector3.zero;
        
        SpriteRenderer sr = flash.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite(64);
        sr.color = Color.white;
        
        StartCoroutine(AnimateFlash(flash));
        
        return flash;
    }
    
    IEnumerator AnimateFlash(GameObject flash)
    {
        SpriteRenderer sr = flash.GetComponent<SpriteRenderer>();
        float elapsed = 0f;
        float duration = 0.3f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            flash.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, maxSize * 1.5f, t);
            sr.color = Color.Lerp(Color.white, new Color(1, 1, 1, 0), t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Destroy(flash);
    }
    
    void CreateParticle()
    {
        GameObject particle = new GameObject("Particle");
        particle.transform.parent = transform;
        particle.transform.localPosition = Vector3.zero;
        
        SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite(16);
        sr.color = startColor;
        
        // Random direction and speed
        Vector2 direction = Random.insideUnitCircle.normalized;
        float speed = Random.Range(2f, 5f);
        float size = Random.Range(0.2f, 0.5f);
        
        StartCoroutine(AnimateParticle(particle, direction, speed, size));
    }
    
    IEnumerator AnimateParticle(GameObject particle, Vector2 direction, float speed, float startSize)
    {
        SpriteRenderer sr = particle.GetComponent<SpriteRenderer>();
        float elapsed = 0f;
        Vector3 startPos = particle.transform.localPosition;
        
        while (elapsed < explosionDuration)
        {
            float t = elapsed / explosionDuration;
            
            // Move outward
            particle.transform.localPosition = startPos + (Vector3)(direction * speed * elapsed);
            
            // Shrink and fade
            particle.transform.localScale = Vector3.one * Mathf.Lerp(startSize, 0f, t);
            sr.color = Color.Lerp(startColor, endColor, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Destroy(particle);
    }
    
    Sprite CreateCircleSprite(int resolution)
    {
        Texture2D tex = new Texture2D(resolution, resolution);
        Color[] pixels = new Color[resolution * resolution];
        
        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float radius = resolution / 2f;
        
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                if (distance <= radius)
                {
                    // Soft edge
                    float alpha = 1f - (distance / radius);
                    alpha = Mathf.Pow(alpha, 0.5f); // Soften edge
                    pixels[y * resolution + x] = new Color(1, 1, 1, alpha);
                }
                else
                {
                    pixels[y * resolution + x] = Color.clear;
                }
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }
}
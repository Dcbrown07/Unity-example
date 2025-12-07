using UnityEngine;
using System.Collections;

public class ProceduralChargeEffect : MonoBehaviour
{
    public int particleCount = 12;
    public float orbitRadius = 0.5f;
    public float orbitSpeed = 360f;
    public Color startColor = new Color(1f, 0.5f, 0f, 0.7f);
    public Color endColor = new Color(1f, 0f, 0f, 1f);
    
    private GameObject[] particles;
    private float[] angles;
    
    void Start()
    {
        particles = new GameObject[particleCount];
        angles = new float[particleCount];
        
        for (int i = 0; i < particleCount; i++)
        {
            particles[i] = CreateParticle();
            angles[i] = (360f / particleCount) * i;
        }
    }
    
    GameObject CreateParticle()
    {
        GameObject particle = new GameObject("ChargeParticle");
        particle.transform.parent = transform;
        
        SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(8, 8);
        
        // Create circle
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(4, 4));
                Color col = dist < 4 ? startColor : Color.clear;
                tex.SetPixel(x, y, col);
            }
        }
        tex.Apply();
        
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f));
        
        return particle;
    }
    
    void Update()
    {
        float chargeProgress = Mathf.PingPong(Time.time * 2f, 1f);
        
        for (int i = 0; i < particleCount; i++)
        {
            if (particles[i] == null) continue;
            
            angles[i] += orbitSpeed * Time.deltaTime;
            float rad = angles[i] * Mathf.Deg2Rad;
            
            Vector2 pos = new Vector2(
                Mathf.Cos(rad) * orbitRadius,
                Mathf.Sin(rad) * orbitRadius
            );
            
            particles[i].transform.localPosition = pos;
            
            SpriteRenderer sr = particles[i].GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = Color.Lerp(startColor, endColor, chargeProgress);
            }
        }
    }
    
    void OnDestroy()
    {
        foreach (GameObject particle in particles)
        {
            if (particle != null) Destroy(particle);
        }
    }
}
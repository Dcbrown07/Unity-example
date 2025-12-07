using UnityEngine;
using System.Collections;

public class Shockwave : MonoBehaviour
{
    public float speed = 8f;
    public float lifetime = 3f;
    public int damage = 2;
    private Vector2 direction;
    private bool hasHit = false;
    
    private SpriteRenderer sprite;
    
    public void Initialize(Vector2 dir, int dmg)
    {
        direction = dir.normalized;
        damage = dmg;
        
        sprite = GetComponent<SpriteRenderer>();
        if (sprite == null)
        {
            sprite = gameObject.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(32, 32);
            for (int i = 0; i < 32 * 32; i++)
            {
                tex.SetPixel(i % 32, i / 32, new Color(1f, 1f, 0f, 0.7f));
            }
            tex.Apply();
            sprite.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        }
        
        // Set scale based on direction
        if (Mathf.Abs(direction.x) > 0.5f)
        {
            transform.localScale = new Vector3(2f, 1f, 1f);
        }
        
        // Add collider
        BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        
        Destroy(gameObject, lifetime);
        StartCoroutine(Pulse());
    }
    
    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);
    }
    
    IEnumerator Pulse()
    {
        while (true)
        {
            if (sprite != null)
            {
                float scale = 1f + Mathf.PingPong(Time.time * 3f, 0.2f);
                transform.localScale = new Vector3(transform.localScale.x, scale, 1f);
            }
            yield return null;
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        
        if (other.CompareTag("Player"))
        {
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage);
                hasHit = true;
                Debug.Log($"<color=yellow>Shockwave hit player for {damage} damage!</color>");
                Destroy(gameObject);
            }
        }
        // Destroy on collision with anything that's not the player
        else if (!other.CompareTag("Player"))
        {
            // Check if it's a solid object (walls, ground, etc)
            if (other.gameObject.layer == LayerMask.NameToLayer("Ground") || 
                other.gameObject.layer == LayerMask.NameToLayer("Wall") ||
                other.isTrigger == false) // Any non-trigger collider
            {
                Destroy(gameObject);
            }
        }
    }
}
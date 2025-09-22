using UnityEngine;

public enum OrbType { Fire, Ice }

public class PongOrb : MonoBehaviour
{
    public float speed = 8f;
    public OrbType currentType = OrbType.Fire;
    public GameObject owner;
    public float lifetime = 20f;

    private Vector2 direction;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        UpdateColor();
        Destroy(gameObject, lifetime);
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject == owner) return;

        Vector2 normal = collision.contacts[0].normal;
        direction = Vector2.Reflect(direction, normal).normalized;

        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Enemy"))
        {
            // Damage handling
            var enemy = collision.gameObject.GetComponent<EnemyController>();
            if (enemy != null)
                enemy.TakeDamage(1);

            var player = collision.gameObject.GetComponent<PlayerCombat2D>();
            if (player != null)
            {
                if (player.IsParrying())
                {
                    direction = -direction; // reflect
                    Debug.Log("Orb reflected by player parry!");
                    return;
                }
                else
                {
                    Debug.Log("Player hit by orb!");
                }
            }

            Destroy(gameObject);
        }

        // Change type on every bounce
        currentType = currentType == OrbType.Fire ? OrbType.Ice : OrbType.Fire;
        UpdateColor();
    }

    void UpdateColor()
    {
        if (!sr) return;
        sr.color = currentType == OrbType.Fire ? Color.red : Color.cyan;
    }
}
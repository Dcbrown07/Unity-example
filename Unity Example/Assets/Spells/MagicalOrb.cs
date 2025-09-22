using UnityEngine;

public enum OrbType { Fire, Ice }

public class PongOrb : MonoBehaviour
{
    [Header("Orb")]
    public float speed = 8f;
    public OrbType currentType = OrbType.Fire;
    public GameObject owner;            // assigned when spawned
    public float lifetime = 20f;

    // internal state
    private Vector2 direction = Vector2.right;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        UpdateColor();
        Destroy(gameObject, lifetime); // auto cleanup
    }

    void Update()
    {
        // Move manually so physics won't "grab" it
        transform.Translate(direction * speed * Time.deltaTime);
    }

    // Public API the rest of your code expects
    public void SetDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude == 0) dir = Vector2.right;
        direction = dir.normalized;
    }

    public Vector2 GetDirection()
    {
        return direction;
    }

    public void ReverseDirection()
    {
        direction = -direction;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // ignore collisions with the shooter immediately
        if (collision.gameObject == owner) return;

        // PLAYER HIT / PARRY
        var player = collision.gameObject.GetComponent<PlayerCombat2D>();
        if (player != null)
        {
            if (player.IsParrying())
            {
                // reflect and make the new owner whoever parried
                ReverseDirection();
                owner = collision.gameObject;
                Debug.Log("Orb parried by player and reflected.");
                return; // don't destroy
            }
            else
            {
                Debug.Log("Orb hit player.");
                Destroy(gameObject);
                return;
            }
        }

        // ENEMY HIT
        var enemy = collision.gameObject.GetComponent<EnemyController>();
        if (enemy != null)
        {
            enemy.TakeDamage(1);
            Debug.Log("Orb hit enemy: " + collision.gameObject.name);
            Destroy(gameObject);
            return;
        }

        // DEFAULT: bounce off walls/other colliders
        // use the collision normal so reflection is correct
        if (collision.contacts != null && collision.contacts.Length > 0)
        {
            Vector2 normal = collision.contacts[0].normal;
            direction = Vector2.Reflect(direction, normal).normalized;

            // change orb type/color on bounce (optional)
            ToggleType();
            UpdateColor();

            Debug.Log("Orb bounced off: " + collision.gameObject.name + " new dir: " + direction);
        }
        else
        {
            // fallback: just reverse
            ReverseDirection();
            Debug.Log("Orb collision without contacts; reversed as fallback.");
        }
    }

    void ToggleType()
    {
        currentType = (currentType == OrbType.Fire) ? OrbType.Ice : OrbType.Fire;
    }

    void UpdateColor()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;
        sr.color = (currentType == OrbType.Fire) ? Color.red : Color.cyan;
    }
}
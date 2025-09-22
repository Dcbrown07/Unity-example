using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    public int health = 3;
    public float moveSpeed = 2f;
    public Transform player;

    public GameObject orbPrefab;
    public Transform orbSpawnPoint;
    public float fireRate = 2f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        StartCoroutine(AutoShoot());
    }

    void FixedUpdate()
    {
        MoveTowardsPlayer();
    }

    void MoveTowardsPlayer()
    {
        if (player == null) return;
        Vector2 dir = (player.position - transform.position).normalized;
        rb.MovePosition(rb.position + dir * moveSpeed * Time.fixedDeltaTime);
    }

    IEnumerator AutoShoot()
    {
        while (true)
        {
            if (orbPrefab != null && orbSpawnPoint != null && player != null)
            {
                GameObject orb = Instantiate(orbPrefab, orbSpawnPoint.position, Quaternion.identity);
                PongOrb orbScript = orb.GetComponent<PongOrb>();
                if (orbScript != null)
                {
                    orbScript.owner = gameObject;
                    orbScript.SetDirection((player.position - orbSpawnPoint.position).normalized);
                }

                Debug.Log(name + " casted orb at player!");
            }

            yield return new WaitForSeconds(fireRate);
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log(name + " took " + damage + " damage. Health left: " + health);
        if (health <= 0)
            DieEnemy();
    }

    void DieEnemy()
    {
        Debug.Log(name + " died!");
        Destroy(gameObject);
    }
}
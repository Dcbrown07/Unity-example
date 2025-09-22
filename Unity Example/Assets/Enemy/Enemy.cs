using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    [Header("Stats")]
    public int health = 3;
    public float moveSpeed = 2f;

    [Header("Combat")]
    public GameObject orbPrefab;
    public Transform orbSpawnPoint;
    public float fireRate = 2f;
    public float parryRange = 1.5f; // how close the orb must be to parry
    public float parryDuration = 0.3f;
    private bool isParrying = false;

    [Header("Target")]
    public Transform player;

    [Header("Respawn")]
    public bool respawn = false;
    public float respawnDelay = 3f;
    private Vector3 spawnPosition;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spawnPosition = transform.position;
        StartCoroutine(AutoShoot());
    }

    void FixedUpdate()
    {
        MoveTowardsPlayer();
        CheckForOrbParry();
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
            if (orbPrefab != null && orbSpawnPoint != null)
            {
                Vector2 targetPoint = GetRandomTargetPoint();
                Vector2 shootDir = (targetPoint - (Vector2)orbSpawnPoint.position).normalized;

                Vector2 spawnPos = (Vector2)orbSpawnPoint.position + shootDir * 0.5f;

                GameObject orb = Instantiate(orbPrefab, spawnPos, Quaternion.identity);
                PongOrb orbScript = orb.GetComponent<PongOrb>();
                orbScript.owner = gameObject;
                orbScript.SetDirection(shootDir);

                Debug.Log(name + " shot orb!");
            }

            yield return new WaitForSeconds(fireRate);
        }
    }

    void CheckForOrbParry()
    {
        if (isParrying) return;

        // find all PongOrbs in scene
        PongOrb[] orbs = FindObjectsOfType<PongOrb>();
        foreach (PongOrb orb in orbs)
        {
            if (orb.owner == gameObject) continue; // ignore own orbs

            float distance = Vector2.Distance(orb.transform.position, transform.position);
            if (distance <= parryRange)
            {
                StartCoroutine(ParryOrb(orb));
                break;
            }
        }
    }

    IEnumerator ParryOrb(PongOrb orb)
    {
        isParrying = true;
        Debug.Log(name + " is parrying!");

        // reverse orb direction
        orb.SetDirection(-orb.GetDirection());

        yield return new WaitForSeconds(parryDuration);
        isParrying = false;
        Debug.Log(name + " parry ended!");
    }

    Vector2 GetRandomTargetPoint()
    {
        if (player != null)
        {
            float range = 3f;
            Vector2 randomOffset = new Vector2(Random.Range(-range, range), Random.Range(-range, range));
            return (Vector2)player.position + randomOffset;
        }
        else
        {
            return (Vector2)transform.position + new Vector2(Random.Range(-5f,5f), Random.Range(-5f,5f));
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
        gameObject.SetActive(false);

        if (respawn)
            StartCoroutine(RespawnEnemy());
    }

    IEnumerator RespawnEnemy()
    {
        yield return new WaitForSeconds(respawnDelay);
        health = 3;
        transform.position = spawnPosition;
        gameObject.SetActive(true);
        Debug.Log(name + " respawned!");
    }
}
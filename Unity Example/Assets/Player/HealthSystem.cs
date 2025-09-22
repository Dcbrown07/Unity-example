using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 5;
    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
        Debug.Log("Player health: " + currentHealth);
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log("Player took " + amount + " damage. Health left: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Player died!");
        // Add respawn, game over, or animation logic here
        gameObject.SetActive(false);
    }

    public int GetHealth() => currentHealth;
}

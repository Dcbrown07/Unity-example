using UnityEngine;
using UnityEngine.SceneManagement;

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
        
        // Reset the current level after a short delay
        Invoke("ResetLevel", 1f); // 1 second delay so player can see what happened
        
        gameObject.SetActive(false);
    }

    void ResetLevel()
    {
        // Reload the current scene to reset the level
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public int GetHealth() => currentHealth;
    
    public int GetMaxHealth() => maxHealth;
}
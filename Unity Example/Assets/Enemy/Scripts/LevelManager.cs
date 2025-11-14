using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Level Configuration")]
    public int currentLevel = 1;
    public int totalLevels = 5;

    [Header("Level Win Conditions")]
    public bool winByKillingEnemy = true;      // Win by defeating enemy
    public bool winByReachingTrigger = false;  // Win by reaching exit trigger
    
    [Header("Scene Names - Set these in Inspector")]
    public string level1SceneName = "Level1";
    public string level2SceneName = "Level2";
    public string level3SceneName = "Level3";
    public string level4SceneName = "Level4";
    public string level5SceneName = "Level5";
    public string victorySceneName = "Victory"; // Final victory scene (optional)

    private bool levelComplete = false;

    void Awake()
    {
        // Singleton pattern - keeps level manager across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Determine current level from active scene
        string currentSceneName = SceneManager.GetActiveScene().name;
        currentLevel = GetLevelFromSceneName(currentSceneName);
        
        Debug.Log("=== LEVEL MANAGER START ===");
        Debug.Log("Current Scene: " + currentSceneName);
        Debug.Log("Current Level: " + currentLevel);
        Debug.Log("Win by Killing Enemy: " + winByKillingEnemy);
        Debug.Log("Win by Reaching Trigger: " + winByReachingTrigger);
        Debug.Log("========================");
    }

    int GetLevelFromSceneName(string sceneName)
    {
        if (sceneName == level1SceneName) return 1;
        if (sceneName == level2SceneName) return 2;
        if (sceneName == level3SceneName) return 3;
        if (sceneName == level4SceneName) return 4;
        if (sceneName == level5SceneName) return 5;
        return 1; // Default to level 1
    }

    public void EnemyDefeated()
    {
        if (winByKillingEnemy && !levelComplete)
        {
            levelComplete = true;
            Debug.Log("Enemy defeated! Level complete.");
            Invoke("LoadNextLevel", 2f); // 2 second delay before loading next level
        }
    }

    public void TriggerReached()
    {
        if (winByReachingTrigger && !levelComplete)
        {
            levelComplete = true;
            Debug.Log("Exit trigger reached! Level complete.");
            Invoke("LoadNextLevel", 1f); // 1 second delay
        }
    }

    void LoadNextLevel()
    {
        if (currentLevel >= totalLevels)
        {
            // Beat all levels - load victory scene or restart
            Debug.Log("All levels complete! You win!");
            if (!string.IsNullOrEmpty(victorySceneName))
            {
                SceneManager.LoadScene(victorySceneName);
            }
            else
            {
                // No victory scene, restart from level 1
                currentLevel = 1;
                SceneManager.LoadScene(level1SceneName);
            }
        }
        else
        {
            // Load next level
            currentLevel++;
            string nextSceneName = GetSceneNameForLevel(currentLevel);
            Debug.Log("Loading next level: " + nextSceneName);
            SceneManager.LoadScene(nextSceneName);
            levelComplete = false; // Reset for next level
        }
    }

    string GetSceneNameForLevel(int level)
    {
        switch (level)
        {
            case 1: return level1SceneName;
            case 2: return level2SceneName;
            case 3: return level3SceneName;
            case 4: return level4SceneName;
            case 5: return level5SceneName;
            default: return level1SceneName;
        }
    }

    public void RestartCurrentLevel()
    {
        levelComplete = false;
        string currentSceneName = GetSceneNameForLevel(currentLevel);
        SceneManager.LoadScene(currentSceneName);
    }

    public void RestartFromLevel1()
    {
        currentLevel = 1;
        levelComplete = false;
        SceneManager.LoadScene(level1SceneName);
    }
}
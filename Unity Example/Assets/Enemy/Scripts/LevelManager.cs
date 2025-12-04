using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[System.Serializable]
public class LevelData
{
    public string sceneName;
    [Tooltip("Optional: Display name for UI")]
    public string levelDisplayName;
}

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Level Configuration")]
    [Tooltip("Add your level scenes here in order. Just click + to add more!")]
    public List<LevelData> levels = new List<LevelData>();
    
    [Space(10)]
    [Tooltip("Optional: Scene to load after completing all levels")]
    public string victorySceneName = "Victory";

    [Header("Win Conditions")]
    public bool winByKillingEnemy = true;
    public bool winByReachingTrigger = false;

    [Header("Transition Settings")]
    [Tooltip("Delay before loading next level after win")]
    public float nextLevelDelay = 2f;

    [Header("Debug Info (Read Only)")]
    [SerializeField] private int currentLevelIndex = 0;
    [SerializeField] private string currentSceneName;
    [SerializeField] private bool levelComplete = false;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        UpdateCurrentLevel();
        LogLevelInfo();
    }

    void UpdateCurrentLevel()
    {
        currentSceneName = SceneManager.GetActiveScene().name;
        
        // Find which level we're on
        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i].sceneName == currentSceneName)
            {
                currentLevelIndex = i;
                return;
            }
        }
        
        // If scene not found in list, default to first level
        Debug.LogWarning($"Scene '{currentSceneName}' not found in level list! Defaulting to first level.");
        currentLevelIndex = 0;
    }

    void LogLevelInfo()
    {
        Debug.Log("=== LEVEL MANAGER ===");
        Debug.Log($"Current Scene: {currentSceneName}");
        Debug.Log($"Level {currentLevelIndex + 1} of {levels.Count}");
        Debug.Log($"Win Conditions - Kill Enemy: {winByKillingEnemy} | Trigger: {winByReachingTrigger}");
        Debug.Log("====================");
    }

    public void EnemyDefeated()
    {
        if (winByKillingEnemy && !levelComplete)
        {
            levelComplete = true;
            Debug.Log("Enemy defeated! Level complete.");
            Invoke(nameof(LoadNextLevel), nextLevelDelay);
        }
    }

    public void TriggerReached()
    {
        if (winByReachingTrigger && !levelComplete)
        {
            levelComplete = true;
            Debug.Log("Exit trigger reached! Level complete.");
            Invoke(nameof(LoadNextLevel), nextLevelDelay);
        }
    }

    void LoadNextLevel()
    {
        // Check if this was the last level
        if (currentLevelIndex >= levels.Count - 1)
        {
            Debug.Log("All levels complete! You win!");
            LoadVictoryOrRestart();
        }
        else
        {
            // Load next level
            currentLevelIndex++;
            string nextScene = levels[currentLevelIndex].sceneName;
            Debug.Log($"Loading Level {currentLevelIndex + 1}: {nextScene}");
            SceneManager.LoadScene(nextScene);
            levelComplete = false;
        }
    }

    void LoadVictoryOrRestart()
    {
        if (!string.IsNullOrEmpty(victorySceneName))
        {
            SceneManager.LoadScene(victorySceneName);
        }
        else
        {
            // No victory scene set, restart from beginning
            RestartFromBeginning();
        }
    }

    public void RestartCurrentLevel()
    {
        levelComplete = false;
        if (currentLevelIndex >= 0 && currentLevelIndex < levels.Count)
        {
            SceneManager.LoadScene(levels[currentLevelIndex].sceneName);
        }
    }

    public void RestartFromBeginning()
    {
        currentLevelIndex = 0;
        levelComplete = false;
        if (levels.Count > 0)
        {
            SceneManager.LoadScene(levels[0].sceneName);
        }
        else
        {
            Debug.LogError("No levels configured in LevelManager!");
        }
    }

    public void LoadSpecificLevel(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < levels.Count)
        {
            currentLevelIndex = levelIndex;
            levelComplete = false;
            SceneManager.LoadScene(levels[levelIndex].sceneName);
        }
        else
        {
            Debug.LogError($"Invalid level index: {levelIndex}");
        }
    }

    // Utility methods
    public int GetCurrentLevelNumber() => currentLevelIndex + 1;
    public int GetTotalLevels() => levels.Count;
    public string GetCurrentLevelName() => levels[currentLevelIndex].levelDisplayName;
}
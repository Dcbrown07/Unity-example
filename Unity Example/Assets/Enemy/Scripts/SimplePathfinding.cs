using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SimplePathfinding : MonoBehaviour
{
    public static SimplePathfinding Instance { get; private set; }

    [Header("Settings")]
    public float raycastSpacing = 1f;
    public LayerMask obstacleLayer;
    public LayerMask groundLayer;
    public float maxJumpHeight = 3f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void EnsureInstance()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("SimplePathfinding");
            go.AddComponent<SimplePathfinding>();
            Debug.Log("SimplePathfinding auto-created!");
        }
    }

    // Simple raycasting pathfinding - find direct path or simple waypoint
    public List<Vector2> FindPath(Vector2 start, Vector2 goal)
    {
        List<Vector2> path = new List<Vector2>();

        // Check if direct path is clear
        if (IsPathClear(start, goal))
        {
            path.Add(start);
            path.Add(goal);
            return path;
        }

        // Try to find intermediate waypoint
        Vector2 direction = (goal - start).normalized;
        float distance = Vector2.Distance(start, goal);
        
        // Sample points along the way
        for (float d = raycastSpacing; d < distance; d += raycastSpacing)
        {
            Vector2 checkPoint = start + direction * d;
            
            // Try going up to find a clear path
            for (float height = 0; height <= maxJumpHeight; height += raycastSpacing)
            {
                Vector2 elevated = checkPoint + Vector2.up * height;
                
                if (IsPathClear(start, elevated) && IsPathClear(elevated, goal))
                {
                    path.Add(start);
                    path.Add(elevated);
                    path.Add(goal);
                    return path;
                }
            }
        }

        // No path found, return direct line anyway
        path.Add(start);
        path.Add(goal);
        return path;
    }

    bool IsPathClear(Vector2 from, Vector2 to)
    {
        float distance = Vector2.Distance(from, to);
        Vector2 direction = (to - from).normalized;

        RaycastHit2D hit = Physics2D.Raycast(from, direction, distance, obstacleLayer);
        return hit.collider == null;
    }

    public bool RequiresJump(Vector2 from, Vector2 to)
    {
        // Check if target is significantly higher
        return to.y > from.y + 0.5f;
    }
}
using UnityEngine;

public class LevelExit : MonoBehaviour
{
    [Header("Visual Feedback")]
    public bool showGizmo = true;
    public Color gizmoColor = Color.green;

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if player entered the trigger
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player reached the exit!");
            
            // Tell the level manager the trigger was reached
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.TriggerReached();
            }
            else
            {
                Debug.LogWarning("LevelManager not found in scene!");
            }
        }
    }

    void OnDrawGizmos()
    {
        if (showGizmo)
        {
            Gizmos.color = gizmoColor;
            
            // Draw the trigger area
            BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(boxCollider.offset, boxCollider.size);
            }
            
            CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
            if (circleCollider != null)
            {
                Gizmos.DrawWireSphere(transform.position, circleCollider.radius);
            }
        }
    }
}
using UnityEngine;

public class AdvancedCameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The player or object to follow")]
    public Transform target;
    
    [Header("Follow Settings")]
    [Tooltip("How smooth the camera follows (higher = smoother)")]
    [Range(1f, 30f)]
    public float smoothSpeed = 10f;
    
    [Tooltip("Separate vertical smoothing to prevent jump jerk")]
    [Range(1f, 30f)]
    public float verticalSmoothSpeed = 5f;
    
    [Tooltip("Offset from target position")]
    public Vector3 offset = new Vector3(0, 0, -10);
    
    [Header("Prediction (Reduces Lag)")]
    [Tooltip("Look ahead based on player velocity")]
    public bool usePrediction = true;
    
    [Tooltip("How far ahead to look (0 = none, 1 = aggressive)")]
    [Range(0f, 2f)]
    public float predictionAmount = 0.5f;

    [Header("Zoom Settings")]
    [Tooltip("Camera zoom level (lower = more zoomed in)")]
    [Range(1f, 20f)]
    public float cameraSize = 5f;
    
    [Tooltip("Enable smooth zoom transitions")]
    public bool smoothZoom = true;
    
    [Tooltip("Zoom transition speed")]
    [Range(1f, 15f)]
    public float zoomSpeed = 5f;

    [Header("Camera Boundaries (Optional)")]
    [Tooltip("Enable camera movement restrictions")]
    public bool useBounds = false;
    
    [Space(5)]
    public float minX = -10f;
    public float maxX = 10f;
    public float minY = -10f;
    public float maxY = 10f;
    
    [Header("Deadzone (Prevents Jitter)")]
    [Tooltip("Player can move this much before camera follows")]
    public bool useDeadzone = false;
    
    [Range(0f, 3f)]
    public float deadzoneRadius = 0.5f;

    [Header("Editor Preview")]
    [Tooltip("Show camera view in scene")]
    public bool showPreview = true;
    
    [Tooltip("Show boundaries in scene")]
    public bool showBounds = true;

    private Camera cam;
    private Vector3 velocity = Vector3.zero;
    private Vector3 lastTargetPosition;

    void Start()
    {
        cam = GetComponent<Camera>();
        
        if (cam != null)
        {
            cam.orthographicSize = cameraSize;
        }
        
        if (target == null)
        {
            Debug.LogWarning("No target assigned to camera!");
        }
        else
        {
            lastTargetPosition = target.position;
            // Snap camera to target on start (no lerp)
            transform.position = target.position + offset;
        }
    }

    void LateUpdate()
    {
        HandleZoom();
        HandleFollow();
    }

    void HandleZoom()
    {
        if (cam == null) return;

        if (smoothZoom)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, cameraSize, zoomSpeed * Time.deltaTime);
        }
        else
        {
            cam.orthographicSize = cameraSize;
        }
    }

    void HandleFollow()
    {
        if (target == null) return;

        // Calculate target velocity for prediction
        Vector3 targetVelocity = Vector3.zero;
        if (usePrediction && Time.deltaTime > 0)
        {
            targetVelocity = (target.position - lastTargetPosition) / Time.deltaTime;
        }
        lastTargetPosition = target.position;

        // Desired position with prediction
        Vector3 predictedPosition = target.position + (targetVelocity * predictionAmount * Time.deltaTime);
        Vector3 desiredPosition = predictedPosition + offset;

        // Deadzone check
        if (useDeadzone)
        {
            float distanceToTarget = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.y),
                new Vector2(desiredPosition.x, desiredPosition.y)
            );
            
            if (distanceToTarget < deadzoneRadius)
            {
                return; // Don't move camera if within deadzone
            }
        }

        // Apply boundaries if enabled
        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        // Keep camera Z from offset
        desiredPosition.z = offset.z;
        
        // Separate horizontal and vertical smoothing to prevent jump jerk
        Vector3 currentPos = transform.position;
        
        // Smooth horizontal movement
        float newX = Mathf.SmoothDamp(
            currentPos.x,
            desiredPosition.x,
            ref velocity.x,
            1f / smoothSpeed
        );
        
        // Smoother vertical movement (prevents jump jerk)
        float newY = Mathf.SmoothDamp(
            currentPos.y,
            desiredPosition.y,
            ref velocity.y,
            1f / verticalSmoothSpeed
        );
        
        transform.position = new Vector3(newX, newY, desiredPosition.z);
    }

    void OnDrawGizmos()
    {
        Camera gizmoCam = GetComponent<Camera>();
        if (gizmoCam == null || !showPreview) return;

        // Calculate camera view dimensions
        float height = cameraSize * 2f;
        float width = height * gizmoCam.aspect;

        // Determine preview position
        Vector3 previewPos = transform.position;
        if (!Application.isPlaying && target != null)
        {
            previewPos = target.position + offset;
            
            if (useBounds)
            {
                previewPos.x = Mathf.Clamp(previewPos.x, minX, maxX);
                previewPos.y = Mathf.Clamp(previewPos.y, minY, maxY);
            }
            
            previewPos.z = transform.position.z;
        }

        // Draw camera view rectangle
        Gizmos.color = new Color(1f, 1f, 0f, 0.6f);
        
        Vector3 bottomLeft = previewPos + new Vector3(-width / 2, -height / 2, 0);
        Vector3 topLeft = previewPos + new Vector3(-width / 2, height / 2, 0);
        Vector3 topRight = previewPos + new Vector3(width / 2, height / 2, 0);
        Vector3 bottomRight = previewPos + new Vector3(width / 2, -height / 2, 0);

        Gizmos.DrawLine(bottomLeft, topLeft);
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);

        // Draw center cross
        float crossSize = 0.5f;
        Gizmos.DrawLine(previewPos + Vector3.left * crossSize, previewPos + Vector3.right * crossSize);
        Gizmos.DrawLine(previewPos + Vector3.down * crossSize, previewPos + Vector3.up * crossSize);

        // Draw deadzone
        if (useDeadzone)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(previewPos, deadzoneRadius);
        }

        // Draw boundaries
        if (useBounds && showBounds)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
            
            Vector3 boundBottomLeft = new Vector3(minX, minY, previewPos.z);
            Vector3 boundTopLeft = new Vector3(minX, maxY, previewPos.z);
            Vector3 boundTopRight = new Vector3(maxX, maxY, previewPos.z);
            Vector3 boundBottomRight = new Vector3(maxX, minY, previewPos.z);

            Gizmos.DrawLine(boundBottomLeft, boundTopLeft);
            Gizmos.DrawLine(boundTopLeft, boundTopRight);
            Gizmos.DrawLine(boundTopRight, boundBottomRight);
            Gizmos.DrawLine(boundBottomRight, boundBottomLeft);
        }
    }
}
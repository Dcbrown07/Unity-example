using UnityEngine;

public class SimpleCameraFollow : MonoBehaviour
{
    public Transform target; // Player to follow
    public float smoothSpeed = 0.125f; // How smooth the camera follows
    public Vector3 offset; // Optional offset from player

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        desiredPosition.z = transform.position.z; // Keep camera z fixed
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
    }
}
using UnityEngine;

public class WaterfallZone : MonoBehaviour
{
    public float downwardForce = 5f;
    public float swimForce = 8f;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();

            // Pull down
            rb.linearVelocity += Vector2.down * downwardForce * Time.deltaTime;

            // Swim up if pressing input
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                rb.linearVelocity += Vector2.up * swimForce * Time.deltaTime;
            }
        }
    }
}

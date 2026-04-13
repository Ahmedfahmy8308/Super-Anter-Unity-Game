using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MarioEntityMovement : MonoBehaviour
{
    public float speed = 1f;
    public Vector2 direction = Vector2.left;

    private Rigidbody2D rb;
    private Vector2 velocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        enabled = false;
    }

    private void OnBecameVisible()
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isPaused) return;
#endif
        enabled = true;
    }

    private void OnBecameInvisible()
    {
        enabled = false;
    }

    private void OnEnable()
    {
        rb.WakeUp();
    }

    private void OnDisable()
    {
        rb.linearVelocity = Vector2.zero;
        rb.Sleep();
    }

    private void FixedUpdate()
    {
        velocity.x = direction.x * speed;
        velocity.y += Physics2D.gravity.y * Time.fixedDeltaTime;

        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);

        if (rb.Raycast(Vector2.down))
            velocity.y = Mathf.Max(velocity.y, 0f);

        if (rb.Raycast(direction))
            direction = -direction;

        if (direction.x > 0f)
            transform.localEulerAngles = new Vector3(0f, 180f, 0f);
        else if (direction.x < 0f)
            transform.localEulerAngles = Vector3.zero;
    }
}

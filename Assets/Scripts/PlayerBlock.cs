using UnityEngine;

public class PlayerBlock : MonoBehaviour
{
    public Ball ball;
    public Transform hitboxView;
    public float netRange = 1.5f;
    public Vector3 hitboxSize = new Vector3(1f, 1f, 0.6f);
    public Vector3 hitboxOffset = new Vector3(0f, 1.2f, 0.3f);
    public float sideShift = 0.5f;
    public float rebound = 0.6f;

    PlayerMovement movement;
    PlayerPass pass;
    bool wasGrounded = true;
    bool touched;
    Vector3 lastBall;

    /// <summary>True while in a block jump: no air drift, and A / D shift the hands instead.</summary>
    public bool Blocking { get; private set; }

    void Start()
    {
        movement = GetComponent<PlayerMovement>();
        pass = GetComponent<PlayerPass>();
    }

    void Update()
    {
        bool grounded = movement.Grounded;
        if (wasGrounded && !grounded)
        {
            Blocking = transform.position.z > -netRange && EnemyBall();
            touched = false;
        }
        if (grounded) Blocking = false;
        wasGrounded = grounded;

        if (hitboxView) hitboxView.gameObject.SetActive(Blocking);
        if (!Blocking) return;

        Vector3 center = transform.position + hitboxOffset + Vector3.right * Mathf.Round(movement.InputDir.x) * sideShift;
        if (hitboxView)
        {
            hitboxView.position = center;
            hitboxView.localScale = hitboxSize;
        }
        Bounds box = new Bounds(center, hitboxSize + Vector3.one * ball.transform.localScale.x);
        Vector3 step = ball.transform.position - lastBall;
        if (!touched && EnemyBall() && ball.Velocity.z < 0f && box.IntersectRay(new Ray(lastBall, step), out float dist) && dist <= step.magnitude)
        {
            touched = true;
            ball.transform.position = lastBall + step.normalized * Mathf.Max(dist, 0f);
            Vector3 v = ball.Velocity;
            ball.Hit(new Vector3(v.x, v.y, -v.z) * rebound, transform);
            pass.Show("Block!");
        }
    }

    void LateUpdate() => lastBall = ball.transform.position;

    bool EnemyBall() => !ball.Dead && ball.LastTouch && ball.LastTouch.position.z > 0f;
}

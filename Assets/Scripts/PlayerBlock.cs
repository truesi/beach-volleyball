using UnityEngine;

public class PlayerBlock : MonoBehaviour
{
    public Ball ball;
    public Transform hitboxView;
    public float netRange = 1.5f;
    public Vector3 hitboxSize = new Vector3(1f, 1f, 0.6f);
    public Vector3 hitboxOffset = new Vector3(0f, 1.2f, 0.3f);
    public float sideShift = 0.5f;
    public float stuffSpeed = 10f;
    public float deflectHeight = 3f;
    public float deflectDistance = 2f;
    public float missSpeed = 0.6f;

    PlayerMovement movement;
    PlayerPass pass;
    bool wasGrounded = true;
    bool touched;

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
        // ponytail: per-frame overlap test, a 22 m/s spike moves ~0.37 m per frame at 60 fps; sweep the ball's path if it tunnels at low fps
        if (!touched && EnemyBall() && ball.Velocity.z < 0f && Inside(ball.transform.position - center))
        {
            touched = true;
            Block();
        }
    }

    bool EnemyBall() => !ball.Dead && ball.LastTouch && ball.LastTouch.position.z > 0f;

    bool Inside(Vector3 d)
    {
        float r = ball.transform.localScale.x * 0.5f;
        return Mathf.Abs(d.x) <= hitboxSize.x * 0.5f + r
            && Mathf.Abs(d.y) <= hitboxSize.y * 0.5f + r
            && Mathf.Abs(d.z) <= hitboxSize.z * 0.5f + r;
    }

    void Block()
    {
        float t = Mathf.Abs(movement.Velocity.y) / movement.gravity;
        string when = movement.Velocity.y > 0f ? "early" : "late";
        if (t <= pass.perfectWindow)
        {
            pass.Show("Block: Perfect (stuff)");
            ball.Hit(new Vector3(0f, -1f, 1f).normalized * stuffSpeed, transform);
        }
        else if (t <= pass.goodWindow)
        {
            pass.Show("Block: Good (" + when + ")");
            ball.Pass(transform, Vector3.back, deflectDistance, deflectHeight, pass.goodSpread);
        }
        else
        {
            pass.Show("Block: Miss (" + when + ", tipped)");
            ball.Hit(Ball.Tilt(ball.Velocity * missSpeed, pass.goodSpread), transform);
        }
    }
}

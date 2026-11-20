using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float maxSpeed = 6f;
    public float acceleration = 40f;
    public float deceleration = 12f;
    public float jumpHeight = 1.2f;
    public float gravity = 30f;
    public float xLimit = 3f;
    public float minZ = -6f;
    public float maxZ = -0.5f;

    /// <summary>True while the player stands on the sand.</summary>
    public bool Grounded => transform.position.y <= groundY;

    /// <summary>Normalized WASD direction this frame, world-relative (W = toward the net).</summary>
    public Vector3 InputDir { get; private set; }

    Vector3 velocity;
    float groundY;
    PlayerPass pass;

    void Start()
    {
        groundY = transform.position.y;
        pass = GetComponent<PlayerPass>();
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        Vector3 input = Vector3.zero;
        bool jump = false;
        if (kb != null)
        {
            if (kb.wKey.isPressed) input.z += 1f;
            if (kb.sKey.isPressed) input.z -= 1f;
            if (kb.dKey.isPressed) input.x += 1f;
            if (kb.aKey.isPressed) input.x -= 1f;
            jump = kb.spaceKey.wasPressedThisFrame && !(pass && pass.BallIncoming());
        }
        input = input.normalized;
        InputDir = input;

        float dt = Time.deltaTime;
        Vector3 planar = new Vector3(velocity.x, 0f, velocity.z);
        float rate = input == Vector3.zero ? deceleration : acceleration;
        planar = pass && pass.Charging ? Vector3.zero : Vector3.MoveTowards(planar, input * maxSpeed, rate * dt);
        velocity.x = planar.x;
        velocity.z = planar.z;

        if (Grounded && jump) velocity.y = Mathf.Sqrt(2f * gravity * jumpHeight);
        velocity.y -= gravity * dt;

        Vector3 p = transform.position + velocity * dt;
        float cx = Mathf.Clamp(p.x, -xLimit, xLimit);
        float cz = Mathf.Clamp(p.z, minZ, maxZ);
        if (cx != p.x) velocity.x = 0f;
        if (cz != p.z) velocity.z = 0f;
        p.x = cx;
        p.z = cz;
        if (p.y <= groundY)
        {
            p.y = groundY;
            velocity.y = 0f;
        }
        transform.position = p;
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpike : MonoBehaviour
{
    public Ball ball;
    public float minJumpHeight = 0.6f;
    public float maxJumpHeight = 1.8f;
    public float maxChargeTime = 0.5f;
    public float handHeight = 1.5f;
    public float reach = 1f;
    public float spikeSpeed = 22f;
    public float goodSpeed = 0.7f;
    public float goodSpread = 6f;
    public float tipHeight = 1f;
    public float sideX = 3f;
    public float shortZ = 1.5f;
    public float midZ = 4.5f;
    public float deepZ = 7f;

    PlayerMovement movement;
    PlayerPass pass;
    float chargeStart = -1f;
    bool swung;

    /// <summary>Jump charge from 0 to 1, or 0 when not charging.</summary>
    public float Charge01 => chargeStart >= 0f ? Mathf.Clamp01((Time.time - chargeStart) / maxChargeTime) : 0f;

    void Start()
    {
        movement = GetComponent<PlayerMovement>();
        pass = GetComponent<PlayerPass>();
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (movement.Grounded) swung = false;
        if (kb != null)
        {
            if (kb.spaceKey.wasPressedThisFrame && movement.Grounded && !pass.PassContext()) chargeStart = Time.time;
            if (chargeStart >= 0f && kb.spaceKey.wasReleasedThisFrame)
            {
                movement.Jump(Mathf.Lerp(minJumpHeight, maxJumpHeight, Charge01));
                chargeStart = -1f;
            }
        }
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && !movement.Grounded && !swung)
        {
            swung = true;
            Swing();
        }
    }

    void Swing()
    {
        Vector3 toBall = ball.transform.position - (transform.position + Vector3.up * handHeight);
        float early = toBall.y / Mathf.Max(movement.Velocity.y - ball.Velocity.y, 1f);
        toBall.y = 0f;
        float t = Mathf.Abs(early);

        if (ball.Dead || !ball.TurnOf(transform, pass.teammate) || toBall.magnitude > reach
            || t > pass.shankWindow || (t > pass.goodWindow && early > 0f))
        {
            pass.Show("Spike: Miss (whiff)");
            return;
        }

        Vector3 target = Target();
        if (t > pass.goodWindow)
        {
            pass.Show("Spike: Miss (late, tip)");
            Vector3 d = new Vector3(target.x, 0f, shortZ) - transform.position;
            d.y = 0f;
            ball.Pass(transform, d.normalized, d.magnitude, tipHeight, pass.goodSpread);
            return;
        }

        float speed = spikeSpeed, spread = 0f;
        if (t <= pass.perfectWindow) pass.Show("Spike: Perfect");
        else
        {
            pass.Show(early > 0f ? "Spike: Good (early)" : "Spike: Good (late)");
            speed *= goodSpeed;
            spread = goodSpread;
        }
        ball.Hit(Ball.Tilt((target - ball.transform.position).normalized * speed, spread), transform);
    }

    Vector3 Target()
    {
        Vector3 dir = movement.InputDir;
        float z = dir.z > 0.1f ? deepZ : dir.z < -0.1f ? shortZ : midZ;
        return new Vector3(Mathf.Round(dir.x) * sideX, 0f, z);
    }

    void OnGUI()
    {
        if (chargeStart >= 0f) pass.DrawChargeBar(Charge01);
    }
}

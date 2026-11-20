using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPass : MonoBehaviour
{
    public Ball ball;
    public Transform teammate;
    public float assistAngle = 45f;
    public Vector3 contactOffset = new Vector3(0f, -0.25f, 0.7f);
    public float incomingRadius = 2.5f;
    public float reach = 1.5f;
    public float maxChargeTime = 0.8f;
    public float minPassHeight = 1.5f;
    public float maxPassHeight = 7f;
    public float passDistance = 3f;
    public float perfectWindow = 0.07f;
    public float goodWindow = 0.18f;
    public float shankWindow = 0.35f;
    public float goodPower = 0.9f;
    public float goodSpread = 12f;
    public float shankPower = 0.5f;
    public float shankSpread = 45f;
    public float chargeBarHeight = 1.4f;
    public Vector2 chargeBarSize = new Vector2(120f, 16f);

    PlayerMovement movement;
    float chargeStart = -1f;
    string result;
    float resultUntil;

    /// <summary>True while Space is held to charge a pass.</summary>
    public bool Charging => chargeStart >= 0f;

    /// <summary>Pass charge from 0 to 1, or 0 when not charging.</summary>
    public float Charge01 => Charging ? Mathf.Clamp01((Time.time - chargeStart) / maxChargeTime) : 0f;

    void Start() => movement = GetComponent<PlayerMovement>();

    /// <summary>True when a Space press should start charging a pass instead of jumping.</summary>
    public bool BallIncoming()
    {
        Vector3 d = ball.LandingPoint - transform.position;
        d.y = 0f;
        return !ball.Dead && ball.Velocity.y < 0f && d.magnitude <= incomingRadius && ball.TurnOf(transform, teammate);
    }

    /// <summary>True when Space should charge a pass: standing still on the sand with the ball dropping toward you. Otherwise Space charges a jump.</summary>
    public bool PassContext() => movement.Grounded && movement.InputDir == Vector3.zero && BallIncoming();

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;
        if (kb.spaceKey.wasPressedThisFrame && PassContext()) chargeStart = Time.time;
        if (chargeStart >= 0f && kb.spaceKey.wasReleasedThisFrame)
        {
            float charge = Charge01;
            chargeStart = -1f;
            Release(charge);
        }
    }

    void Release(float charge)
    {
        Vector3 toBall = ball.transform.position - (transform.position + contactOffset);
        float early = toBall.y / Mathf.Max(-ball.Velocity.y, 0.1f);
        toBall.y = 0f;
        float t = Mathf.Abs(early);

        if (ball.Dead || toBall.magnitude > reach || t > shankWindow)
        {
            Show("Miss (whiff)");
            return;
        }

        float power = 1f, spread = 0f;
        if (t <= perfectWindow) Show("Perfect");
        else if (t <= goodWindow) { Show(early > 0f ? "Good (early)" : "Good (late)"); power = goodPower; spread = goodSpread; }
        else { Show(early > 0f ? "Miss (shank, early)" : "Miss (shank, late)"); power = shankPower; spread = shankSpread; }

        Vector3 dir = movement.InputDir;
        float distance = passDistance;
        Vector3 toMate = teammate.position - transform.position;
        toMate.y = 0f;
        if (dir != Vector3.zero && Vector3.Angle(dir, toMate) <= assistAngle)
        {
            dir = toMate.normalized;
            distance = toMate.magnitude;
        }
        ball.Pass(transform, dir, distance, Mathf.Lerp(minPassHeight, maxPassHeight, charge) * power, spread);
    }

    /// <summary>Shows <paramref name="text"/> as the touch result in the top-left corner for 1.5 s.</summary>
    public void Show(string text)
    {
        result = text;
        resultUntil = Time.time + 1.5f;
    }

    void OnGUI()
    {
        GUI.skin.label.fontSize = 28;
        if (Charging) DrawChargeBar(Charge01);
        if (Time.time < resultUntil) GUI.Label(new Rect(20, 20, 600, 40), result);
    }

    /// <summary>Draws a charge bar above the player's head, filled to <paramref name="charge"/> (0 to 1).</summary>
    public void DrawChargeBar(float charge)
    {
        Vector3 s = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * chargeBarHeight);
        if (s.z < 0f) return;
        Rect back = new Rect(s.x - chargeBarSize.x * 0.5f, Screen.height - s.y, chargeBarSize.x, chargeBarSize.y);
        GUI.DrawTexture(back, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, new Color(0f, 0f, 0f, 0.6f), 0f, 0f);
        Rect fill = new Rect(back.x + 2f, back.y + 2f, (back.width - 4f) * charge, back.height - 4f);
        Color color = charge < 1f ? Color.Lerp(Color.green, Color.yellow, charge) : Color.red;
        GUI.DrawTexture(fill, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, color, 0f, 0f);
    }
}

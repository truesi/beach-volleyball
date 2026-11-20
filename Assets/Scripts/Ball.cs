using UnityEngine;

public class Ball : MonoBehaviour
{
    public Transform player;
    public Transform landingMarker;
    public float physicsDiameter = 0.21f;
    public float mass = 0.27f;
    public float dragCoefficient = 0.47f;
    public float airDensity = 1.225f;
    public float sandBounce = 0.15f;
    public float sandRoll = 0.4f;
    public float dropHeight = 8f;
    public float dropSpread = 1.5f;
    public float respawnDelay = 1f;

    public Vector3 Velocity { get; private set; }
    public Vector3 LandingPoint { get; private set; }
    public bool Dead { get; private set; }

    /// <summary>Who touched the ball last, or null for a fresh ball.</summary>
    public Transform LastTouch { get; private set; }

    float radius;

    void Start()
    {
        radius = transform.localScale.x * 0.5f;
        Respawn();
    }

    /// <summary>Sends the ball off with a new velocity, as a touch by <paramref name="by"/> does.</summary>
    public void Hit(Vector3 velocity, Transform by)
    {
        Velocity = velocity;
        LastTouch = by;
    }

    /// <summary>Pass: rises <paramref name="height"/> above the contact, travels about <paramref name="distance"/> along flat <paramref name="dir"/> (drag shortens it), tilted randomly by up to <paramref name="spread"/> degrees.</summary>
    public void Pass(Transform by, Vector3 dir, float distance, float height, float spread)
    {
        float g = -Physics.gravity.y;
        float vy = Mathf.Sqrt(2f * g * height);
        Vector3 v = dir * (distance / (2f * vy / g)) + Vector3.up * vy;
        Vector3 axis = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.right;
        Hit(Quaternion.AngleAxis(Random.Range(0f, spread), axis) * v, by);
    }

    /// <summary>True when <paramref name="who"/> plays the ball next: no double touches, and a fresh ball goes to whoever is closer to the landing point.</summary>
    public bool TurnOf(Transform who, Transform partner)
    {
        if (LastTouch == partner) return true;
        if (LastTouch == who) return false;
        return FlatDistance(who) <= FlatDistance(partner);
    }

    float FlatDistance(Transform t)
    {
        Vector3 d = LandingPoint - t.position;
        d.y = 0f;
        return d.magnitude;
    }

    void Update()
    {
        Vector3 p = transform.position;
        Vector3 v = Velocity;
        Step(ref p, ref v, Time.deltaTime);
        if (p.y < radius)
        {
            p.y = radius;
            if (v.y < 0f)
            {
                v.y = -v.y * sandBounce;
                v.x *= sandRoll;
                v.z *= sandRoll;
            }
            if (!Dead)
            {
                Dead = true;
                Invoke(nameof(Respawn), respawnDelay);
            }
        }
        transform.position = p;
        Velocity = v;

        LandingPoint = PredictLanding();
        if (landingMarker) landingMarker.position = LandingPoint + Vector3.up * 0.02f;
    }

    void Step(ref Vector3 p, ref Vector3 v, float dt)
    {
        float r = physicsDiameter * 0.5f;
        float k = 0.5f * airDensity * dragCoefficient * Mathf.PI * r * r / mass;
        v += (Physics.gravity - k * v.magnitude * v) * dt;
        p += v * dt;
    }

    Vector3 PredictLanding()
    {
        Vector3 p = transform.position;
        Vector3 v = Velocity;
        for (int i = 0; i < 500 && p.y > radius; i++) Step(ref p, ref v, 0.02f);
        return new Vector3(p.x, 0f, p.z);
    }

    void Respawn()
    {
        Dead = false;
        LastTouch = null;
        Vector2 r = Random.insideUnitCircle * dropSpread;
        transform.position = new Vector3(player.position.x + r.x, dropHeight, player.position.z + r.y);
        Velocity = Vector3.zero;
    }
}

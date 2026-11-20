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

    float radius;

    void Start()
    {
        radius = transform.localScale.x * 0.5f;
        Respawn();
    }

    /// <summary>Sends the ball off with a new velocity, as a touch does.</summary>
    public void Hit(Vector3 velocity) => Velocity = velocity;

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
        Vector2 r = Random.insideUnitCircle * dropSpread;
        transform.position = new Vector3(player.position.x + r.x, dropHeight, player.position.z + r.y);
        Velocity = Vector3.zero;
    }
}

using UnityEngine;

public class TeammateBot : MonoBehaviour
{
    public Ball ball;
    public PlayerPass partner;
    public Vector3 home = new Vector3(2f, 1f, -4f);
    public float contactDrop = 0.25f;
    public float passHeight = 4f;
    public float setHeight = 5f;
    public float setNetGap = 1f;
    [Range(0f, 1f)] public float perfectOdds = 0.7f;
    [Range(0f, 1f)] public float goodOdds = 0.25f;

    PlayerMovement partnerMove;
    Vector3 velocity;
    string result;
    float resultUntil;

    void Start() => partnerMove = partner.GetComponent<PlayerMovement>();

    void Update()
    {
        bool mine = !ball.Dead && ball.LandingPoint.z < 0f && ball.TurnOf(transform, partner.transform);
        Vector3 target = mine ? ball.LandingPoint : home;
        target.x = Mathf.Clamp(target.x, -partnerMove.xLimit, partnerMove.xLimit);
        target.z = Mathf.Clamp(target.z, partnerMove.minZ, partnerMove.maxZ);
        Run(target);
        if (mine) TryPass();
    }

    void Run(Vector3 target)
    {
        Vector3 to = target - transform.position;
        to.y = 0f;
        float speed = Mathf.Min(partnerMove.maxSpeed, Mathf.Sqrt(2f * partnerMove.deceleration * to.magnitude));
        velocity = Vector3.MoveTowards(velocity, to.normalized * speed, partnerMove.acceleration * Time.deltaTime);
        transform.position += velocity * Time.deltaTime;
    }

    void TryPass()
    {
        Vector3 toBall = ball.transform.position - transform.position;
        if (ball.Velocity.y >= 0f || toBall.y > -contactDrop) return;
        toBall.y = 0f;
        if (toBall.magnitude > partner.reach) return;

        bool set = ball.LastTouch == partner.transform;
        string touch = set ? " set" : " pass";
        float roll = Random.value, power = 1f, spread = 0f;
        if (roll < perfectOdds) Show("Perfect" + touch);
        else if (roll < perfectOdds + goodOdds) { Show("Good" + touch); power = partner.goodPower; spread = partner.goodSpread; }
        else { Show("Miss (shank)" + touch); power = partner.shankPower; spread = partner.shankSpread; }

        Vector3 to = set ? new Vector3(partner.transform.position.x, 0f, -setNetGap) : partner.transform.position;
        to -= transform.position;
        to.y = 0f;
        ball.Pass(transform, to.normalized, to.magnitude, (set ? setHeight : passHeight) * power, spread);
    }

    void Show(string text)
    {
        result = "Teammate: " + text;
        resultUntil = Time.time + 1.5f;
    }

    void OnGUI()
    {
        if (Time.time < resultUntil) GUI.Label(new Rect(20, 60, 600, 40), result);
    }
}

using UnityEngine;

public class EnemyBot : MonoBehaviour
{
    public Ball ball;
    public EnemyBot partner;
    public PlayerSpike player;
    public Vector3 home = new Vector3(2f, 1f, 4f);
    public float contactDrop = 0.25f;
    public float passHeight = 4f;
    public float setHeight = 5f;
    public float setNetGap = 1f;
    public float apexMargin = 0.3f;
    public bool aimCenter = true;
    [Range(0f, 1f)] public float perfectOdds = 0.7f;
    [Range(0f, 1f)] public float goodOdds = 0.25f;
    public float labelY = 100f;

    PlayerMovement move;
    PlayerPass pass;
    Vector3 velocity;
    float groundY;
    bool swung;
    string result;
    float resultUntil;

    bool Grounded => transform.position.y <= groundY;

    void Start()
    {
        move = player.GetComponent<PlayerMovement>();
        pass = player.GetComponent<PlayerPass>();
        groundY = transform.position.y;
    }

    void Update()
    {
        bool turn = !ball.Dead && ball.TurnOf(transform, partner.transform);
        bool attack = turn && ball.LastTouch == partner.transform && ball.Touches == 2 && ball.transform.position.z > 0f;
        bool mine = turn && ball.LandingPoint.z > 0f;
        if (Grounded) swung = false;
        Run(attack ? ApproachAndJump() : mine ? ball.LandingPoint : home);
        if (attack) TrySpike();
        else if (mine && Grounded) TryPass();
    }

    Vector3 ApproachAndJump()
    {
        float apex = ball.PredictFall(float.MaxValue, out _).y;
        float hand = Mathf.Min(groundY + player.maxJumpHeight + player.handHeight, apex - apexMargin);
        Vector3 contact = ball.PredictFall(hand, out float time);
        float height = Mathf.Clamp(hand - groundY - player.handHeight, player.minJumpHeight, player.maxJumpHeight);
        if (Grounded && time <= Mathf.Sqrt(2f * height / move.gravity)) velocity.y = Mathf.Sqrt(2f * move.gravity * height);
        return contact;
    }

    void Run(Vector3 target)
    {
        target.x = Mathf.Clamp(target.x, -move.xLimit, move.xLimit);
        target.z = Mathf.Clamp(target.z, -move.maxZ, -move.minZ);
        Vector3 to = target - transform.position;
        to.y = 0f;
        float dt = Time.deltaTime;
        float speed = Mathf.Min(move.maxSpeed, Mathf.Sqrt(2f * move.deceleration * to.magnitude));
        Vector3 planar = Vector3.MoveTowards(new Vector3(velocity.x, 0f, velocity.z), to.normalized * speed, move.acceleration * dt);
        velocity = new Vector3(planar.x, velocity.y - move.gravity * dt, planar.z);
        Vector3 p = transform.position + velocity * dt;
        if (p.y <= groundY)
        {
            p.y = groundY;
            velocity.y = 0f;
        }
        transform.position = p;
    }

    void TryPass()
    {
        Vector3 toBall = ball.transform.position - transform.position;
        if (ball.Velocity.y >= 0f || toBall.y > -contactDrop) return;
        toBall.y = 0f;
        if (toBall.magnitude > pass.reach) return;

        bool set = ball.LastTouch == partner.transform;
        int grade = Roll(set ? "set" : "dig");
        float power = grade == 0 ? 1f : grade == 1 ? pass.goodPower : pass.shankPower;
        float spread = grade == 0 ? 0f : grade == 1 ? pass.goodSpread : pass.shankSpread;
        Vector3 to = set ? new Vector3(partner.transform.position.x, 0f, setNetGap) : partner.transform.position;
        to -= transform.position;
        to.y = 0f;
        ball.Pass(transform, to.normalized, (set ? 2f : 1f) * to.magnitude, (set ? setHeight : passHeight) * power, spread);
    }

    void TrySpike()
    {
        Vector3 toBall = ball.transform.position - (transform.position + Vector3.up * player.handHeight);
        if (Grounded || swung || toBall.y > 0f) return;
        swung = true;
        toBall.y = 0f;
        if (toBall.magnitude > player.reach)
        {
            Show("Miss spike (whiff)");
            return;
        }

        Vector3 target = aimCenter ? new Vector3(0f, 0f, -player.midZ) : OpenSand();
        int grade = Roll("spike");
        if (grade == 2)
        {
            Vector3 d = new Vector3(target.x, 0f, -player.shortZ) - transform.position;
            d.y = 0f;
            ball.Pass(transform, d.normalized, d.magnitude, player.tipHeight, pass.goodSpread);
            return;
        }
        Vector3 v = (target - ball.transform.position).normalized * player.spikeSpeed;
        ball.Hit(grade == 0 ? v : Ball.Tilt(v * player.goodSpeed, player.goodSpread), transform);
    }

    Vector3 OpenSand()
    {
        Vector3 best = Vector3.zero;
        float bestGap = -1f;
        foreach (float x in new[] { -player.sideX, 0f, player.sideX })
        foreach (float z in new[] { player.shortZ, player.midZ, player.deepZ })
        {
            Vector3 t = new Vector3(x, 0f, -z);
            float gap = Mathf.Min(Flat(t - player.transform.position), Flat(t - pass.teammate.position));
            if (gap > bestGap)
            {
                bestGap = gap;
                best = t;
            }
        }
        return best;
    }

    static float Flat(Vector3 d) => new Vector2(d.x, d.z).magnitude;

    int Roll(string touch)
    {
        float r = Random.value;
        int grade = r < perfectOdds ? 0 : r < perfectOdds + goodOdds ? 1 : 2;
        Show(new[] { "Perfect ", "Good ", "Miss " }[grade] + touch);
        return grade;
    }

    void Show(string text)
    {
        result = name + ": " + text;
        resultUntil = Time.time + 1.5f;
    }

    void OnGUI()
    {
        if (Time.time < resultUntil) GUI.Label(new Rect(20, labelY, 600, 40), result);
    }
}

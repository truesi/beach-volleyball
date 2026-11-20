using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.15f;
    public float chargeZoom = 8f;
    public float releaseKick = 5f;
    public float kickReturnTime = 0.4f;
    public float zoomSmoothTime = 0.1f;

    Vector3 offset;
    Vector3 velocity;
    Camera cam;
    PlayerPass pass;
    PlayerSpike spike;
    float baseFov;
    float fovVelocity;
    float lastCharge;
    float kick;

    void Start()
    {
        offset = transform.position - target.position;
        cam = GetComponent<Camera>();
        pass = target.GetComponent<PlayerPass>();
        spike = target.GetComponent<PlayerSpike>();
        baseFov = cam.fieldOfView;
    }

    void LateUpdate()
    {
        Vector3 goal = target.position + offset;
        goal.y = transform.position.y;
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);

        float charge = Mathf.Max(pass ? pass.Charge01 : 0f, spike ? spike.Charge01 : 0f);
        if (lastCharge > 0f && charge == 0f) kick = releaseKick * lastCharge;
        lastCharge = charge;
        kick = Mathf.MoveTowards(kick, 0f, releaseKick / kickReturnTime * Time.deltaTime);
        float goalFov = baseFov - chargeZoom * charge + kick;
        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, goalFov, ref fovVelocity, zoomSmoothTime);
    }
}

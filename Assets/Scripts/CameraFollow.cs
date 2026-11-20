using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.15f;

    Vector3 offset;
    Vector3 velocity;

    void Start() => offset = transform.position - target.position;

    void LateUpdate()
    {
        Vector3 goal = target.position + offset;
        goal.y = transform.position.y;
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }
}

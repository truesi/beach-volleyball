using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Moves one beach-volleyball character forward/back along its fixed lane (the Z axis).
/// The lane (X position) never changes — the whole game is about timing these runs:
/// forward toward the net to attack/set, back toward the baseline to receive.
///
/// Player 1 (left lane)  -> W = forward (toward net), S = back.
/// Player 2 (right lane) -> Up = forward (toward net), Down = back.
/// </summary>
public class LaneMover : MonoBehaviour
{
    [Tooltip("1 = left character (W/S), 2 = right character (Up/Down arrows).")]
    public int playerIndex = 1;

    [Tooltip("Run speed in units per second.")]
    public float speed = 3.5f;

    [Tooltip("Z at the back line (defensive position). The net sits at Z = 0.")]
    public float backZ = -3f;

    [Tooltip("Z at the most-forward position. Stops just short of the net by default.")]
    public float forwardZ = -0.8f;

    Key forwardKey;
    Key backKey;

    void Awake()
    {
        if (playerIndex == 2) { forwardKey = Key.UpArrow; backKey = Key.DownArrow; }
        else                  { forwardKey = Key.W;       backKey = Key.S; }
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return; // no keyboard connected this frame

        float dir = 0f;
        if (kb[forwardKey].isPressed) dir += 1f; // toward the net (+Z)
        if (kb[backKey].isPressed)    dir -= 1f; // toward the baseline (-Z)

        if (dir == 0f) return;

        Vector3 p = transform.localPosition;
        p.z = Mathf.Clamp(p.z + dir * speed * Time.deltaTime, backZ, forwardZ);
        transform.localPosition = p;
    }
}

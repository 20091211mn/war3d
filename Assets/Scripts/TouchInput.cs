using UnityEngine;

// Touch controls: left half = move stick, right half = look, circle button = fire.
// In the Editor / on a PC it falls back to WASD + right mouse to look + Space/left click to fire.
public class TouchInput : MonoBehaviour
{
    public static Vector2 Move, Look;
    public static bool Fire;
    public static bool StickActive;
    public static Vector2 StickOrigin, StickPos;

    static int moveId = -1, lookId = -1, fireId = -1;
    static Vector2 lastMouse;

    public static float R { get { return Screen.height * 0.11f; } }
    public static Vector2 FireCenter { get { return new Vector2(Screen.width - R * 2.2f, R * 2.2f); } }
    public static Vector2 DefaultStick { get { return new Vector2(R * 2.2f, R * 2.2f); } }

    public static void ResetAll()
    {
        moveId = lookId = fireId = -1;
        Move = Look = Vector2.zero;
        Fire = false;
        StickActive = false;
    }

    void Update()
    {
        Move = Vector2.zero;
        Look = Vector2.zero;

        if (Input.touchSupported && Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                Vector2 p = t.position;
                bool ended = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;

                if (t.phase == TouchPhase.Began)
                {
                    if (fireId < 0 && (p - FireCenter).magnitude < R * 1.4f) fireId = t.fingerId;
                    else if (moveId < 0 && p.x < Screen.width * 0.5f)
                    {
                        moveId = t.fingerId;
                        StickOrigin = p;
                        StickPos = p;
                        StickActive = true;
                    }
                    else if (lookId < 0 && p.x >= Screen.width * 0.5f) lookId = t.fingerId;
                }

                if (t.fingerId == moveId)
                {
                    if (ended) { moveId = -1; StickActive = false; }
                    else
                    {
                        StickPos = p;
                        Vector2 v = (p - StickOrigin) / (R * 1.2f);
                        Move = Vector2.ClampMagnitude(v, 1f);
                        if (Move.magnitude < 0.1f) Move = Vector2.zero;
                    }
                }
                else if (t.fingerId == lookId)
                {
                    if (ended) lookId = -1;
                    else Look += t.deltaPosition;
                }
                else if (t.fingerId == fireId)
                {
                    if (ended) fireId = -1;
                }
            }
            Fire = fireId >= 0;
        }
        else if (Input.touchSupported)
        {
            moveId = lookId = fireId = -1;
            StickActive = false;
            Fire = false;
        }
        else
        {
            float x = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            float y = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            Move = Vector2.ClampMagnitude(new Vector2(x, y), 1f);
            Vector2 m = Input.mousePosition;
            if (Input.GetMouseButton(1)) Look = m - lastMouse;
            lastMouse = m;
            Fire = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);
        }
    }
}

using UnityEngine;

// Health + ammo pack dropped by enemies.
public class Pickup : MonoBehaviour
{
    float baseY;

    public static void Create(Vector3 p)
    {
        var go = Fx.Prim(PrimitiveType.Cube, false);
        go.name = "Pickup";
        go.transform.position = new Vector3(p.x, 0.7f, p.z);
        go.transform.localScale = Vector3.one * 0.5f;
        Fx.Tint(go.GetComponent<Renderer>(), new Color(0.2f, 0.9f, 0.3f));
        go.AddComponent<Pickup>();
    }

    void Start()
    {
        baseY = transform.position.y;
    }

    void Update()
    {
        transform.Rotate(0f, 120f * Time.deltaTime, 0f);
        var p = transform.position;
        p.y = baseY + Mathf.Sin(Time.time * 3f) * 0.12f;
        transform.position = p;

        var gm = GameManager.I;
        if (gm == null || !gm.alive) return;
        Vector3 d = gm.player.transform.position - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude < 2f * 2f)
        {
            gm.player.Heal(25f);
            Destroy(gameObject);
        }
    }
}

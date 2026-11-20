using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float health = 40f, speed = 3f, damage = 7f;

    const float Range = 28f;

    float fireTimer, hitFlash, deadTimer, phase, side;
    bool dead;
    Renderer[] rends;
    Color[] cols;
    Transform player;
    Quaternion deadStart;

    public static Enemy Create(Vector3 pos, int wave)
    {
        var root = new GameObject("Enemy");
        root.transform.position = pos;

        var col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0.75f, 0f);
        col.radius = 0.4f;
        col.height = 1.5f;
        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        var body = Fx.Prim(PrimitiveType.Capsule, false);
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        body.transform.localScale = new Vector3(0.8f, 0.7f, 0.8f);
        Fx.Tint(body.GetComponent<Renderer>(), new Color(0.35f, 0.42f, 0.22f));

        var head = Fx.Prim(PrimitiveType.Sphere, true);
        head.name = "Head";
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.65f, 0f);
        head.transform.localScale = Vector3.one * 0.42f;
        Fx.Tint(head.GetComponent<Renderer>(), new Color(0.85f, 0.65f, 0.5f));

        var gun = Fx.Prim(PrimitiveType.Cube, false);
        gun.transform.SetParent(root.transform, false);
        gun.transform.localPosition = new Vector3(0.35f, 1.0f, 0.5f);
        gun.transform.localScale = new Vector3(0.1f, 0.12f, 0.8f);
        Fx.Tint(gun.GetComponent<Renderer>(), new Color(0.1f, 0.1f, 0.1f));

        var e = root.AddComponent<Enemy>();
        e.health = 40f + wave * 8f;
        e.speed = Mathf.Min(2.6f + wave * 0.25f, 6f);
        e.damage = Mathf.Min(5f + wave, 14f);
        return e;
    }

    void Start()
    {
        player = GameManager.I.player.transform;
        rends = GetComponentsInChildren<Renderer>();
        cols = new Color[rends.Length];
        var block = new MaterialPropertyBlock();
        for (int i = 0; i < rends.Length; i++)
        {
            rends[i].GetPropertyBlock(block);
            cols[i] = block.GetColor("_Color");
        }
        phase = Random.value * 6f;
        side = Random.value < 0.5f ? -1f : 1f;
        fireTimer = Random.Range(0.8f, 2.2f);
    }

    void Update()
    {
        var gm = GameManager.I;
        if (gm == null) return;
        float dt = Time.deltaTime;

        if (dead)
        {
            deadTimer += dt;
            transform.rotation = Quaternion.Slerp(deadStart, deadStart * Quaternion.Euler(-90f, 0f, 0f), Mathf.Clamp01(deadTimer * 4f));
            return;
        }

        if (hitFlash > 0f)
        {
            hitFlash -= dt;
            if (hitFlash <= 0f) SetFlash(false);
        }

        if (!gm.alive) return;

        Vector3 to = player.position - transform.position;
        to.y = 0f;
        float d = to.magnitude;
        if (d > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 8f * dt);

        Vector3 dir = to.normalized;
        Vector3 chest = transform.position + Vector3.up * 0.8f;
        if (d > 11f)
        {
            if (Physics.Raycast(chest, dir, 1.6f)) dir = Quaternion.Euler(0f, 70f * side, 0f) * dir;
            transform.position += dir * speed * dt;
        }
        else
        {
            transform.position += transform.right * Mathf.Sin(Time.time * 0.8f + phase) * speed * 0.5f * dt;
        }

        fireTimer -= dt;
        if (d < Range && fireTimer <= 0f)
        {
            fireTimer = Random.Range(0.9f, 1.8f);
            Shoot();
        }
    }

    void Shoot()
    {
        Vector3 o = transform.TransformPoint(0.35f, 1.0f, 1.0f);
        Vector3 target = player.position + Random.insideUnitSphere * 0.8f;
        Vector3 dir = (target - o).normalized;
        Vector3 end = o + dir * (Range + 10f);

        RaycastHit hit;
        if (Physics.Raycast(o, dir, out hit, Range + 10f))
        {
            end = hit.point;
            var p = hit.collider.GetComponentInParent<PlayerController>();
            if (p != null) p.Damage(damage);
        }
        Fx.Tracer(o, end, new Color(1f, 0.3f, 0.2f));
        Sfx.PlayAt(Sfx.Shot, o, 0.35f);
    }

    public void Hit(float dmg, bool head)
    {
        if (dead) return;
        health -= dmg;
        hitFlash = 0.1f;
        SetFlash(true);
        if (health <= 0f) Die(head);
    }

    void SetFlash(bool on)
    {
        if (rends == null) return;
        for (int i = 0; i < rends.Length; i++)
            Fx.Tint(rends[i], on ? new Color(1f, 0.2f, 0.2f) : cols[i]);
    }

    void Die(bool head)
    {
        dead = true;
        deadStart = transform.rotation;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        SetFlash(false);
        GameManager.I.OnKill(this, head);
        Sfx.PlayAt(Sfx.Boom, transform.position, 0.4f);
        Destroy(gameObject, 1.5f);
    }
}

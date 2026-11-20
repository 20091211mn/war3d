using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public const int MagSize = 30;

    public float health = 100f, maxHealth = 100f;
    public int ammo = MagSize;
    public bool reloading;
    public float speed = 6.5f;

    CharacterController cc;
    Transform cam, gun;
    float yaw, pitch, vy, fireTimer, reloadTimer, recoil;
    readonly Vector3 gunBase = new Vector3(0.28f, -0.25f, 0.55f);

    void Awake()
    {
        cc = gameObject.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;

        var camGo = new GameObject("PlayerCamera");
        camGo.tag = "MainCamera";
        cam = camGo.transform;
        cam.SetParent(transform, false);
        cam.localPosition = new Vector3(0f, 0.7f, 0f);
        var c = camGo.AddComponent<Camera>();
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = new Color(0.62f, 0.78f, 0.92f);
        c.fieldOfView = 70f;
        c.nearClipPlane = 0.05f;
        c.farClipPlane = 150f;
        camGo.AddComponent<AudioListener>();

        var g = Fx.Prim(PrimitiveType.Cube, false);
        gun = g.transform;
        gun.SetParent(cam, false);
        gun.localPosition = gunBase;
        gun.localScale = new Vector3(0.09f, 0.12f, 0.6f);
        Fx.Tint(g.GetComponent<Renderer>(), new Color(0.12f, 0.12f, 0.12f));
    }

    public void ResetPlayer()
    {
        health = maxHealth;
        ammo = MagSize;
        reloading = false;
        yaw = 0f;
        pitch = 0f;
        vy = 0f;
        cc.enabled = false;
        transform.position = new Vector3(0f, 0.95f, 0f);
        cc.enabled = true;
    }

    void Update()
    {
        var gm = GameManager.I;
        if (gm == null || !gm.alive) return;
        float dt = Time.deltaTime;

        yaw += TouchInput.Look.x * 0.18f;
        pitch -= TouchInput.Look.y * 0.18f;
        pitch = Mathf.Clamp(pitch, -80f, 80f);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        Vector3 mv = transform.right * TouchInput.Move.x + transform.forward * TouchInput.Move.y;
        if (cc.isGrounded && vy < 0f) vy = -1f;
        vy -= 20f * dt;
        cc.Move((mv * speed + Vector3.up * vy) * dt);

        if (reloading)
        {
            reloadTimer -= dt;
            if (reloadTimer <= 0f) { reloading = false; ammo = MagSize; }
        }
        else if (ammo <= 0) StartReload();

        fireTimer -= dt;
        if (TouchInput.Fire && !reloading && fireTimer <= 0f && ammo > 0) Shoot();

        recoil = Mathf.MoveTowards(recoil, 0f, dt * 8f);
        gun.localPosition = gunBase + new Vector3(0f, 0.02f * recoil, -0.08f * recoil);
    }

    void StartReload()
    {
        reloading = true;
        reloadTimer = 1.4f;
    }

    void Shoot()
    {
        fireTimer = 0.11f;
        ammo--;
        recoil = 1f;

        Vector3 origin = cam.position;
        Vector3 dir = (cam.forward + cam.right * Random.Range(-0.012f, 0.012f) + cam.up * Random.Range(-0.012f, 0.012f)).normalized;
        Vector3 muzzle = gun.position + gun.forward * 0.35f;
        Vector3 end = origin + dir * 100f;

        RaycastHit hit;
        if (Physics.Raycast(origin, dir, out hit, 100f))
        {
            end = hit.point;
            var e = hit.collider.GetComponentInParent<Enemy>();
            if (e != null)
            {
                bool head = hit.collider.name == "Head";
                e.Hit(head ? 60f : 22f, head);
                GameManager.I.hitMarker = 0.15f;
            }
            else Fx.Spark(hit.point);
        }

        Fx.Tracer(muzzle, end, new Color(1f, 0.85f, 0.3f));
        Sfx.Play(Sfx.Shot, 0.5f);
    }

    public void Damage(float d)
    {
        var gm = GameManager.I;
        if (gm == null || !gm.alive) return;
        health -= d;
        gm.hurtFlash = 0.8f;
        Sfx.Play(Sfx.Hurt, 0.6f);
        if (health <= 0f)
        {
            health = 0f;
            gm.GameOver();
        }
    }

    public void Heal(float h)
    {
        health = Mathf.Min(maxHealth, health + h);
        ammo = MagSize;
        reloading = false;
    }
}

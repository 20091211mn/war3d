using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I;

    public PlayerController player;
    public bool alive = true;
    public float hurtFlash, hitMarker;
    public int score, wave, kills;

    const float Arena = 45f;

    readonly List<Enemy> enemies = new List<Enemy>();
    int toSpawn, best;
    float spawnTimer, nextWaveTimer, banner;
    Texture2D circle;
    GUIStyle sSmall, sMid, sBig, sBtn;

    void Awake()
    {
        I = this;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        QualitySettings.shadowDistance = 40f;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        Sfx.Init(gameObject);
        gameObject.AddComponent<TouchInput>();
        circle = MakeCircle();
        best = PlayerPrefs.GetInt("best", 0);

        BuildWorld();

        var go = new GameObject("Player");
        go.transform.position = new Vector3(0f, 0.95f, 0f);
        player = go.AddComponent<PlayerController>();

        StartWave(1);
    }

    // ---------------------------------------------------------------- world

    void BuildWorld()
    {
        RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.65f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.7f, 0.78f, 0.85f);
        RenderSettings.fogStartDistance = 30f;
        RenderSettings.fogEndDistance = 110f;

        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.95f, 0.85f);
        sun.intensity = 1.1f;
        sun.shadows = LightShadows.Hard;
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var ground = Fx.Prim(PrimitiveType.Plane, true);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(Arena * 2f / 10f, 1f, Arena * 2f / 10f);
        Fx.Tint(ground.GetComponent<Renderer>(), new Color(0.38f, 0.45f, 0.28f));

        Color wallCol = new Color(0.35f, 0.33f, 0.3f);
        Block(new Vector3(0f, 3f, Arena + 1f), new Vector3(Arena * 2f + 4f, 6f, 2f), wallCol);
        Block(new Vector3(0f, 3f, -Arena - 1f), new Vector3(Arena * 2f + 4f, 6f, 2f), wallCol);
        Block(new Vector3(Arena + 1f, 3f, 0f), new Vector3(2f, 6f, Arena * 2f + 4f), wallCol);
        Block(new Vector3(-Arena - 1f, 3f, 0f), new Vector3(2f, 6f, Arena * 2f + 4f), wallCol);

        var rng = new System.Random(7);
        Color[] palette =
        {
            new Color(0.45f, 0.38f, 0.3f), new Color(0.5f, 0.5f, 0.52f),
            new Color(0.32f, 0.36f, 0.3f), new Color(0.55f, 0.45f, 0.25f)
        };
        int placed = 0;
        while (placed < 30)
        {
            float x = (float)(rng.NextDouble() * 2.0 - 1.0) * (Arena - 6f);
            float z = (float)(rng.NextDouble() * 2.0 - 1.0) * (Arena - 6f);
            if (x * x + z * z < 10f * 10f) continue;
            float w = 2f + (float)rng.NextDouble() * 3f;
            float h = 1.2f + (float)rng.NextDouble() * 3f;
            float d = 2f + (float)rng.NextDouble() * 3f;
            Block(new Vector3(x, h * 0.5f, z), new Vector3(w, h, d), palette[rng.Next(palette.Length)]);
            placed++;
        }

        for (int i = 0; i < 10; i++)
        {
            float x = (float)(rng.NextDouble() * 2.0 - 1.0) * (Arena - 5f);
            float z = (float)(rng.NextDouble() * 2.0 - 1.0) * (Arena - 5f);
            if (x * x + z * z < 8f * 8f) continue;
            var b = Fx.Prim(PrimitiveType.Cylinder, true);
            b.transform.position = new Vector3(x, 0.6f, z);
            b.transform.localScale = new Vector3(0.9f, 0.6f, 0.9f);
            Fx.Tint(b.GetComponent<Renderer>(), new Color(0.6f, 0.15f, 0.1f));
        }
    }

    void Block(Vector3 pos, Vector3 size, Color c)
    {
        var b = Fx.Prim(PrimitiveType.Cube, true);
        b.transform.position = pos;
        b.transform.localScale = size;
        Fx.Tint(b.GetComponent<Renderer>(), c);
    }

    // ---------------------------------------------------------------- waves

    void StartWave(int w)
    {
        wave = w;
        toSpawn = 3 + w * 2;
        spawnTimer = 1f;
        nextWaveTimer = 0f;
        banner = 2.5f;
    }

    void SpawnEnemy()
    {
        float ang = Random.value * Mathf.PI * 2f;
        float dist = Random.Range(25f, 38f);
        Vector3 p = player.transform.position + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * dist;
        p.x = Mathf.Clamp(p.x, -Arena + 3f, Arena - 3f);
        p.z = Mathf.Clamp(p.z, -Arena + 3f, Arena - 3f);
        p.y = 0f;
        enemies.Add(Enemy.Create(p, wave));
    }

    public void OnKill(Enemy e, bool head)
    {
        enemies.Remove(e);
        kills++;
        score += head ? 150 : 100;
        if (Random.value < 0.25f) Pickup.Create(e.transform.position);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        hurtFlash = Mathf.MoveTowards(hurtFlash, 0f, dt * 1.5f);
        hitMarker = Mathf.MoveTowards(hitMarker, 0f, dt);
        if (banner > 0f) banner -= dt;
        if (!alive) return;

        if (toSpawn > 0)
        {
            spawnTimer -= dt;
            if (spawnTimer <= 0f && enemies.Count < 12)
            {
                SpawnEnemy();
                toSpawn--;
                spawnTimer = Mathf.Max(0.6f, 2.2f - wave * 0.12f);
            }
        }
        else if (enemies.Count == 0)
        {
            nextWaveTimer += dt;
            if (nextWaveTimer > 4f) StartWave(wave + 1);
        }
    }

    // ---------------------------------------------------------------- state

    public void GameOver()
    {
        alive = false;
        if (score > best)
        {
            best = score;
            PlayerPrefs.SetInt("best", best);
            PlayerPrefs.Save();
        }
        TouchInput.ResetAll();
    }

    void Restart()
    {
        foreach (var e in enemies) if (e != null) Destroy(e.gameObject);
        enemies.Clear();
        foreach (var p in FindObjectsOfType<Pickup>()) Destroy(p.gameObject);
        score = 0;
        kills = 0;
        hurtFlash = 0f;
        player.ResetPlayer();
        alive = true;
        TouchInput.ResetAll();
        StartWave(1);
    }

    // ---------------------------------------------------------------- HUD

    static Vector2 ToGui(Vector2 p) { return new Vector2(p.x, Screen.height - p.y); }

    static void Box(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void Circle(Vector2 center, float r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(new Rect(center.x - r, center.y - r, r * 2f, r * 2f), circle);
        GUI.color = Color.white;
    }

    static void Label(Rect r, string t, GUIStyle st, TextAnchor a)
    {
        st.alignment = a;
        GUI.Label(r, t, st);
    }

    static Texture2D MakeCircle()
    {
        int n = 64;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        var px = new Color[n * n];
        float r = n / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d));
            }
        t.SetPixels(px);
        t.filterMode = FilterMode.Bilinear;
        t.Apply();
        return t;
    }

    void EnsureStyles(float s)
    {
        if (sSmall == null)
        {
            sSmall = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            sMid = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            sBig = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            sBtn = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
            sSmall.normal.textColor = sMid.normal.textColor = sBig.normal.textColor = Color.white;
        }
        sSmall.fontSize = (int)(20f * s);
        sMid.fontSize = (int)(32f * s);
        sBig.fontSize = (int)(64f * s);
        sBtn.fontSize = (int)(30f * s);
    }

    void OnGUI()
    {
        if (player == null) return;
        float s = Screen.height / 540f;
        float w = Screen.width, h = Screen.height;
        EnsureStyles(s);

        if (hurtFlash > 0f) Box(new Rect(0, 0, w, h), new Color(1f, 0f, 0f, Mathf.Clamp01(hurtFlash) * 0.45f));

        // crosshair
        Color cc = hitMarker > 0f ? Color.red : Color.white;
        float gap = 8f * s, len = 14f * s, th = 2f * s;
        Box(new Rect(w / 2f - gap - len, h / 2f - th / 2f, len, th), cc);
        Box(new Rect(w / 2f + gap, h / 2f - th / 2f, len, th), cc);
        Box(new Rect(w / 2f - th / 2f, h / 2f - gap - len, th, len), cc);
        Box(new Rect(w / 2f - th / 2f, h / 2f + gap, th, len), cc);

        // health
        var bar = new Rect(24f * s, 20f * s, 260f * s, 24f * s);
        Box(bar, new Color(0f, 0f, 0f, 0.5f));
        float hp = Mathf.Clamp01(player.health / player.maxHealth);
        Box(new Rect(bar.x + 2f * s, bar.y + 2f * s, (bar.width - 4f * s) * hp, bar.height - 4f * s),
            Color.Lerp(Color.red, new Color(0.2f, 0.9f, 0.3f), hp));
        Label(new Rect(bar.x, bar.yMax + 2f * s, 260f * s, 30f * s), "HP " + Mathf.CeilToInt(player.health), sSmall, TextAnchor.UpperLeft);

        // wave / score
        Label(new Rect(w - 280f * s, 12f * s, 260f * s, 44f * s), "WAVE " + wave, sMid, TextAnchor.UpperRight);
        Label(new Rect(w - 280f * s, 52f * s, 260f * s, 30f * s), "SCORE " + score, sSmall, TextAnchor.UpperRight);
        Label(new Rect(w - 280f * s, 80f * s, 260f * s, 30f * s), "BEST " + best, sSmall, TextAnchor.UpperRight);

        // touch controls
        float R = TouchInput.R;
        Vector2 so = TouchInput.StickActive ? TouchInput.StickOrigin : TouchInput.DefaultStick;
        Vector2 sp = TouchInput.StickActive ? TouchInput.StickPos : so;
        Circle(ToGui(so), R * 1.2f, new Color(1f, 1f, 1f, 0.15f));
        Vector2 k = Vector2.ClampMagnitude(sp - so, R * 1.2f);
        Circle(ToGui(so + k), R * 0.5f, new Color(1f, 1f, 1f, 0.4f));

        Vector2 fc = ToGui(TouchInput.FireCenter);
        Circle(fc, R, TouchInput.Fire ? new Color(1f, 0.3f, 0.2f, 0.7f) : new Color(1f, 0.3f, 0.2f, 0.35f));
        Label(new Rect(fc.x - R, fc.y - 20f * s, R * 2f, 40f * s), "FIRE", sSmall, TextAnchor.MiddleCenter);
        string ammoTxt = player.reloading ? "RELOADING" : player.ammo + " / " + PlayerController.MagSize;
        Label(new Rect(fc.x - R * 5f, fc.y - 20f * s, R * 3.6f, 40f * s), ammoTxt, sMid, TextAnchor.MiddleRight);

        // wave banner
        if (banner > 0f && alive)
            Label(new Rect(0, h * 0.2f, w, 90f * s), "WAVE " + wave, sBig, TextAnchor.MiddleCenter);

        // game over
        if (!alive)
        {
            Box(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.65f));
            Label(new Rect(0, h * 0.18f, w, 90f * s), "GAME OVER", sBig, TextAnchor.MiddleCenter);
            Label(new Rect(0, h * 0.18f + 95f * s, w, 44f * s), "SCORE " + score + "   WAVE " + wave, sMid, TextAnchor.MiddleCenter);
            Label(new Rect(0, h * 0.18f + 140f * s, w, 40f * s), "BEST " + best, sSmall, TextAnchor.MiddleCenter);
            if (GUI.Button(new Rect(w / 2f - 160f * s, h * 0.62f, 320f * s, 76f * s), "PLAY AGAIN", sBtn)) Restart();
        }
    }
}

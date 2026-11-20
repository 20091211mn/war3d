using UnityEngine;
using UnityEngine.Rendering;

// Small helpers: primitives without colliders/shadows, tint without creating material instances,
// tracers and sparks.
public static class Fx
{
    static MaterialPropertyBlock mpb;

    public static GameObject Prim(PrimitiveType type, bool keepCollider)
    {
        var go = GameObject.CreatePrimitive(type);
        if (!keepCollider)
        {
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var r = go.GetComponent<Renderer>();
            if (r != null) r.shadowCastingMode = ShadowCastingMode.Off;
        }
        return go;
    }

    public static void Tint(Renderer r, Color c)
    {
        if (mpb == null) mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        mpb.SetColor("_Color", c);
        r.SetPropertyBlock(mpb);
    }

    public static void Tracer(Vector3 a, Vector3 b, Color c)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 0.1f) return;
        var t = Prim(PrimitiveType.Cube, false);
        t.transform.position = (a + b) * 0.5f;
        t.transform.rotation = Quaternion.LookRotation(d);
        t.transform.localScale = new Vector3(0.04f, 0.04f, len);
        Tint(t.GetComponent<Renderer>(), c);
        Object.Destroy(t, 0.05f);
    }

    public static void Spark(Vector3 p)
    {
        var s = Prim(PrimitiveType.Sphere, false);
        s.transform.position = p;
        s.transform.localScale = Vector3.one * 0.25f;
        Tint(s.GetComponent<Renderer>(), new Color(1f, 0.85f, 0.3f));
        Object.Destroy(s, 0.07f);
    }
}

// Procedural sounds so the project needs no audio files.
public static class Sfx
{
    public static AudioClip Shot, Boom, Hurt;
    static AudioSource src;

    public static void Init(GameObject host)
    {
        src = host.AddComponent<AudioSource>();
        src.spatialBlend = 0f;
        Shot = Make(0.12f, 700f, 0.7f);
        Boom = Make(0.35f, 120f, 0.5f);
        Hurt = Make(0.18f, 300f, 0.3f);
    }

    static AudioClip Make(float len, float freq, float noiseMix)
    {
        int rate = 22050;
        int n = (int)(len * rate);
        var d = new float[n];
        var rnd = new System.Random(1);
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float env = (1f - t) * (1f - t);
            float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * i / rate * (1f - t * 0.5f));
            d[i] = (noise * noiseMix + tone * (1f - noiseMix)) * env * 0.8f;
        }
        var c = AudioClip.Create("sfx", n, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    public static void Play(AudioClip c, float vol)
    {
        if (src != null && c != null) src.PlayOneShot(c, vol);
    }

    public static void PlayAt(AudioClip c, Vector3 p, float vol)
    {
        if (c != null) AudioSource.PlayClipAtPoint(c, p, vol);
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GokCephesi
{
    /// <summary>
    /// Tüm görseller hazır model gerektirmeden Unity'nin basit şekillerinden (küp, küre, silindir...) kurulur.
    /// Malzeme, projenin render hattının (Built-in veya URP) varsayılan malzemesinden kopyalanır; böylece pembe görünmez.
    /// </summary>
    public static class Gfx
    {
        static Material baseMat;
        static readonly Dictionary<long, Material> cache = new Dictionary<long, Material>();
        static Texture2D circleTex, ringTex;

        static Material Base()
        {
            if (baseMat == null)
            {
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseMat = tmp.GetComponent<Renderer>().sharedMaterial;
                Object.Destroy(tmp);
            }
            return baseMat;
        }

        public static Material Mat(Color c, float glow = 0f)
        {
            long key = ((long)Mathf.RoundToInt(c.r * 255f) << 32) | ((long)Mathf.RoundToInt(c.g * 255f) << 24) |
                       ((long)Mathf.RoundToInt(c.b * 255f) << 16) | (long)Mathf.RoundToInt(glow * 20f);
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;

            m = new Material(Base());
            m.enableInstancing = true;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.3f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.3f);
            if (glow > 0f && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * glow);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            cache[key] = m;
            return m;
        }

        public static GameObject Prim(PrimitiveType t, Transform parent, Vector3 localPos, Vector3 scale, Color c,
                                      float glow = 0f, bool shadows = true)
        {
            var g = GameObject.CreatePrimitive(t);
            var col = g.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            if (parent != null) g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localScale = scale;
            var r = g.GetComponent<Renderer>();
            r.sharedMaterial = Mat(c, glow);
            if (!shadows || glow > 0f) r.shadowCastingMode = ShadowCastingMode.Off;
            return g;
        }

        public static GameObject Prim(PrimitiveType t, Transform parent, Vector3 localPos, Vector3 scale, Vector3 euler,
                                      Color c, float glow = 0f, bool shadows = true)
        {
            var g = Prim(t, parent, localPos, scale, c, glow, shadows);
            g.transform.localRotation = Quaternion.Euler(euler);
            return g;
        }

        public static void Recolor(GameObject g, Color c, float glow = 0f)
        {
            var r = g.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = Mat(c, glow);
        }

        // Arayüz için yuvarlak dokular
        public static Texture2D Circle
        {
            get
            {
                if (circleTex == null) circleTex = MakeCircle(false);
                return circleTex;
            }
        }

        public static Texture2D Ring
        {
            get
            {
                if (ringTex == null) ringTex = MakeCircle(true);
                return ringTex;
            }
        }

        static Texture2D MakeCircle(bool ring)
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[n * n];
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = Mathf.Clamp01((1f - d) * 40f);
                    if (ring) a *= Mathf.Clamp01((d - 0.86f) * 40f);
                    px[y * n + x] = new Color(1, 1, 1, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        public static Color Lerp(Color a, Color b, float t) { return Color.Lerp(a, b, t); }
    }

    // ------------------------------------------------------------------
    // PATLAMA / KIVILCIM EFEKTLERİ
    // ------------------------------------------------------------------
    public class Fx : MonoBehaviour
    {
        float t, life, size;
        Vector3 vel;
        bool shrinkOnly;

        public static void Explode(Vector3 pos, float size, Color c)
        {
            var core = Gfx.Prim(PrimitiveType.Sphere, null, pos, Vector3.zero, new Color(1f, 0.85f, 0.4f), 3f, false);
            var f = core.AddComponent<Fx>();
            f.life = 0.55f; f.size = size;

            int pieces = size > 10f ? 10 : 6;
            for (int i = 0; i < pieces; i++)
            {
                var d = Gfx.Prim(PrimitiveType.Cube, null, pos, Vector3.one * size * 0.15f, Random.rotation.eulerAngles, c, 1.5f, false);
                var fd = d.AddComponent<Fx>();
                fd.life = 0.9f; fd.size = size * 0.15f; fd.shrinkOnly = true;
                fd.vel = Random.onUnitSphere * size * 4f;
            }
            Sfx.Play(size > 10f ? Sfx.BigBoom : Sfx.Boom, pos);
        }

        public static void Spark(Vector3 pos, Color c)
        {
            var s = Gfx.Prim(PrimitiveType.Sphere, null, pos, Vector3.zero, c, 3f, false);
            var f = s.AddComponent<Fx>();
            f.life = 0.18f; f.size = 2.2f;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = t / life;
            if (k >= 1f) { Destroy(gameObject); return; }
            if (shrinkOnly)
            {
                transform.position += vel * Time.deltaTime;
                vel *= 0.96f;
                transform.localScale = Vector3.one * size * (1f - k);
                transform.Rotate(300f * Time.deltaTime, 200f * Time.deltaTime, 0);
            }
            else
            {
                float s = k < 0.3f ? Mathf.Lerp(0f, size, k / 0.3f) : Mathf.Lerp(size, 0f, (k - 0.3f) / 0.7f);
                transform.localScale = Vector3.one * s;
            }
        }
    }

    // ------------------------------------------------------------------
    // SES EFEKTLERİ (kodla üretilir, ses dosyası gerekmez)
    // ------------------------------------------------------------------
    public static class Sfx
    {
        public static AudioClip Laser, Missile, Boom, BigBoom, LevelUp, Warp;
        static AudioSource src;
        public static bool Enabled = true;

        public static void Init(GameObject host)
        {
            src = host.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            Laser = Make("laser", 0.09f, (t, p) => Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(1400f, 500f, p) * t) * (1f - p) * 0.35f);
            Missile = Make("missile", 0.35f, (t, p) => (Random.value * 2f - 1f) * (1f - p) * 0.3f + Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.15f * (1f - p));
            Boom = Make("boom", 0.45f, (t, p) => (Random.value * 2f - 1f) * Mathf.Pow(1f - p, 2f) * 0.6f);
            BigBoom = Make("bigboom", 1.1f, (t, p) => (Random.value * 2f - 1f) * Mathf.Pow(1f - p, 1.5f) * 0.8f + Mathf.Sin(2f * Mathf.PI * 50f * t) * 0.3f * (1f - p));
            LevelUp = Make("levelup", 0.6f, (t, p) => Mathf.Sin(2f * Mathf.PI * (p < 0.33f ? 523f : p < 0.66f ? 659f : 784f) * t) * 0.3f);
            Warp = Make("warp", 0.7f, (t, p) => Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(200f, 1200f, p) * t) * Mathf.Sin(Mathf.PI * p) * 0.3f);
        }

        delegate float Wave(float t, float progress);

        static AudioClip Make(string name, float seconds, Wave w)
        {
            const int rate = 22050;
            int n = Mathf.CeilToInt(seconds * rate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                data[i] = w(t, i / (float)n);
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static void Play(AudioClip c, Vector3 pos, float vol = 1f)
        {
            if (!Enabled || src == null || c == null) return;
            var cam = Camera.main;
            if (cam != null)
            {
                float d = Vector3.Distance(cam.transform.position, pos);
                if (d > 400f) return;
                vol *= Mathf.Clamp01(1f - d / 400f);
            }
            src.PlayOneShot(c, vol);
        }

        public static void Play2D(AudioClip c, float vol = 1f)
        {
            if (!Enabled || src == null || c == null) return;
            src.PlayOneShot(c, vol);
        }
    }
}

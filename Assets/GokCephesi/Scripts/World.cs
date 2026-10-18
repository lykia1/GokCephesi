using System.Collections.Generic;
using UnityEngine;

namespace GokCephesi
{
    public struct Obstacle
    {
        public Vector3 pos;
        public float radius;
        public float height;
    }

    public struct WorldLabel
    {
        public Vector3 pos;
        public string text;
        public Color color;
        public float maxDist;
    }

    /// <summary>
    /// Bir haritanın tamamını kodla kurar: zemin, dağlar, binalar, kapılar, üsler, kaleler, yaratıklar ve botlar.
    /// Harita değişince bu nesne silinir ve yenisi kurulur.
    /// </summary>
    public class World : MonoBehaviour
    {
        public static World I;

        public MapDef def;
        public int playerNation;
        public const float MaxAlt = 380f;

        public readonly List<Obstacle> obstacles = new List<Obstacle>();
        public readonly List<Gate> gates = new List<Gate>();
        public readonly List<Outpost> outposts = new List<Outpost>();
        public readonly Fortress[] fortresses = new Fortress[2];
        public readonly List<BotPilot> bots = new List<BotPilot>();
        public readonly List<WorldLabel> labels = new List<WorldLabel>();

        System.Random rng;
        float monsterTimer;
        Transform deco;

        public bool SafeZone { get { return def.kind == MapKind.Home; } }
        public bool IsWar { get { return def.kind == MapKind.War; } }

        // ------------------------------------------------------------------
        public static World Build(MapDef def, int playerNation)
        {
            var go = new GameObject("Harita_" + def.name);
            var w = go.AddComponent<World>();
            I = w;
            w.def = def;
            w.playerNation = playerNation;
            w.rng = new System.Random(def.seed);
            w.deco = new GameObject("Dekor").transform;
            w.deco.SetParent(go.transform, false);

            w.SetupAtmosphere();
            w.BuildGround();
            switch (def.theme)
            {
                case 0: w.BuildCity(); break;
                case 1: w.BuildValley(); break;
                case 2: w.BuildCanyon(); break;
                case 3: w.BuildFront(); break;
            }
            w.BuildBoundary();
            w.BuildGates();
            w.SpawnInitialUnits();
            // Hareketsiz dekoru birleştir: telefonlarda çok daha akıcı çalışır
            StaticBatchingUtility.Combine(w.deco.gameObject);
            return w;
        }

        void OnDestroy()
        {
            if (I == this) I = null;
        }

        float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }

        /// <summary>Savaş haritasında 2. ulus için harita aynalanır (kendi tarafın hep "güney"de olur).</summary>
        public Vector3 M(Vector3 v)
        {
            if (IsWar && playerNation == 1) return new Vector3(v.x, v.y, -v.z);
            return v;
        }

        public Vector3 PlayerSpawn { get { return M(def.spawn); } }

        public Vector3 SpawnFor(int nation)
        {
            Vector3 jitter = new Vector3(Random.Range(-60f, 60f), Random.Range(-10f, 20f), Random.Range(-30f, 30f));
            if (IsWar)
            {
                float z = nation == 0 ? -Mathf.Abs(def.spawn.z) : Mathf.Abs(def.spawn.z);
                return new Vector3(0, def.spawn.y, z) + jitter;
            }
            if (nation == playerNation || nation < 0) return def.spawn + jitter;
            return new Vector3(-def.spawn.x, def.spawn.y, -def.spawn.z) + jitter;
        }

        public static float YawTowards(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        }

        // ------------------------------------------------------------------
        // HAREKET SINIRLARI
        // ------------------------------------------------------------------
        public void Constrain(ref Vector3 p, float r)
        {
            float minY = 5f + r * 0.4f;
            if (p.y < minY) p.y = minY;
            if (p.y > MaxAlt) p.y = MaxAlt;

            float R = def.radius;
            float h2 = p.x * p.x + p.z * p.z;
            if (h2 > R * R)
            {
                float h = Mathf.Sqrt(h2);
                p.x = p.x / h * R;
                p.z = p.z / h * R;
            }

            for (int i = 0; i < obstacles.Count; i++)
            {
                var o = obstacles[i];
                if (p.y > o.height + r) continue;
                float dx = p.x - o.pos.x, dz = p.z - o.pos.z;
                float min = o.radius + r;
                float d2 = dx * dx + dz * dz;
                if (d2 >= min * min) continue;
                float d = Mathf.Sqrt(d2);
                if (d < 0.01f) { dx = 1f; dz = 0f; d = 1f; }
                // Engelin üstüne yakınsa yukarı it, değilse yana it
                if (o.height + r - p.y < min - d) p.y = o.height + r;
                else { p.x = o.pos.x + dx / d * min; p.z = o.pos.z + dz / d * min; }
            }
        }

        public bool OutOfBoundsWarning(Vector3 p)
        {
            return p.x * p.x + p.z * p.z > def.radius * def.radius * 0.97f * 0.97f;
        }

        // ------------------------------------------------------------------
        // ORTAM
        // ------------------------------------------------------------------
        void SetupAtmosphere()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = def.sky;
                cam.farClipPlane = 3000f;
                cam.nearClipPlane = 0.5f;
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = def.fog;
            RenderSettings.fogDensity = def.fogDensity;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(def.sky, Color.white, 0.3f) * 0.75f;

            Light sun = null;
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
                if (lights[i].type == LightType.Directional) { sun = lights[i]; break; }
            if (sun == null)
            {
                var lg = new GameObject("Gunes");
                sun = lg.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.shadows = LightShadows.Soft;
            sun.intensity = def.theme == 2 ? 0.8f : 1.15f;
            sun.color = def.theme == 1 ? new Color(1f, 0.92f, 0.8f) : (def.theme == 2 ? new Color(0.85f, 0.75f, 1f) : Color.white);
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        }

        void BuildGround()
        {
            float size = def.radius * 2.8f;
            Gfx.Prim(PrimitiveType.Cube, deco, new Vector3(0, -0.5f, 0), new Vector3(size, 1f, size), def.ground);
            for (int i = 0; i < 40; i++)
            {
                float a = R(0, Mathf.PI * 2f), d = R(0, def.radius * 1.2f);
                float s = R(60f, 220f);
                Gfx.Prim(PrimitiveType.Cylinder, deco, new Vector3(Mathf.Cos(a) * d, 0.02f, Mathf.Sin(a) * d),
                    new Vector3(s, 0.02f, s * R(0.6f, 1.4f)), def.ground2, 0f, false);
            }
        }

        void BuildBoundary()
        {
            int n = 40;
            Color m = Color.Lerp(def.ground, Color.black, 0.25f);
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n + R(-0.05f, 0.05f);
                float d = def.radius * R(1.1f, 1.25f);
                Vector3 p = new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d);
                if (def.theme == 0)
                {
                    float h = R(120f, 260f);
                    Gfx.Prim(PrimitiveType.Cube, deco, p + Vector3.up * h * 0.5f, new Vector3(R(60, 110), h, R(60, 110)),
                        new Vector3(0, R(0, 90), 0), Color.Lerp(Nations.Dark[playerNation], Color.gray, 0.5f));
                }
                else
                {
                    float w = R(140f, 260f), h = R(120f, 300f);
                    Gfx.Prim(PrimitiveType.Sphere, deco, p, new Vector3(w, h, w * R(0.8f, 1.3f)), m);
                }
            }
        }

        void AddObstacle(Vector3 pos, float radius, float height)
        {
            obstacles.Add(new Obstacle { pos = pos, radius = radius, height = height });
        }

        bool NearKeyPoint(Vector3 p, float dist)
        {
            if ((new Vector3(p.x, 0, p.z) - new Vector3(def.spawn.x, 0, def.spawn.z)).magnitude < dist) return true;
            if ((new Vector3(p.x, 0, p.z) - new Vector3(-def.spawn.x, 0, -def.spawn.z)).magnitude < dist && IsWar) return true;
            for (int i = 0; i < def.gates.Count; i++)
            {
                var g = def.gates[i].pos;
                if ((new Vector3(p.x, 0, p.z) - new Vector3(g.x, 0, g.z)).magnitude < dist) return true;
                if (IsWar && (new Vector3(p.x, 0, p.z) - new Vector3(g.x, 0, -g.z)).magnitude < dist) return true;
            }
            return false;
        }

        Vector3 RandomFreePoint(float minFromKey, float maxR)
        {
            for (int tries = 0; tries < 30; tries++)
            {
                float a = R(0, Mathf.PI * 2f), d = R(60f, maxR);
                var p = new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d);
                if (!NearKeyPoint(p, minFromKey)) return p;
            }
            return new Vector3(R(-maxR, maxR) * 0.5f, 0, R(-maxR, maxR) * 0.5f);
        }

        // ------------------------------------------------------------------
        // TEMALAR
        // ------------------------------------------------------------------
        void BuildCity()
        {
            Color main = Nations.Main[playerNation];
            Color dark = Nations.Dark[playerNation];
            Color concrete = new Color(0.62f, 0.64f, 0.68f);

            // Merkez kule
            Vector3 c = new Vector3(0, 0, 120);
            Gfx.Prim(PrimitiveType.Cylinder, deco, c + Vector3.up * 80f, new Vector3(50f, 80f, 50f), concrete);
            Gfx.Prim(PrimitiveType.Cylinder, deco, c + Vector3.up * 165f, new Vector3(70f, 5f, 70f), main);
            Gfx.Prim(PrimitiveType.Sphere, deco, c + Vector3.up * 185f, Vector3.one * 26f, main, 2.5f);
            AddObstacle(c, 36f, 195f);
            labels.Add(new WorldLabel { pos = c + Vector3.up * 215f, text = Nations.Names[playerNation] + " Komuta Kulesi", color = main, maxDist = 900f });

            // Binalar
            for (int gx = -4; gx <= 4; gx++)
            {
                for (int gz = -4; gz <= 4; gz++)
                {
                    Vector3 p = new Vector3(gx * 115f + R(-20, 20), 0, gz * 115f + R(-20, 20));
                    if (p.magnitude < 170f) continue;
                    if (Mathf.Abs(p.x) < 80f) continue;                 // ana cadde
                    if (p.magnitude > def.radius - 60f) continue;
                    float h = R(30f, 150f), wx = R(28f, 50f), wz = R(28f, 50f);
                    Color bc = Color.Lerp(concrete, dark, R(0.1f, 0.5f));
                    Gfx.Prim(PrimitiveType.Cube, deco, p + Vector3.up * h * 0.5f, new Vector3(wx, h, wz), bc);
                    // ışıklı pencere şeritleri
                    int strips = Mathf.Max(1, (int)(h / 30f));
                    for (int s = 0; s < strips; s++)
                    {
                        float y = 12f + s * 28f;
                        if (y > h - 6f) break;
                        Gfx.Prim(PrimitiveType.Cube, deco, p + Vector3.up * y, new Vector3(wx + 0.6f, 2.2f, wz + 0.6f), Color.Lerp(main, Color.white, 0.5f), 1.5f, false);
                    }
                    AddObstacle(p, Mathf.Max(wx, wz) * 0.62f, h);
                }
            }

            // Hangar ve iniş pistleri
            Vector3 hp = new Vector3(0, 0, -260);
            Gfx.Prim(PrimitiveType.Sphere, deco, hp, new Vector3(120f, 60f, 80f), Color.Lerp(concrete, main, 0.25f));
            Gfx.Prim(PrimitiveType.Cube, deco, hp + new Vector3(0, 8f, 40f), new Vector3(70f, 16f, 4f), dark);
            AddObstacle(hp, 55f, 30f);
            labels.Add(new WorldLabel { pos = hp + Vector3.up * 50f, text = "HANGAR", color = Color.white, maxDist = 700f });

            for (int i = -1; i <= 1; i++)
            {
                Vector3 pad = new Vector3(i * 70f, 0.1f, -60f);
                Gfx.Prim(PrimitiveType.Cylinder, deco, pad, new Vector3(40f, 0.15f, 40f), new Color(0.2f, 0.22f, 0.25f), 0f, false);
                Gfx.Prim(PrimitiveType.Cylinder, deco, pad + Vector3.up * 0.1f, new Vector3(30f, 0.15f, 30f), main, 1.2f, false);
                Gfx.Prim(PrimitiveType.Cylinder, deco, pad + Vector3.up * 0.2f, new Vector3(26f, 0.15f, 26f), new Color(0.2f, 0.22f, 0.25f), 0f, false);
            }

            // Ana cadde ışıkları
            for (int z = -500; z <= 500; z += 50)
            {
                Gfx.Prim(PrimitiveType.Cube, deco, new Vector3(-40, 0.2f, z), new Vector3(3, 0.3f, 18), main, 2f, false);
                Gfx.Prim(PrimitiveType.Cube, deco, new Vector3(40, 0.2f, z), new Vector3(3, 0.3f, 18), main, 2f, false);
            }

            // Bayrak direkleri
            for (int i = -1; i <= 1; i += 2)
            {
                Vector3 fp = new Vector3(i * 60f, 0, 300f);
                Gfx.Prim(PrimitiveType.Cylinder, deco, fp + Vector3.up * 30f, new Vector3(2f, 30f, 2f), concrete);
                Gfx.Prim(PrimitiveType.Cube, deco, fp + new Vector3(i * 10f, 52f, 0), new Vector3(20f, 12f, 0.5f), main, 0.6f);
            }
        }

        void BuildValley()
        {
            Color rock = new Color(0.62f, 0.44f, 0.28f);
            Color rockTop = new Color(0.80f, 0.64f, 0.42f);

            for (int i = 0; i < 16; i++)
            {
                var p = RandomFreePoint(160f, def.radius * 0.9f);
                float r = R(28f, 75f), h = R(40f, 130f);
                Gfx.Prim(PrimitiveType.Cylinder, deco, p + Vector3.up * h * 0.5f, new Vector3(r * 2f, h * 0.5f, r * 2f * R(0.8f, 1.2f)), rock);
                Gfx.Prim(PrimitiveType.Cylinder, deco, p + Vector3.up * h, new Vector3(r * 2.1f, 1.5f, r * 2.1f), rockTop, 0f, false);
                AddObstacle(p, r * 1.05f, h + 1f);
            }
            for (int i = 0; i < 40; i++)
            {
                var p = RandomFreePoint(80f, def.radius);
                float s = R(6f, 22f);
                Gfx.Prim(PrimitiveType.Sphere, deco, p, new Vector3(s, s * R(0.5f, 0.9f), s * R(0.8f, 1.3f)), Color.Lerp(rock, rockTop, R(0, 1)));
            }
            Color cactus = new Color(0.30f, 0.50f, 0.22f);
            for (int i = 0; i < 25; i++)
            {
                var p = RandomFreePoint(80f, def.radius);
                float h = R(8f, 18f);
                Gfx.Prim(PrimitiveType.Cylinder, deco, p + Vector3.up * h * 0.5f, new Vector3(2.2f, h * 0.5f, 2.2f), cactus);
                Gfx.Prim(PrimitiveType.Cylinder, deco, p + new Vector3(2.2f, h * 0.6f, 0), new Vector3(1.4f, h * 0.18f, 1.4f), cactus);
            }
            // Antik kalıntılar
            Vector3 ruin = RandomFreePoint(200f, def.radius * 0.5f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                Vector3 p = ruin + new Vector3(Mathf.Cos(a) * 45f, 0, Mathf.Sin(a) * 45f);
                float h = R(15f, 45f);
                Gfx.Prim(PrimitiveType.Cylinder, deco, p + Vector3.up * h * 0.5f, new Vector3(6f, h * 0.5f, 6f), new Color(0.86f, 0.80f, 0.68f));
            }
            labels.Add(new WorldLabel { pos = ruin + Vector3.up * 60f, text = "Antik Kalıntılar", color = new Color(1f, 0.9f, 0.7f), maxDist = 500f });
        }

        void BuildCanyon()
        {
            Color wall = new Color(0.30f, 0.22f, 0.38f);
            // Kanyon duvarları
            for (int i = 0; i < 9; i++)
            {
                var p = RandomFreePoint(220f, def.radius * 0.85f);
                float len = R(180f, 340f), h = R(90f, 170f), th = R(30f, 50f);
                float ang = R(0f, 180f);
                Gfx.Prim(PrimitiveType.Cube, deco, p + Vector3.up * h * 0.5f, new Vector3(th, h, len), new Vector3(0, ang, 0), wall);
                Vector3 dir = Quaternion.Euler(0, ang, 0) * Vector3.forward;
                int seg = Mathf.CeilToInt(len / (th * 0.9f));
                for (int s = 0; s <= seg; s++)
                {
                    Vector3 q = p + dir * (-len * 0.5f + s * len / seg);
                    AddObstacle(q, th * 0.6f, h);
                }
            }
            // Kristal kuleler
            for (int i = 0; i < 34; i++)
            {
                var p = RandomFreePoint(120f, def.radius * 0.95f);
                float h = R(25f, 90f), w = R(6f, 16f);
                Color cc = rng.NextDouble() < 0.5 ? new Color(0.9f, 0.35f, 1f) : new Color(0.3f, 0.9f, 1f);
                Gfx.Prim(PrimitiveType.Cube, deco, p + Vector3.up * h * 0.45f, new Vector3(w, h, w), new Vector3(R(-12, 12), R(0, 90), R(-12, 12)), cc, 1.4f);
                AddObstacle(p, w * 0.8f, h * 0.9f);
            }
            // Yüzen kayalar (dekor)
            for (int i = 0; i < 18; i++)
            {
                var p = RandomFreePoint(100f, def.radius);
                float s = R(15f, 45f);
                Gfx.Prim(PrimitiveType.Sphere, deco, p + Vector3.up * R(220f, 340f), new Vector3(s, s * 0.6f, s), wall);
            }
        }

        void BuildFront()
        {
            Color crater = new Color(0.22f, 0.24f, 0.20f);
            for (int i = 0; i < 30; i++)
            {
                var p = RandomFreePoint(150f, def.radius * 0.95f);
                if (Mathf.Abs(p.z) > 700f) continue;
                float s = R(20f, 60f);
                Gfx.Prim(PrimitiveType.Cylinder, deco, p + Vector3.up * 0.06f, new Vector3(s, 0.04f, s), crater, 0f, false);
            }
            // Enkazlar
            Color wreck = new Color(0.35f, 0.33f, 0.30f);
            for (int i = 0; i < 18; i++)
            {
                var p = RandomFreePoint(150f, def.radius * 0.9f);
                if (Mathf.Abs(p.z) > 650f) continue;
                float s = R(10f, 25f);
                Gfx.Prim(PrimitiveType.Cube, deco, p + Vector3.up * s * 0.2f, new Vector3(s, s * 0.4f, s * 2.2f), new Vector3(R(-20, 20), R(0, 180), R(-25, 25)), wreck);
                Gfx.Prim(PrimitiveType.Cube, deco, p + Vector3.up * s * 0.4f, new Vector3(s * 2f, s * 0.15f, s * 0.6f), new Vector3(R(-20, 20), R(0, 180), R(-20, 20)), wreck);
            }
            // Orta hattaki tepeler
            for (int i = 0; i < 8; i++)
            {
                float x = -800f + i * 230f + R(-40, 40);
                if (Mathf.Abs(x - 0f) < 140f || Mathf.Abs(x - 450f) < 140f || Mathf.Abs(x + 450f) < 140f) continue;
                Vector3 p = new Vector3(x, 0, R(-60f, 60f));
                float r = R(40f, 70f), h = R(50f, 110f);
                Gfx.Prim(PrimitiveType.Sphere, deco, p, new Vector3(r * 2f, h * 2f, r * 2f), new Color(0.32f, 0.38f, 0.28f));
                AddObstacle(p, r * 0.9f, h * 0.9f);
            }

            // Kaleler
            for (int n = 0; n < 2; n++)
            {
                Vector3 fp = new Vector3(0, 0, n == 0 ? -870f : 870f);
                var fgo = new GameObject("Kale_" + Nations.Short[n]);
                fgo.transform.SetParent(transform, false);
                fgo.transform.position = fp;
                var f = fgo.AddComponent<Fortress>();
                f.Build(n);
                fortresses[n] = f;
                AddObstacle(fp, 62f, 70f);
            }

            // Stratejik üsler
            string[] names = { "Alfa Üssü", "Merkez Üssü", "Delta Üssü" };
            float[] xs = { -450f, 0f, 450f };
            for (int i = 0; i < 3; i++)
            {
                var ogo = new GameObject("Us_" + i);
                ogo.transform.SetParent(transform, false);
                ogo.transform.position = new Vector3(xs[i], 0, 0);
                var o = ogo.AddComponent<Outpost>();
                o.Build(names[i]);
                outposts.Add(o);
                AddObstacle(ogo.transform.position, 12f, 48f);
            }
        }

        void BuildGates()
        {
            for (int i = 0; i < def.gates.Count; i++)
            {
                var gd = def.gates[i];
                var target = Maps.All[gd.target];
                var ggo = new GameObject("Kapi_" + target.name);
                ggo.transform.SetParent(transform, false);
                Vector3 p = M(gd.pos);
                ggo.transform.position = p;
                Vector3 toCenter = new Vector3(-p.x, 0, -p.z);
                if (toCenter.sqrMagnitude < 1f) toCenter = Vector3.forward;
                ggo.transform.rotation = Quaternion.LookRotation(toCenter.normalized);
                var g = ggo.AddComponent<Gate>();
                g.Build(gd.target, target.name, target.minLevel);
                gates.Add(g);
            }
        }

        public Gate GateTo(int mapId)
        {
            for (int i = 0; i < gates.Count; i++) if (gates[i].target == mapId) return gates[i];
            return null;
        }

        // ------------------------------------------------------------------
        // BİRİMLER
        // ------------------------------------------------------------------
        void SpawnInitialUnits()
        {
            int pl = GameManager.I != null ? GameManager.I.save.level : 1;
            for (int i = 0; i < def.allyBots; i++) SpawnBot(playerNation, pl);
            for (int i = 0; i < def.enemyBots; i++) SpawnBot(Nations.Enemy(playerNation), pl);
            for (int i = 0; i < def.monsterCount; i++) SpawnMonster();
        }

        public BotPilot SpawnBot(int nation, int playerLevel)
        {
            int lv = Mathf.Clamp(playerLevel + Random.Range(-1, 2), 1, Progression.MaxLevel);
            int cls = Random.Range(0, ShipClasses.All.Length);
            int up = lv / 4;
            var go = new GameObject("Bot_" + Nations.Short[nation]);
            go.transform.SetParent(transform, false);
            var ship = go.AddComponent<Ship>();
            ship.Setup(nation, cls, lv, ShipStats.Compute(cls, lv, up, up, up, up), Nations.RandomCallsign(nation), false);
            ship.aimSpread = Random.Range(2f, 4.5f);
            Vector3 sp = SpawnFor(nation);
            ship.Teleport(sp, YawTowards(sp, Vector3.zero));
            var bot = go.AddComponent<BotPilot>();
            bot.Init(ship, sp, false);
            bots.Add(bot);
            return bot;
        }

        void SpawnMonster()
        {
            Vector3 p = RandomFreePoint(220f, def.radius * 0.85f);
            p.y = R(25f, 90f);
            int lv = Random.Range(def.minMonsterLv, def.maxMonsterLv + 1);
            var go = new GameObject("Yaratik");
            go.transform.SetParent(transform, false);
            var ship = go.AddComponent<Ship>();
            ship.SetupMonster(lv);
            ship.aimSpread = 5f;
            ship.Teleport(p, R(0, 360));
            var bot = go.AddComponent<BotPilot>();
            bot.Init(ship, p, true);
            bots.Add(bot);
        }

        public int AliveMonsters()
        {
            int n = 0;
            for (int i = 0; i < bots.Count; i++)
                if (bots[i] != null && bots[i].IsMonster) n++;
            return n;
        }

        void Update()
        {
            bots.RemoveAll(b => b == null);
            if (def.monsterCount > 0)
            {
                monsterTimer -= Time.deltaTime;
                if (monsterTimer <= 0f)
                {
                    monsterTimer = 3f;
                    if (AliveMonsters() < def.monsterCount) SpawnMonster();
                }
            }
        }

        // ------------------------------------------------------------------
        // ULUS SAVAŞI
        // ------------------------------------------------------------------
        public bool AllOutpostsOwnedBy(int nation)
        {
            if (outposts.Count == 0) return false;
            for (int i = 0; i < outposts.Count; i++)
                if (outposts[i].owner != nation) return false;
            return true;
        }

        public int OutpostsOwnedBy(int nation)
        {
            int n = 0;
            for (int i = 0; i < outposts.Count; i++) if (outposts[i].owner == nation) n++;
            return n;
        }

        public void ResetWar()
        {
            for (int i = 0; i < outposts.Count; i++) outposts[i].ResetOwner();
            for (int i = 0; i < 2; i++) if (fortresses[i] != null) fortresses[i].Restore();
        }
    }
}

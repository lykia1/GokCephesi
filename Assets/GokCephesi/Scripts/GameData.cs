using System;
using System.Collections.Generic;
using UnityEngine;

namespace GokCephesi
{
    public enum MapKind { Home, Hunting, War }

    // ------------------------------------------------------------------
    // ULUSLAR
    // ------------------------------------------------------------------
    public static class Nations
    {
        public const int Monster = -1;

        public static readonly string[] Names = { "Arkon Federasyonu", "Velora Birliği" };
        public static readonly string[] Short = { "ARKON", "VELORA" };

        public static readonly string[] Desc =
        {
            "Çöl gezegeni Arkon'un savaşçı pilotları. Ağır zırh, sert vuruş ve demir disiplin. Kızıl bayrak asla yere düşmez.",
            "Bulut şehirlerinin teknoloji ustaları. Hız, hassasiyet ve ileri kalkan sistemleri. Mavi şimşek göğü yarar."
        };

        public static readonly Color[] Main =
        {
            new Color(0.95f, 0.36f, 0.18f),
            new Color(0.20f, 0.62f, 1.00f)
        };

        public static readonly Color[] Dark =
        {
            new Color(0.40f, 0.12f, 0.07f),
            new Color(0.07f, 0.18f, 0.42f)
        };

        public static readonly Color MonsterColor = new Color(0.55f, 0.95f, 0.30f);
        public static readonly Color NeutralColor = new Color(0.75f, 0.75f, 0.75f);

        public static int Enemy(int n) { return n == 0 ? 1 : 0; }

        public static Color ColorOf(int n)
        {
            if (n < 0) return MonsterColor;
            return Main[n];
        }

        public static string NameOf(int n)
        {
            if (n < 0) return "Yaratık";
            return Names[n];
        }

        static readonly string[] CallsignsArkon =
            { "Şahin", "Kartal", "Atmaca", "Doğan", "Börü", "Toygar", "Alp", "Kaya", "Yıldırım", "Bora", "Poyraz", "Kuzgun", "Tuğrul", "Akın" };
        static readonly string[] CallsignsVelora =
            { "Nova", "Orion", "Vega", "Lyra", "Sirius", "Atlas", "Rigel", "Draco", "Altair", "Cygnus", "Pollux", "Deneb", "Hydra", "Kepler" };

        public static string RandomCallsign(int nation)
        {
            var list = nation == 0 ? CallsignsArkon : CallsignsVelora;
            return list[UnityEngine.Random.Range(0, list.Length)] + "-" + UnityEngine.Random.Range(1, 99);
        }
    }

    // ------------------------------------------------------------------
    // GEMİ SINIFLARI
    // ------------------------------------------------------------------
    public class ShipClass
    {
        public string name;
        public string desc;
        public float hp;
        public float speed;
        public float boost;
        public float turn;            // derece / saniye
        public float laserDmg;
        public float fireInterval;
        public float missileDmg;
        public float missileCooldown;
        public float range;
        public float armor;           // 0..1 hasar azaltma
        public float structureBonus;  // üs/kale hasar çarpanı
        public Vector3 bodyScale;
        public float wingSpan;
    }

    public static class ShipClasses
    {
        public static readonly ShipClass[] All =
        {
            new ShipClass
            {
                name = "Avcı", desc = "Çok hızlı ve çevik. Seri lazer atar. Zırhı zayıf.",
                hp = 140, speed = 58, boost = 100, turn = 105, laserDmg = 7, fireInterval = 0.12f,
                missileDmg = 32, missileCooldown = 4f, range = 230, armor = 0f, structureBonus = 1f,
                bodyScale = new Vector3(1.1f, 0.9f, 4.4f), wingSpan = 5.5f
            },
            new ShipClass
            {
                name = "Saldırı", desc = "Dengeli gemi. Güçlü füzeleri vardır. Her işe uygun.",
                hp = 200, speed = 48, boost = 82, turn = 80, laserDmg = 10, fireInterval = 0.18f,
                missileDmg = 50, missileCooldown = 5f, range = 245, armor = 0.1f, structureBonus = 1.2f,
                bodyScale = new Vector3(1.5f, 1.1f, 4.8f), wingSpan = 6.5f
            },
            new ShipClass
            {
                name = "Zırhlı", desc = "Yavaş ama çok dayanıklı. Ağır top atar, hasarı azaltır.",
                hp = 330, speed = 38, boost = 64, turn = 60, laserDmg = 15, fireInterval = 0.3f,
                missileDmg = 42, missileCooldown = 6f, range = 260, armor = 0.25f, structureBonus = 1.2f,
                bodyScale = new Vector3(2.2f, 1.5f, 5.2f), wingSpan = 6f
            },
            new ShipClass
            {
                name = "Bombardıman", desc = "Üs ve kalelere çok büyük hasar verir. Dev bombalar taşır.",
                hp = 250, speed = 40, boost = 66, turn = 55, laserDmg = 9, fireInterval = 0.25f,
                missileDmg = 95, missileCooldown = 7f, range = 280, armor = 0.15f, structureBonus = 2.5f,
                bodyScale = new Vector3(2.6f, 1.2f, 4.6f), wingSpan = 9f
            }
        };
    }

    public struct ShipStats
    {
        public float maxHp, speed, boost, turn, laserDmg, fireInterval, missileDmg, missileCooldown, range, armor, structureBonus, regen;

        public static ShipStats Compute(int cls, int level, int upEngine, int upArmor, int upWeapon, int upShield)
        {
            var c = ShipClasses.All[Mathf.Clamp(cls, 0, ShipClasses.All.Length - 1)];
            float lv = Progression.LevelMul(level);
            var s = new ShipStats();
            s.maxHp = c.hp * lv * Progression.UpMul(upArmor);
            s.speed = c.speed * (1f + 0.04f * upEngine);
            s.boost = c.boost * (1f + 0.04f * upEngine);
            s.turn = c.turn * (1f + 0.02f * upEngine);
            s.laserDmg = c.laserDmg * lv * Progression.UpMul(upWeapon);
            s.fireInterval = c.fireInterval;
            s.missileDmg = c.missileDmg * lv * Progression.UpMul(upWeapon);
            s.missileCooldown = c.missileCooldown;
            s.range = c.range;
            s.armor = Mathf.Min(0.6f, c.armor + 0.02f * upShield);
            s.structureBonus = c.structureBonus;
            s.regen = 0.02f + 0.006f * upShield; // saniyede max HP oranı
            return s;
        }

        public static ShipStats Monster(int level)
        {
            var s = new ShipStats();
            s.maxHp = 60 + 38 * level;
            s.speed = 34 + level;
            s.boost = 60;
            s.turn = 70;
            s.laserDmg = 4f + 1.7f * level;
            s.fireInterval = 0.7f;
            s.missileDmg = 0;
            s.missileCooldown = 999;
            s.range = 170;
            s.armor = 0;
            s.structureBonus = 1;
            s.regen = 0.03f;
            return s;
        }
    }

    // ------------------------------------------------------------------
    // İLERLEME (SEVİYE, GELİŞTİRME)
    // ------------------------------------------------------------------
    public static class Progression
    {
        public const int MaxLevel = 30;
        public const int MaxUpgrade = 10;

        public static readonly string[] UpgradeNames = { "Motor", "Zırh", "Silah", "Kalkan" };
        public static readonly string[] UpgradeDesc =
        {
            "Hız ve dönüş +%4 / seviye",
            "Maksimum can +%8 / seviye",
            "Lazer ve füze hasarı +%8 / seviye",
            "Hasar azaltma +%2 ve onarım hızı / seviye"
        };

        public static int XpToNext(int lv) { return 80 + lv * lv * 20; }
        public static int UpgradeCost(int current) { return 120 + current * current * 70 + current * 90; }
        public static float LevelMul(int lv) { return 1f + 0.06f * (lv - 1); }
        public static float UpMul(int up) { return 1f + 0.08f * up; }
    }

    // ------------------------------------------------------------------
    // HARİTALAR
    // ------------------------------------------------------------------
    public class GateDef
    {
        public int target;
        public Vector3 pos;
        public GateDef(int target, Vector3 pos) { this.target = target; this.pos = pos; }
    }

    public class MapDef
    {
        public int id;
        public string name;
        public string subtitle;
        public MapKind kind;
        public int theme;       // 0 şehir, 1 vadi, 2 kanyon, 3 cephe
        public float radius;
        public int minLevel;
        public Color ground, ground2, sky, fog;
        public float fogDensity;
        public int monsterCount, minMonsterLv, maxMonsterLv;
        public int allyBots, enemyBots;
        public Vector3 spawn;   // ulus 0 için; savaş haritasında ulus 1 için z ters çevrilir
        public List<GateDef> gates = new List<GateDef>();
        public int seed;
    }

    public static class Maps
    {
        public const int Home = 0, Valley = 1, Front = 2, Canyon = 3;

        public static readonly MapDef[] All = Build();

        static MapDef[] Build()
        {
            var home = new MapDef
            {
                id = Home, name = "Ana Şehir", subtitle = "Güvenli bölge • Hangar burada", kind = MapKind.Home, theme = 0,
                radius = 600, minLevel = 1,
                ground = new Color(0.30f, 0.32f, 0.36f), ground2 = new Color(0.24f, 0.26f, 0.30f),
                sky = new Color(0.55f, 0.72f, 0.92f), fog = new Color(0.62f, 0.75f, 0.90f), fogDensity = 0.0012f,
                spawn = new Vector3(0, 40, -150), seed = 11
            };
            home.gates.Add(new GateDef(Valley, new Vector3(0, 45, 420)));

            var valley = new MapDef
            {
                id = Valley, name = "Kumul Vadisi", subtitle = "Avlanma bölgesi • Yaratık Sv. 1-7", kind = MapKind.Hunting, theme = 1,
                radius = 900, minLevel = 1,
                ground = new Color(0.78f, 0.62f, 0.40f), ground2 = new Color(0.70f, 0.54f, 0.34f),
                sky = new Color(0.95f, 0.78f, 0.55f), fog = new Color(0.93f, 0.78f, 0.58f), fogDensity = 0.0010f,
                monsterCount = 14, minMonsterLv = 1, maxMonsterLv = 7, allyBots = 1, enemyBots = 1,
                spawn = new Vector3(0, 40, -600), seed = 22
            };
            valley.gates.Add(new GateDef(Home, new Vector3(0, 45, -760)));
            valley.gates.Add(new GateDef(Front, new Vector3(-700, 45, 350)));
            valley.gates.Add(new GateDef(Canyon, new Vector3(700, 45, 350)));

            var front = new MapDef
            {
                id = Front, name = "Sınır Cephesi", subtitle = "ULUS SAVAŞI • Üsleri ele geçir, düşman kalesini yık", kind = MapKind.War, theme = 3,
                radius = 1100, minLevel = 4,
                ground = new Color(0.36f, 0.42f, 0.32f), ground2 = new Color(0.30f, 0.35f, 0.27f),
                sky = new Color(0.45f, 0.50f, 0.58f), fog = new Color(0.50f, 0.54f, 0.60f), fogDensity = 0.0009f,
                monsterCount = 4, minMonsterLv = 4, maxMonsterLv = 8, allyBots = 5, enemyBots = 6,
                spawn = new Vector3(0, 50, -760), seed = 33
            };
            front.gates.Add(new GateDef(Valley, new Vector3(-160, 45, -900)));
            front.gates.Add(new GateDef(Canyon, new Vector3(160, 45, -900)));

            var canyon = new MapDef
            {
                id = Canyon, name = "Kristal Kanyon", subtitle = "Zorlu av bölgesi • Yaratık Sv. 7-16", kind = MapKind.Hunting, theme = 2,
                radius = 900, minLevel = 7,
                ground = new Color(0.24f, 0.18f, 0.32f), ground2 = new Color(0.20f, 0.15f, 0.27f),
                sky = new Color(0.22f, 0.16f, 0.35f), fog = new Color(0.28f, 0.20f, 0.40f), fogDensity = 0.0014f,
                monsterCount = 16, minMonsterLv = 7, maxMonsterLv = 16, allyBots = 1, enemyBots = 2,
                spawn = new Vector3(-600, 40, 0), seed = 44
            };
            canyon.gates.Add(new GateDef(Valley, new Vector3(-760, 45, 0)));
            canyon.gates.Add(new GateDef(Front, new Vector3(0, 45, 760)));

            return new[] { home, valley, front, canyon };
        }
    }

    // ------------------------------------------------------------------
    // KAYIT
    // ------------------------------------------------------------------
    [Serializable]
    public class SaveData
    {
        public int nation = -1;
        public int shipClass = 0;
        public int level = 1;
        public int xp = 0;
        public int credits = 300;
        public int upEngine, upArmor, upWeapon, upShield;
        public int map = 0;
        public int kills, deaths, warWins, warLosses;
        public bool invertY = false;
        public bool sound = true;

        const string Key = "GokCephesi_Save_v1";

        public static SaveData Load()
        {
            string s = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(s)) return new SaveData();
            try
            {
                var d = JsonUtility.FromJson<SaveData>(s);
                return d ?? new SaveData();
            }
            catch (Exception)
            {
                return new SaveData();
            }
        }

        public void Store()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        public static void Wipe()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }

        public int GetUpgrade(int i)
        {
            switch (i)
            {
                case 0: return upEngine;
                case 1: return upArmor;
                case 2: return upWeapon;
                default: return upShield;
            }
        }

        public void SetUpgrade(int i, int v)
        {
            switch (i)
            {
                case 0: upEngine = v; break;
                case 1: upArmor = v; break;
                case 2: upWeapon = v; break;
                default: upShield = v; break;
            }
        }
    }
}

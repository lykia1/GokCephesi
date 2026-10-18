using System.Collections.Generic;
using UnityEngine;

namespace GokCephesi
{
    // ------------------------------------------------------------------
    // KAPI (haritalar arası geçiş)
    // ------------------------------------------------------------------
    public class Gate : MonoBehaviour
    {
        public int target;
        public string targetName;
        public int minLevel;
        public const float TriggerRadius = 22f;

        Transform ring, core;

        public void Build(int targetMap, string name, int minLv)
        {
            target = targetMap;
            targetName = name;
            minLevel = minLv;
            Color c = new Color(0.4f, 0.95f, 1f);

            ring = new GameObject("Halka").transform;
            ring.SetParent(transform, false);
            int n = 18;
            float r = 24f;
            for (int i = 0; i < n; i++)
            {
                float a = i * 360f / n;
                Vector3 p = Quaternion.Euler(0, 0, a) * Vector3.right * r;
                Gfx.Prim(PrimitiveType.Cube, ring, p, new Vector3(3f, 7f, 3f), new Vector3(0, 0, a), c, 2f, false);
            }
            core = Gfx.Prim(PrimitiveType.Sphere, transform, Vector3.zero, Vector3.one * 12f, new Color(0.7f, 1f, 1f), 3f, false).transform;

            float h = transform.position.y - r;
            if (h > 1f)
            {
                Gfx.Prim(PrimitiveType.Cylinder, transform, new Vector3(0, -r - h * 0.5f, 0), new Vector3(6f, h * 0.5f, 6f), new Color(0.4f, 0.42f, 0.46f));
                Gfx.Prim(PrimitiveType.Cylinder, transform, new Vector3(0, -transform.position.y + 0.3f, 0), new Vector3(40f, 0.3f, 40f), c, 1.2f, false);
            }

            if (World.I != null)
            {
                string txt = "Geçit → " + name + (minLv > 1 ? "  (Sv. " + minLv + "+)" : "");
                World.I.labels.Add(new WorldLabel { pos = transform.position + Vector3.up * 36f, text = txt, color = c, maxDist = 1200f });
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (ring != null) ring.Rotate(0, 0, 35f * dt, Space.Self);
            if (core != null) core.localScale = Vector3.one * (11f + Mathf.Sin(Time.time * 4f) * 2f);

            var p = GameManager.PlayerUnit;
            if (p != null && p.Alive && GameManager.I != null)
            {
                if ((p.transform.position - transform.position).sqrMagnitude < TriggerRadius * TriggerRadius)
                    GameManager.I.TryGate(this);
            }
        }
    }

    // ------------------------------------------------------------------
    // STRATEJİK ÜS (ele geçirilebilir nokta)
    // ------------------------------------------------------------------
    public class Outpost : MonoBehaviour
    {
        public const float CaptureRadius = 85f;
        public const float CaptureRate = 0.07f;   // gemi başına saniyede

        public string label;
        public int owner = -1;
        public float progress;                    // -1: Arkon'un, +1: Velora'nın
        public int presentN0, presentN1;

        readonly List<GameObject> tinted = new List<GameObject>();
        GameObject beacon;
        Transform spinner;
        int shownOwner = -99;

        public void Build(string name)
        {
            label = name;
            Color concrete = new Color(0.5f, 0.52f, 0.55f);
            Gfx.Prim(PrimitiveType.Cylinder, transform, new Vector3(0, 2f, 0), new Vector3(60f, 2f, 60f), concrete);
            Gfx.Prim(PrimitiveType.Cylinder, transform, new Vector3(0, 22f, 0), new Vector3(14f, 20f, 14f), concrete);
            tinted.Add(Gfx.Prim(PrimitiveType.Cylinder, transform, new Vector3(0, 4.2f, 0), new Vector3(48f, 0.3f, 48f), Nations.NeutralColor, 0.8f, false));
            beacon = Gfx.Prim(PrimitiveType.Sphere, transform, new Vector3(0, 48f, 0), Vector3.one * 12f, Nations.NeutralColor, 3f, false);

            spinner = new GameObject("Donen").transform;
            spinner.SetParent(transform, false);
            spinner.localPosition = new Vector3(0, 48f, 0);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f;
                Vector3 p = Quaternion.Euler(0, a, 0) * Vector3.forward * 12f;
                tinted.Add(Gfx.Prim(PrimitiveType.Cube, spinner, p, new Vector3(2f, 2f, 8f), new Vector3(0, a, 0), Nations.NeutralColor, 2f, false));
            }

            int n = 20;
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                Vector3 p = new Vector3(Mathf.Cos(a) * CaptureRadius, 3f, Mathf.Sin(a) * CaptureRadius);
                tinted.Add(Gfx.Prim(PrimitiveType.Cube, transform, p, new Vector3(2.5f, 6f, 2.5f), Nations.NeutralColor, 1.5f, false));
            }
            if (World.I != null)
                World.I.labels.Add(new WorldLabel { pos = transform.position + Vector3.up * 70f, text = name, color = Color.white, maxDist = 1500f });
            RefreshColors();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (spinner != null) spinner.Rotate(0, 50f * dt, 0);

            presentN0 = 0; presentN1 = 0;
            Vector3 c = transform.position;
            for (int i = 0; i < Unit.All.Count; i++)
            {
                var u = Unit.All[i];
                if (u == null || !u.Alive || u.IsStructure || u.nation < 0) continue;
                Vector3 d = u.transform.position - c;
                d.y = 0f;
                if (d.sqrMagnitude > CaptureRadius * CaptureRadius) continue;
                if (u.transform.position.y > 160f) continue;
                if (u.nation == 0) presentN0++; else presentN1++;
            }

            if (presentN0 > 0 && presentN1 == 0) progress -= CaptureRate * Mathf.Min(presentN0, 3) * dt;
            else if (presentN1 > 0 && presentN0 == 0) progress += CaptureRate * Mathf.Min(presentN1, 3) * dt;
            else if (presentN0 == 0 && presentN1 == 0)
            {
                float rest = owner == 0 ? -1f : owner == 1 ? 1f : 0f;
                progress = Mathf.MoveTowards(progress, rest, 0.02f * dt);
            }
            progress = Mathf.Clamp(progress, -1f, 1f);

            int old = owner;
            if (progress <= -1f) owner = 0;
            else if (progress >= 1f) owner = 1;
            else if (owner == 0 && progress > 0f) owner = -1;
            else if (owner == 1 && progress < 0f) owner = -1;

            if (old != owner && GameManager.I != null) GameManager.I.OnOutpostChanged(this, old, owner);
            if (shownOwner != owner) RefreshColors();
        }

        public bool Contested { get { return presentN0 > 0 && presentN1 > 0; } }

        void RefreshColors()
        {
            shownOwner = owner;
            Color c = owner < 0 ? Nations.NeutralColor : Nations.Main[owner];
            for (int i = 0; i < tinted.Count; i++) Gfx.Recolor(tinted[i], c, 1.5f);
            if (beacon != null) Gfx.Recolor(beacon, c, 3f);
        }

        public void ResetOwner()
        {
            owner = -1;
            progress = 0f;
            RefreshColors();
        }

        public Color OwnerColor { get { return owner < 0 ? Nations.NeutralColor : Nations.Main[owner]; } }
    }

    // ------------------------------------------------------------------
    // ULUS KALESİ (kalkanı düşünce yıkılabilir)
    // ------------------------------------------------------------------
    public class Fortress : Unit
    {
        Transform core, shield;
        float turretTimer;
        int turretSide;
        float baseHp;

        public override bool IsStructure { get { return true; } }
        public override Vector3 AimPoint { get { return transform.position + Vector3.up * 62f; } }

        public bool ShieldUp
        {
            get { return World.I == null || !World.I.AllOutpostsOwnedBy(Nations.Enemy(nation)); }
        }

        public void Build(int n)
        {
            nation = n;
            displayName = Nations.Short[n] + " Kalesi";
            int pl = GameManager.I != null ? GameManager.I.save.level : 1;
            level = Mathf.Max(5, pl + 2);
            baseHp = 4000f + 450f * pl;
            maxHp = baseHp;
            hp = maxHp;
            radius = 22f;

            Color main = Nations.Main[n];
            Color dark = Nations.Dark[n];
            Color stone = Color.Lerp(new Color(0.45f, 0.46f, 0.5f), dark, 0.35f);

            Gfx.Prim(PrimitiveType.Cube, transform, new Vector3(0, 12f, 0), new Vector3(130f, 24f, 130f), stone);
            Gfx.Prim(PrimitiveType.Cube, transform, new Vector3(0, 30f, 0), new Vector3(80f, 14f, 80f), dark);
            for (int i = 0; i < 4; i++)
            {
                float a = 45f + i * 90f;
                Vector3 p = Quaternion.Euler(0, a, 0) * Vector3.forward * 88f;
                Gfx.Prim(PrimitiveType.Cylinder, transform, p + Vector3.up * 30f, new Vector3(18f, 30f, 18f), stone);
                Gfx.Prim(PrimitiveType.Sphere, transform, p + Vector3.up * 62f, Vector3.one * 10f, main, 2f, false);
            }
            core = Gfx.Prim(PrimitiveType.Sphere, transform, new Vector3(0, 62f, 0), Vector3.one * 34f, main, 2.2f).transform;

            shield = new GameObject("Kalkan").transform;
            shield.SetParent(transform, false);
            shield.localPosition = new Vector3(0, 62f, 0);
            Color sc = Color.Lerp(main, Color.white, 0.5f);
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f;
                Vector3 p = Quaternion.Euler(0, a, 0) * Vector3.forward * 30f;
                Gfx.Prim(PrimitiveType.Cube, shield, p, new Vector3(14f, 30f, 1.5f), new Vector3(0, a, 0), sc, 1.8f, false);
            }
            if (World.I != null)
                World.I.labels.Add(new WorldLabel { pos = transform.position + Vector3.up * 110f, text = Nations.Names[n] + " Kalesi", color = main, maxDist = 2500f });
        }

        void Update()
        {
            float dt = Time.deltaTime;
            bool up = ShieldUp && Alive;
            if (shield != null)
            {
                if (shield.gameObject.activeSelf != up) shield.gameObject.SetActive(up);
                shield.Rotate(0, 25f * dt, 0);
            }
            if (!Alive) return;

            // Savunma kulesi
            turretTimer -= dt;
            if (turretTimer <= 0f)
            {
                turretTimer = 0.55f;
                var t = NearestHostile(nation, AimPoint, 330f, false, false);
                if (t != null)
                {
                    float a = 45f + turretSide * 90f;
                    turretSide = (turretSide + 1) % 4;
                    Vector3 muzzle = transform.position + Quaternion.Euler(0, a, 0) * Vector3.forward * 88f + Vector3.up * 66f;
                    Vector3 lead = t.AimPoint + t.Velocity * (Vector3.Distance(muzzle, t.AimPoint) / Projectile.LaserSpeed);
                    Vector3 dir = (lead - muzzle).normalized;
                    dir = Quaternion.Euler(Random.Range(-2f, 2f), Random.Range(-2f, 2f), 0) * dir;
                    int pl = GameManager.I != null ? GameManager.I.save.level : 1;
                    Projectile.FireLaser(this, muzzle, dir, 6f + 2f * pl, 360f, Color.Lerp(Nations.Main[nation], Color.white, 0.4f), 1.8f, 1f);
                    Sfx.Play(Sfx.Laser, muzzle, 0.15f);
                }
            }
        }

        public override void TakeDamage(float dmg, Unit attacker)
        {
            if (!Alive) return;
            if (ShieldUp)
            {
                Fx.Spark(AimPoint + Random.onUnitSphere * 30f, Color.white);
                if (attacker != null && attacker == GameManager.PlayerUnit && GameManager.I != null)
                    GameManager.I.ShieldHint();
                return;
            }
            hp -= dmg;
            if (attacker != null && attacker == GameManager.PlayerUnit && GameManager.I != null)
                GameManager.I.ShowDamage(AimPoint + Vector3.up * 20f, dmg);
            if (hp <= 0f)
            {
                hp = 0f;
                Fx.Explode(AimPoint, 60f, Nations.Main[nation]);
                Fx.Explode(transform.position + Vector3.up * 20f, 40f, Color.white);
                if (core != null) core.gameObject.SetActive(false);
                if (GameManager.I != null) GameManager.I.OnFortressDestroyed(this, attacker);
            }
        }

        public void Restore()
        {
            maxHp = baseHp;
            hp = maxHp;
            if (core != null) core.gameObject.SetActive(true);
        }
    }
}

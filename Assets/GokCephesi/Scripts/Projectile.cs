using System.Collections.Generic;
using UnityEngine;

namespace GokCephesi
{
    /// <summary>Lazer ve füzeler. Fizik motoru kullanmadan, çizgi-küre kesişimiyle isabet kontrolü yapar.</summary>
    public class Projectile : MonoBehaviour
    {
        public const float LaserSpeed = 420f;
        public const float MissileSpeed = 170f;

        static readonly Stack<Projectile> pool = new Stack<Projectile>();
        static readonly List<Projectile> active = new List<Projectile>();
        static Transform poolRoot;

        Unit owner;
        int ownerNation;
        float dmg, speed, life, structureMul, hitRadius, turnRate;
        Unit homing;
        bool missile, bomb;
        Vector3 dir;
        Color color;
        GameObject visual;

        static Projectile Get()
        {
            if (poolRoot == null)
            {
                poolRoot = new GameObject("Projectiles").transform;
                pool.Clear();
                active.Clear();
            }
            Projectile p;
            if (pool.Count > 0)
            {
                p = pool.Pop();
            }
            else
            {
                var go = new GameObject("Projectile");
                go.transform.SetParent(poolRoot, false);
                p = go.AddComponent<Projectile>();
                p.visual = Gfx.Prim(PrimitiveType.Cube, go.transform, Vector3.zero, Vector3.one, Color.white, 2f, false);
            }
            p.gameObject.SetActive(true);
            active.Add(p);
            return p;
        }

        void Release()
        {
            active.Remove(this);
            gameObject.SetActive(false);
            pool.Push(this);
        }

        public static void ClearAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
                if (active[i] != null) active[i].Release();
            active.Clear();
        }

        public static void FireLaser(Unit from, Vector3 pos, Vector3 dir, float dmg, float range, Color c, float thickness, float structureMul)
        {
            var p = Get();
            p.Init(from, pos, dir, dmg, LaserSpeed, range / LaserSpeed, false, false, null, c, structureMul);
            p.visual.transform.localScale = new Vector3(0.35f * thickness, 0.35f * thickness, 5f);
            p.hitRadius = 1.2f;
        }

        public static void FireMissile(Ship from, Vector3 pos, Vector3 dir, float dmg, Unit target, bool bomb)
        {
            var p = Get();
            Color c = bomb ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.45f, 0.2f);
            p.Init(from, pos, dir, dmg, bomb ? MissileSpeed * 0.8f : MissileSpeed, 4.5f, true, bomb, target, c, from.stats.structureBonus);
            p.visual.transform.localScale = bomb ? new Vector3(1.4f, 1.4f, 3f) : new Vector3(0.8f, 0.8f, 3.2f);
            p.hitRadius = bomb ? 6f : 3f;
        }

        void Init(Unit from, Vector3 pos, Vector3 d, float damage, float spd, float lifeTime, bool isMissile, bool isBomb, Unit target, Color c, float sMul)
        {
            owner = from;
            ownerNation = from.nation;
            structureMul = sMul;
            dmg = damage;
            speed = spd;
            life = lifeTime;
            missile = isMissile;
            bomb = isBomb;
            homing = target;
            turnRate = isBomb ? 90f : 200f;
            dir = d.normalized;
            color = c;
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(dir));
            Gfx.Recolor(visual, c, 2.5f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            if (life <= 0f)
            {
                if (missile) Fx.Spark(transform.position, color);
                Release();
                return;
            }

            if (missile && homing != null && homing.Alive)
            {
                Vector3 want = (homing.AimPoint - transform.position).normalized;
                dir = Vector3.RotateTowards(dir, want, turnRate * Mathf.Deg2Rad * dt, 0f);
            }

            Vector3 a = transform.position;
            Vector3 b = a + dir * speed * dt;

            // İsabet kontrolü
            for (int i = 0; i < Unit.All.Count; i++)
            {
                var u = Unit.All[i];
                if (u == null || !u.Alive || u == owner) continue;
                if (!Unit.Hostile(ownerNation, u.nation)) continue;
                float r = u.radius + hitRadius;
                Vector3 c = u.AimPoint;
                if ((c - a).sqrMagnitude > (speed * dt + r + 60f) * (speed * dt + r + 60f)) continue;
                if (SegmentSphere(a, b, c, r))
                {
                    float d = dmg * (u.IsStructure ? structureMul : 1f);
                    Unit attacker = owner != null && owner.gameObject.activeInHierarchy ? owner : null;
                    u.TakeDamage(d, attacker);
                    if (missile) Fx.Explode(b, bomb ? 8f : 4f, color);
                    else Fx.Spark(b, color);
                    Release();
                    return;
                }
            }

            if (b.y < 0.5f)
            {
                Fx.Spark(new Vector3(b.x, 0.5f, b.z), color);
                Release();
                return;
            }

            transform.SetPositionAndRotation(b, Quaternion.LookRotation(dir));
        }

        static bool SegmentSphere(Vector3 a, Vector3 b, Vector3 c, float r)
        {
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude;
            float t = len2 > 0.0001f ? Mathf.Clamp01(Vector3.Dot(c - a, ab) / len2) : 0f;
            Vector3 p = a + ab * t;
            return (p - c).sqrMagnitude <= r * r;
        }
    }
}

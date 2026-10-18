using System.Collections.Generic;
using UnityEngine;

namespace GokCephesi
{
    /// <summary>Oyuncunun gemisini kontrollerden okuyup uçurur, hedef kilitler ve ateş eder.</summary>
    [RequireComponent(typeof(Ship))]
    public class PlayerPilot : MonoBehaviour
    {
        Ship ship;
        public Unit target;
        float retargetTimer;
        readonly List<Unit> candidates = new List<Unit>();

        void Awake()
        {
            ship = GetComponent<Ship>();
        }

        public void ClearTarget()
        {
            target = null;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.screen != GameScreen.Playing || ship == null || !ship.Alive) return;

            var c = gm.controls;
            float dt = Time.deltaTime;
            float pitchIn = c.stick.y * (gm.save.invertY ? -1f : 1f);
            ship.Steer(c.stick.x, pitchIn, c.boost, c.brake, dt, true);

            // Hedef geçerli mi?
            if (target != null)
            {
                if (!target.Alive || !Unit.Hostile(ship.nation, target.nation) ||
                    Vector3.Distance(target.AimPoint, transform.position) > ship.stats.range * 2f)
                    target = null;
            }

            retargetTimer -= dt;
            if (target == null && retargetTimer <= 0f)
            {
                retargetTimer = 0.25f;
                target = FindInCone();
            }
            if (c.targetDown) target = CycleTarget();

            bool safe = World.I != null && World.I.SafeZone;
            if (safe)
            {
                if (c.fire || c.missileDown) gm.SafeZoneHint();
                return;
            }
            if (c.fire) ship.FireLaser(target);
            if (c.missileDown) ship.FireMissile(target);
        }

        Unit FindInCone()
        {
            Unit best = null;
            float bestScore = float.MaxValue;
            float maxD = ship.stats.range * 1.4f;
            var all = Unit.All;
            for (int i = 0; i < all.Count; i++)
            {
                var u = all[i];
                if (u == null || u == ship || !u.Alive || !Unit.Hostile(ship.nation, u.nation)) continue;
                Vector3 to = u.AimPoint - transform.position;
                float d = to.magnitude;
                if (d > maxD) continue;
                float ang = Vector3.Angle(transform.forward, to);
                if (ang > 40f) continue;
                float score = ang * 4f + d * 0.1f + (u.IsStructure ? 60f : 0f);
                if (score < bestScore) { bestScore = score; best = u; }
            }
            return best;
        }

        Unit CycleTarget()
        {
            candidates.Clear();
            float maxD = ship.stats.range * 1.8f;
            var all = Unit.All;
            for (int i = 0; i < all.Count; i++)
            {
                var u = all[i];
                if (u == null || u == ship || !u.Alive || !Unit.Hostile(ship.nation, u.nation)) continue;
                if (Vector3.Distance(u.AimPoint, transform.position) > maxD) continue;
                candidates.Add(u);
            }
            if (candidates.Count == 0) return null;
            Vector3 p = transform.position;
            candidates.Sort((a, b) => (a.AimPoint - p).sqrMagnitude.CompareTo((b.AimPoint - p).sqrMagnitude));
            int idx = target != null ? candidates.IndexOf(target) : -1;
            return candidates[(idx + 1) % candidates.Count];
        }
    }
}

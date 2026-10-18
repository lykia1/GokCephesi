using UnityEngine;

namespace GokCephesi
{
    /// <summary>
    /// Yapay zekâ pilotu. Ulus botları düşmanlara saldırır, üsleri ele geçirmeye çalışır ve
    /// kalkanı düşen düşman kalesine hücum eder. Yaratıklar kendi bölgelerinde dolaşır ve saldırılınca karşılık verir.
    /// </summary>
    public class BotPilot : MonoBehaviour
    {
        Ship ship;
        Vector3 home;
        bool monster;
        Unit target;
        Vector3 objective;
        bool hasObjective;
        float thinkTimer, respawnTimer = -1f, breakTimer, objectiveTimer;
        Vector3 breakPoint;
        float orbitSign;
        int preference;

        public bool IsMonster { get { return monster; } }
        public Ship Ship { get { return ship; } }

        public void Init(Ship s, Vector3 homePos, bool isMonster)
        {
            ship = s;
            home = homePos;
            monster = isMonster;
            orbitSign = Random.value < 0.5f ? -1f : 1f;
            preference = Random.Range(0, 3);
            thinkTimer = Random.Range(0f, 0.5f);
        }

        void Update()
        {
            if (ship == null || World.I == null) return;
            float dt = Time.deltaTime;

            if (!ship.Alive)
            {
                if (respawnTimer < 0f) respawnTimer = monster ? 1.5f : 9f;
                respawnTimer -= dt;
                if (respawnTimer <= 0f)
                {
                    respawnTimer = -1f;
                    if (monster) { Destroy(gameObject); return; }
                    Vector3 sp = World.I.SpawnFor(ship.nation);
                    ship.Respawn(sp, World.YawTowards(sp, Vector3.zero));
                    target = null;
                    hasObjective = false;
                }
                return;
            }

            thinkTimer -= dt;
            if (thinkTimer <= 0f)
            {
                thinkTimer = Random.Range(0.3f, 0.6f);
                Think();
            }
            Act(dt);
        }

        // ------------------------------------------------------------------
        void Think()
        {
            if (monster)
            {
                Unit cand = null;
                if (ship.lastAttacker != null && ship.lastAttacker.Alive && Time.time - ship.lastDamageTime < 12f)
                    cand = ship.lastAttacker;
                if (cand == null)
                    cand = Unit.NearestHostile(ship.nation, transform.position, 100f, false, false);
                if (cand != null && (cand.transform.position - home).magnitude > 450f) cand = null;
                target = cand;
                if (target == null && (!hasObjective || objectiveTimer <= 0f)) PickWander(140f);
                return;
            }

            Unit enemy = Unit.NearestHostile(ship.nation, transform.position, 320f, false, false);
            if (enemy == null && ship.lastAttacker != null && ship.lastAttacker.Alive && Time.time - ship.lastDamageTime < 8f)
                enemy = ship.lastAttacker;
            if (enemy == null)
            {
                var m = Unit.NearestHostile(ship.nation, transform.position, 220f, true, false);
                if (m != null && m.nation < 0) enemy = m;
            }
            if (enemy == null && World.I.IsWar)
            {
                var ef = World.I.fortresses[Nations.Enemy(ship.nation)];
                if (ef != null && ef.Alive && !ef.ShieldUp) enemy = ef;
            }
            target = enemy;

            if (target == null)
            {
                if (World.I.IsWar) ChooseWarObjective();
                else if (!hasObjective || objectiveTimer <= 0f) PickWander(World.I.def.radius * 0.7f);
            }
        }

        void PickWander(float range)
        {
            Vector3 c = monster ? home : Vector3.zero;
            Vector2 r = Random.insideUnitCircle * range;
            objective = new Vector3(c.x + r.x, Random.Range(30f, 90f), c.z + r.y);
            hasObjective = true;
            objectiveTimer = Random.Range(6f, 12f);
        }

        void ChooseWarObjective()
        {
            var ops = World.I.outposts;
            Outpost best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < ops.Count; i++)
            {
                var o = ops[i];
                if (o.owner == ship.nation && !o.Contested) continue;
                float d = Vector3.Distance(transform.position, o.transform.position);
                if (i == preference) d *= 0.6f;      // botlar farklı üslere dağılsın
                if (o.Contested && o.owner == ship.nation) d *= 0.5f; // kendi üssünü savun
                if (d < bestScore) { bestScore = d; best = o; }
            }
            if (best == null && ops.Count > 0) best = ops[preference % ops.Count];
            if (best != null)
            {
                float a = Time.time * 0.4f * orbitSign + preference * 2.1f;
                objective = best.transform.position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * Outpost.CaptureRadius * 0.55f;
                objective.y = 35f + preference * 8f;
                hasObjective = true;
            }
        }

        // ------------------------------------------------------------------
        void Act(float dt)
        {
            objectiveTimer -= dt;

            if (target != null && target.Alive)
            {
                Vector3 tp = target.AimPoint;
                Vector3 to = tp - transform.position;
                float dist = to.magnitude;
                float range = ship.stats.range;
                float breakDist = target.IsStructure ? 110f : 45f;

                if (breakTimer > 0f)
                {
                    breakTimer -= dt;
                    ship.FlyTowards(breakPoint, false, dt);
                }
                else
                {
                    if (dist < breakDist)
                    {
                        breakTimer = Random.Range(1.2f, 2.2f);
                        breakPoint = transform.position + transform.forward * 140f + transform.right * orbitSign * 90f + Vector3.up * Random.Range(-10f, 40f);
                    }
                    Vector3 lead = tp + target.Velocity * Mathf.Min(1.5f, dist / Projectile.LaserSpeed);
                    ship.FlyTowards(lead, dist > range * 1.2f && !monster, dt);
                }

                if (dist < range && Vector3.Angle(transform.forward, to) < 22f)
                {
                    ship.FireLaser(target);
                    if (!monster && dist < range * 1.2f && Random.value < dt * 0.35f)
                        ship.FireMissile(target);
                }

                if (monster && (transform.position - home).magnitude > 500f) target = null;
                return;
            }

            if (hasObjective)
            {
                ship.FlyTowards(objective, !monster && (objective - transform.position).magnitude > 300f, dt);
                if ((objective - transform.position).magnitude < 35f)
                {
                    if (World.I.IsWar && !monster) ChooseWarObjective();
                    else objectiveTimer = 0f;
                }
            }
            else
            {
                ship.Steer(0.3f * orbitSign, 0f, false, false, dt, true);
            }
        }
    }
}

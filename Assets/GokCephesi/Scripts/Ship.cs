using System.Collections.Generic;
using UnityEngine;

namespace GokCephesi
{
    /// <summary>
    /// Uçan her şey (oyuncu, ulus botları ve yaratıklar) bu sınıfı kullanır.
    /// Uçuş, silahlar, hasar ve görünüm burada.
    /// </summary>
    public class Ship : Unit
    {
        public ShipStats stats;
        public int shipClass;
        public bool isPlayer;
        public bool isMonster;

        public float yaw, pitch, speedCur;
        public bool boosting;
        public float aimSpread = 0f;       // botlar için nişan sapması (derece)

        public Unit lastAttacker;
        public float lastDamageTime = -99f;

        Transform model;
        readonly List<Transform> engineGlows = new List<Transform>();
        Transform spinner;
        float roll;
        float fireTimer, missileTimer;
        int muzzleSide;
        float wingOffset = 2f;
        Vector3 velocity;

        public override Vector3 Velocity { get { return velocity; } }

        public float MissileReady01
        {
            get
            {
                if (stats.missileCooldown <= 0f || stats.missileCooldown > 100f) return 0f;
                return 1f - Mathf.Clamp01(missileTimer / stats.missileCooldown);
            }
        }

        public float MissileSecondsLeft { get { return Mathf.Max(0f, missileTimer); } }

        public Color TeamColor { get { return Nations.ColorOf(nation); } }

        // ------------------------------------------------------------------
        // KURULUM
        // ------------------------------------------------------------------
        public void Setup(int nation, int cls, int level, ShipStats st, string name, bool player)
        {
            this.nation = nation;
            shipClass = cls;
            this.level = level;
            displayName = name;
            isPlayer = player;
            isMonster = false;
            stats = st;
            maxHp = st.maxHp;
            hp = maxHp;
            speedCur = st.speed;
            BuildShipVisual();
        }

        public void SetupMonster(int level)
        {
            nation = Nations.Monster;
            this.level = level;
            isMonster = true;
            isPlayer = false;
            stats = ShipStats.Monster(level);
            maxHp = stats.maxHp;
            hp = maxHp;
            speedCur = stats.speed;
            displayName = MonsterName(level);
            BuildMonsterVisual();
        }

        static string MonsterName(int lv)
        {
            if (lv <= 3) return "Kum Böceği";
            if (lv <= 6) return "Çöl Akrebi";
            if (lv <= 9) return "Taş Yiyen";
            if (lv <= 12) return "Kristal Örümcek";
            return "Boşluk Canavarı";
        }

        public void ApplyStats(ShipStats st, bool heal)
        {
            float ratio = maxHp > 0 ? hp / maxHp : 1f;
            stats = st;
            maxHp = st.maxHp;
            hp = heal ? maxHp : Mathf.Max(1f, maxHp * ratio);
        }

        public void ChangeClass(int cls, ShipStats st)
        {
            shipClass = cls;
            ApplyStats(st, true);
            BuildShipVisual();
        }

        void ClearVisual()
        {
            engineGlows.Clear();
            spinner = null;
            if (model != null) Destroy(model.gameObject);
        }

        void BuildShipVisual()
        {
            ClearVisual();
            var c = ShipClasses.All[shipClass];
            Color main = Nations.Main[nation];
            Color dark = Nations.Dark[nation];
            Color metal = new Color(0.78f, 0.80f, 0.84f);
            Color glass = new Color(0.25f, 0.85f, 1f);
            Color flame = nation == 0 ? new Color(1f, 0.55f, 0.15f) : new Color(0.3f, 0.8f, 1f);

            var root = new GameObject("Model").transform;
            root.SetParent(transform, false);
            model = root;

            Vector3 b = c.bodyScale;
            float span = c.wingSpan;

            // Gövde
            Gfx.Prim(PrimitiveType.Capsule, root, Vector3.zero, new Vector3(b.x, b.z * 0.5f, b.y), new Vector3(90, 0, 0), metal);
            // Burun
            Gfx.Prim(PrimitiveType.Sphere, root, new Vector3(0, 0, b.z * 0.48f), new Vector3(b.x * 0.6f, b.y * 0.55f, b.x * 0.9f), main);
            // Kokpit
            Gfx.Prim(PrimitiveType.Sphere, root, new Vector3(0, b.y * 0.42f, b.z * 0.18f), new Vector3(b.x * 0.55f, b.y * 0.45f, b.z * 0.28f), glass, 0.6f);
            // Kanatlar
            Gfx.Prim(PrimitiveType.Cube, root, new Vector3(0, -b.y * 0.1f, -b.z * 0.08f), new Vector3(span, 0.18f, b.z * 0.32f), main);
            Gfx.Prim(PrimitiveType.Cube, root, new Vector3(span * 0.5f, 0.15f, -b.z * 0.12f), new Vector3(0.2f, 0.9f, b.z * 0.22f), dark);
            Gfx.Prim(PrimitiveType.Cube, root, new Vector3(-span * 0.5f, 0.15f, -b.z * 0.12f), new Vector3(0.2f, 0.9f, b.z * 0.22f), dark);
            // Kuyruk
            Gfx.Prim(PrimitiveType.Cube, root, new Vector3(0, b.y * 0.55f, -b.z * 0.38f), new Vector3(0.18f, b.y * 0.9f, b.z * 0.2f), dark);
            Gfx.Prim(PrimitiveType.Cube, root, new Vector3(0, b.y * 0.15f, -b.z * 0.42f), new Vector3(span * 0.4f, 0.12f, b.z * 0.14f), main);

            // Motorlar
            float ex = b.x * 0.45f;
            for (int s = -1; s <= 1; s += 2)
            {
                Gfx.Prim(PrimitiveType.Cylinder, root, new Vector3(ex * s, -b.y * 0.05f, -b.z * 0.42f), new Vector3(0.7f, b.z * 0.18f, 0.7f), new Vector3(90, 0, 0), dark);
                var glow = Gfx.Prim(PrimitiveType.Sphere, root, new Vector3(ex * s, -b.y * 0.05f, -b.z * 0.6f), new Vector3(0.6f, 0.6f, 1.2f), flame, 4f, false);
                engineGlows.Add(glow.transform);
            }

            // Sınıfa özel parçalar
            switch (shipClass)
            {
                case 0: // Avcı: ön kanatçıklar
                    Gfx.Prim(PrimitiveType.Cube, root, new Vector3(0, 0, b.z * 0.3f), new Vector3(span * 0.45f, 0.1f, 0.6f), main);
                    break;
                case 1: // Saldırı: füze rampaları
                    for (int s = -1; s <= 1; s += 2)
                        Gfx.Prim(PrimitiveType.Capsule, root, new Vector3(span * 0.3f * s, -0.4f, 0), new Vector3(0.35f, 0.9f, 0.35f), new Vector3(90, 0, 0), new Color(0.9f, 0.9f, 0.3f));
                    break;
                case 2: // Zırhlı: zırh plakaları
                    for (int s = -1; s <= 1; s += 2)
                        Gfx.Prim(PrimitiveType.Cube, root, new Vector3(b.x * 0.55f * s, 0.1f, 0.2f), new Vector3(0.5f, b.y * 0.9f, b.z * 0.55f), dark);
                    Gfx.Prim(PrimitiveType.Cylinder, root, new Vector3(0, -b.y * 0.55f, b.z * 0.25f), new Vector3(0.45f, 1.2f, 0.45f), new Vector3(90, 0, 0), metal);
                    break;
                case 3: // Bombardıman: bomba kapsülleri
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Gfx.Prim(PrimitiveType.Capsule, root, new Vector3(span * 0.22f * s, -0.7f, -0.3f), new Vector3(0.7f, 1.3f, 0.7f), new Vector3(90, 0, 0), dark);
                        Gfx.Prim(PrimitiveType.Capsule, root, new Vector3(span * 0.4f * s, -0.5f, -0.3f), new Vector3(0.5f, 1.0f, 0.5f), new Vector3(90, 0, 0), dark);
                    }
                    break;
            }

            // Oyuncunun gemisinin altında küçük bir işaret ışığı
            if (isPlayer)
                Gfx.Prim(PrimitiveType.Sphere, root, new Vector3(0, -b.y * 0.6f, 0), Vector3.one * 0.5f, new Color(1f, 1f, 0.6f), 3f, false);

            radius = Mathf.Max(b.z * 0.6f, span * 0.45f);
            wingOffset = span * 0.35f;
        }

        void BuildMonsterVisual()
        {
            ClearVisual();
            var root = new GameObject("Model").transform;
            root.SetParent(transform, false);
            model = root;

            float size = 4f + level * 0.25f;
            bool crystal = level >= 9;
            Color body = crystal ? new Color(0.55f, 0.35f, 0.85f) : new Color(0.55f, 0.62f, 0.25f);
            Color spikeC = crystal ? new Color(0.9f, 0.4f, 1f) : new Color(0.85f, 0.75f, 0.35f);

            Gfx.Prim(PrimitiveType.Sphere, root, Vector3.zero, new Vector3(size, size * 0.7f, size * 1.2f), body);
            Gfx.Prim(PrimitiveType.Sphere, root, new Vector3(-size * 0.2f, size * 0.12f, size * 0.52f), Vector3.one * size * 0.18f, new Color(1f, 0.15f, 0.1f), 3f, false);
            Gfx.Prim(PrimitiveType.Sphere, root, new Vector3(size * 0.2f, size * 0.12f, size * 0.52f), Vector3.one * size * 0.18f, new Color(1f, 0.15f, 0.1f), 3f, false);

            var sp = new GameObject("Spikes").transform;
            sp.SetParent(root, false);
            spinner = sp;
            int n = 6;
            for (int i = 0; i < n; i++)
            {
                float a = i * 360f / n;
                var dir = Quaternion.Euler(0, 0, a) * Vector3.up;
                Gfx.Prim(PrimitiveType.Cube, sp, dir * size * 0.55f, new Vector3(size * 0.15f, size * 0.6f, size * 0.15f), new Vector3(0, 0, a), spikeC, crystal ? 1.5f : 0f);
            }
            radius = size * 0.65f;
            wingOffset = size * 0.3f;
        }

        // ------------------------------------------------------------------
        // UÇUŞ
        // ------------------------------------------------------------------
        public void Steer(float yawIn, float pitchIn, bool boost, bool brake, float dt, bool autoLevel)
        {
            if (!Alive) return;
            yawIn = Mathf.Clamp(yawIn, -1f, 1f);
            pitchIn = Mathf.Clamp(pitchIn, -1f, 1f);

            yaw += yawIn * stats.turn * dt;
            if (yaw > 360f) yaw -= 360f;
            if (yaw < -360f) yaw += 360f;
            pitch = Mathf.Clamp(pitch + pitchIn * stats.turn * 0.75f * dt, -60f, 60f);
            if (autoLevel && Mathf.Abs(pitchIn) < 0.08f) pitch = Mathf.MoveTowards(pitch, 0f, 18f * dt);

            boosting = boost && !brake;
            float targetSpeed = brake ? stats.speed * 0.3f : (boosting ? stats.boost : stats.speed);
            speedCur = Mathf.MoveTowards(speedCur, targetSpeed, (boosting ? 70f : 45f) * dt);

            var rot = Quaternion.Euler(-pitch, yaw, 0f);
            Vector3 old = transform.position;
            Vector3 pos = old + rot * Vector3.forward * speedCur * dt;
            if (World.I != null) World.I.Constrain(ref pos, radius);
            velocity = (pos - old) / Mathf.Max(dt, 0.0001f);
            transform.SetPositionAndRotation(pos, rot);

            roll = Mathf.Lerp(roll, -yawIn * 45f, dt * 4f);
            if (model != null) model.localRotation = Quaternion.Euler(0, 0, roll);
        }

        /// <summary>Botlar için: bir noktaya doğru dön ve uç.</summary>
        public void FlyTowards(Vector3 point, bool boost, float dt)
        {
            Vector3 d = point - transform.position;
            float wantYaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            float flat = new Vector2(d.x, d.z).magnitude;
            float wantPitch = Mathf.Clamp(Mathf.Atan2(d.y, Mathf.Max(flat, 0.01f)) * Mathf.Rad2Deg, -45f, 45f);
            float k = Mathf.Max(1f, stats.turn * dt * 3f);
            float yIn = Mathf.DeltaAngle(yaw, wantYaw) / k;
            float pIn = (wantPitch - pitch) / k;
            Steer(yIn, pIn, boost, false, dt, false);
        }

        public void Teleport(Vector3 pos, float newYaw)
        {
            yaw = newYaw;
            pitch = 0f;
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            velocity = Vector3.zero;
        }

        // ------------------------------------------------------------------
        // SİLAHLAR
        // ------------------------------------------------------------------
        void Update()
        {
            float dt = Time.deltaTime;
            if (fireTimer > 0f) fireTimer -= dt;
            if (missileTimer > 0f) missileTimer -= dt;

            if (Alive && hp < maxHp && Time.time - lastDamageTime > 5f)
                hp = Mathf.Min(maxHp, hp + maxHp * stats.regen * dt);

            if (spinner != null) spinner.Rotate(0, 0, 60f * dt, Space.Self);
            if (engineGlows.Count > 0)
            {
                float k = Mathf.Clamp01(speedCur / Mathf.Max(1f, stats.boost));
                float z = 0.8f + k * 2.2f + Random.value * 0.2f;
                for (int i = 0; i < engineGlows.Count; i++)
                    engineGlows[i].localScale = new Vector3(0.6f, 0.6f, z);
            }
        }

        Vector3 Lead(Unit t, float projSpeed, Vector3 from)
        {
            Vector3 tp = t.AimPoint;
            float time = Vector3.Distance(from, tp) / projSpeed;
            return tp + t.Velocity * time;
        }

        public bool CanFire { get { return fireTimer <= 0f && Alive; } }
        public bool CanMissile { get { return missileTimer <= 0f && Alive && stats.missileDmg > 0f; } }

        public bool FireLaser(Unit tgt)
        {
            if (!CanFire) return false;
            fireTimer = stats.fireInterval;

            Vector3 side = transform.right * (muzzleSide == 0 ? -wingOffset : wingOffset);
            muzzleSide ^= 1;
            Vector3 muzzle = transform.position + transform.forward * (radius + 1f) + side;
            Vector3 dir = transform.forward;

            if (tgt != null && tgt.Alive)
            {
                Vector3 to = Lead(tgt, Projectile.LaserSpeed, muzzle) - muzzle;
                float cone = isPlayer ? 30f : 22f;
                if (to.magnitude < stats.range * 1.1f && Vector3.Angle(transform.forward, to) < cone)
                    dir = to.normalized;
            }
            if (aimSpread > 0f)
                dir = Quaternion.Euler(Random.Range(-aimSpread, aimSpread), Random.Range(-aimSpread, aimSpread), 0f) * dir;

            Color c = isMonster ? new Color(0.6f, 1f, 0.3f) : Color.Lerp(TeamColor, Color.white, 0.35f);
            Projectile.FireLaser(this, muzzle, dir, stats.laserDmg, stats.range, c, isMonster ? 1.6f : 1f, stats.structureBonus);
            Sfx.Play(Sfx.Laser, muzzle, isPlayer ? 0.3f : 0.12f);
            return true;
        }

        public bool FireMissile(Unit tgt)
        {
            if (!CanMissile) return false;
            missileTimer = stats.missileCooldown;
            Vector3 muzzle = transform.position - transform.up * 1.2f + transform.forward * radius;
            Unit homing = null;
            if (tgt != null && tgt.Alive && Vector3.Distance(tgt.AimPoint, transform.position) < stats.range * 1.5f)
                homing = tgt;
            bool bomb = shipClass == 3;
            Projectile.FireMissile(this, muzzle, transform.forward, stats.missileDmg, homing, bomb);
            Sfx.Play(Sfx.Missile, muzzle, isPlayer ? 0.5f : 0.2f);
            return true;
        }

        // ------------------------------------------------------------------
        // HASAR / ÖLÜM
        // ------------------------------------------------------------------
        public override void TakeDamage(float dmg, Unit attacker)
        {
            if (!Alive) return;
            if (World.I != null && World.I.SafeZone) return;

            float d = dmg * (1f - stats.armor);
            hp -= d;
            lastDamageTime = Time.time;
            if (attacker != null) lastAttacker = attacker;

            if (isPlayer && GameManager.I != null) GameManager.I.OnPlayerHit(d);
            if (attacker != null && attacker == GameManager.PlayerUnit && GameManager.I != null)
                GameManager.I.ShowDamage(transform.position + Vector3.up * radius, d);

            if (hp <= 0f)
            {
                hp = 0f;
                Fx.Explode(transform.position, radius * 2.5f, isMonster ? Nations.MonsterColor : TeamColor);
                if (model != null) model.gameObject.SetActive(false);
                if (GameManager.I != null) GameManager.I.OnUnitKilled(this, attacker);
            }
        }

        public void Respawn(Vector3 pos, float newYaw)
        {
            hp = maxHp;
            fireTimer = 0f;
            missileTimer = 0f;
            lastAttacker = null;
            speedCur = stats.speed;
            Teleport(pos, newYaw);
            if (model != null) model.gameObject.SetActive(true);
        }
    }
}

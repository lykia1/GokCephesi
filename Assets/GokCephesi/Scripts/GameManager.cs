using System.Collections.Generic;
using UnityEngine;

namespace GokCephesi
{
    public enum GameScreen { NationSelect, ClassSelect, Playing }

    /// <summary>
    /// Oyunun beyni: ekran akışı, harita geçişleri, ödüller, seviye atlama, ulus savaşı ve kayıt.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I;
        public static Ship PlayerUnit { get { return I != null ? I.player : null; } }

        public SaveData save;
        public GameScreen screen;
        public Ship player;
        public PlayerPilot pilot;
        public World world;
        public readonly MobileControls controls = new MobileControls();
        public ChaseCamera chaseCam;

        public bool hangarOpen, menuOpen;
        public float respawnTimer;
        public int pendingNation = -1;

        public struct Msg { public string text; public Color color; public float time; }
        public struct DmgText { public Vector3 pos; public float amount; public float time; }
        public readonly List<Msg> messages = new List<Msg>();
        public readonly List<DmgText> damageTexts = new List<DmgText>();

        public string bannerTitle, bannerSub;
        public Color bannerColor = Color.white;
        public float bannerTime = -99f;
        public float hitFlash;

        float gateCooldown, shieldHintTime, safeHintTime, saveTimer;
        float warResetTimer = -1f;

        // ------------------------------------------------------------------
        void Awake()
        {
            I = this;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;

            Sfx.Init(gameObject);
            gameObject.AddComponent<HUD>();

            var cam = Camera.main;
            if (cam == null)
            {
                var cgo = new GameObject("Main Camera");
                cgo.tag = "MainCamera";
                cam = cgo.AddComponent<Camera>();
                cgo.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.07f, 0.12f);
            chaseCam = cam.GetComponent<ChaseCamera>();
            if (chaseCam == null) chaseCam = cam.gameObject.AddComponent<ChaseCamera>();

            save = SaveData.Load();
            Sfx.Enabled = save.sound;
            if (save.nation < 0 || save.nation > 1) screen = GameScreen.NationSelect;
            else StartGame();
        }

        public ShipStats PlayerStats()
        {
            return ShipStats.Compute(save.shipClass, save.level, save.upEngine, save.upArmor, save.upWeapon, save.upShield);
        }

        // ------------------------------------------------------------------
        // MENÜ AKIŞI
        // ------------------------------------------------------------------
        public void ChooseNation(int n)
        {
            pendingNation = n;
            screen = GameScreen.ClassSelect;
        }

        public void BackToNationSelect()
        {
            screen = GameScreen.NationSelect;
        }

        public void ChooseClass(int cls)
        {
            save.nation = pendingNation;
            save.shipClass = cls;
            save.map = Maps.Home;
            save.Store();
            StartGame();
            Banner("HOŞ GELDİN PİLOT", Nations.Names[save.nation] + " seni bekliyordu.", Nations.Main[save.nation]);
            AddMsg("İpucu: Parlayan geçitlerden geçerek bölgeler arasında dolaş.", new Color(0.7f, 0.95f, 1f));
            AddMsg("İpucu: Ana Şehir'de HANGAR butonundan gemini geliştir.", new Color(0.7f, 0.95f, 1f));
        }

        void StartGame()
        {
            screen = GameScreen.Playing;
            CreatePlayer();
            LoadMap(save.map, -1);
        }

        void CreatePlayer()
        {
            if (player != null) Destroy(player.gameObject);
            var go = new GameObject("Oyuncu");
            player = go.AddComponent<Ship>();
            player.Setup(save.nation, save.shipClass, save.level, PlayerStats(), "Sen", true);
            pilot = go.AddComponent<PlayerPilot>();
            chaseCam.target = player.transform;
        }

        public void ResetProgress()
        {
            SaveData.Wipe();
            Projectile.ClearAll();
            if (world != null) { world.gameObject.SetActive(false); Destroy(world.gameObject); world = null; }
            if (player != null) { Destroy(player.gameObject); player = null; }
            save = new SaveData();
            hangarOpen = menuOpen = false;
            messages.Clear();
            screen = GameScreen.NationSelect;
            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = new Color(0.05f, 0.07f, 0.12f);
        }

        // ------------------------------------------------------------------
        // HARİTA
        // ------------------------------------------------------------------
        public void LoadMap(int id, int fromMap)
        {
            if (id < 0 || id >= Maps.All.Length) id = Maps.Home;
            Projectile.ClearAll();
            if (world != null)
            {
                world.gameObject.SetActive(false);
                Destroy(world.gameObject);
            }
            warResetTimer = -1f;
            world = World.Build(Maps.All[id], save.nation);
            save.map = id;
            save.Store();

            Vector3 pos;
            float yaw;
            Gate g = fromMap >= 0 ? world.GateTo(fromMap) : null;
            if (g != null)
            {
                pos = g.transform.position + g.transform.forward * 70f;
                yaw = World.YawTowards(g.transform.position, pos);
            }
            else
            {
                pos = world.PlayerSpawn;
                yaw = World.YawTowards(pos, new Vector3(0, pos.y, 0));
                if (id == Maps.Home) yaw = 0f;
            }

            if (player.Alive) player.Teleport(pos, yaw);
            else player.Respawn(pos, yaw);

            gateCooldown = 2.5f;
            hangarOpen = false;
            pilot.ClearTarget();
            controls.Reset();
            chaseCam.Snap();

            var def = Maps.All[id];
            Banner(def.name, def.subtitle, Color.white);
            Sfx.Play2D(Sfx.Warp, 0.6f);
        }

        public void TryGate(Gate g)
        {
            if (gateCooldown > 0f || world == null) return;
            if (save.level < g.minLevel)
            {
                gateCooldown = 2f;
                AddMsg(g.targetName + " için en az " + g.minLevel + ". seviye olmalısın.", new Color(1f, 0.5f, 0.4f));
                return;
            }
            LoadMap(g.target, world.def.id);
        }

        public bool CanReturnToCity { get { return world != null && !world.SafeZone; } }

        public void ReturnToCity()
        {
            if (player == null) return;
            if (player.Alive && Time.time - player.lastDamageTime < 6f)
            {
                AddMsg("Çatışmadayken şehre dönemezsin!", new Color(1f, 0.5f, 0.4f));
                return;
            }
            LoadMap(Maps.Home, -1);
        }

        // ------------------------------------------------------------------
        void Update()
        {
            float dt = Time.deltaTime;
            UI.Recalc();

            // Eski mesajları temizle
            for (int i = messages.Count - 1; i >= 0; i--)
                if (Time.time - messages[i].time > 6f) messages.RemoveAt(i);
            for (int i = damageTexts.Count - 1; i >= 0; i--)
                if (Time.time - damageTexts[i].time > 1f) damageTexts.RemoveAt(i);

            if (screen != GameScreen.Playing || player == null) return;

            controls.blocked = hangarOpen || menuOpen || !player.Alive;
            controls.Update();
            if (InputBridge.Key(GameKey.Menu) && !menuOpen && !hangarOpen) menuOpen = true;

            gateCooldown -= dt;
            if (hitFlash > 0f) hitFlash -= dt;

            if (!player.Alive)
            {
                respawnTimer -= dt;
                if (respawnTimer <= 0f) RespawnPlayer(false);
            }

            if (warResetTimer > 0f)
            {
                warResetTimer -= dt;
                if (warResetTimer <= 0f && world != null)
                {
                    world.ResetWar();
                    Banner("YENİ ULUS SAVAŞI", "Üsler tarafsız. Savaş yeniden başlıyor!", new Color(1f, 0.9f, 0.4f));
                }
            }

            saveTimer += dt;
            if (saveTimer > 20f) { saveTimer = 0f; save.Store(); }
        }

        public void RespawnPlayer(bool inCity)
        {
            if (inCity && world != null && world.def.id != Maps.Home)
            {
                player.Respawn(Vector3.zero, 0f);
                LoadMap(Maps.Home, -1);
                return;
            }
            Vector3 pos = world != null ? world.PlayerSpawn : new Vector3(0, 40, 0);
            player.Respawn(pos, World.YawTowards(pos, new Vector3(0, pos.y, 0)));
            pilot.ClearTarget();
            chaseCam.Snap();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && save != null) save.Store();
        }

        void OnApplicationQuit()
        {
            if (save != null) save.Store();
        }

        // ------------------------------------------------------------------
        // ÖDÜLLER VE OLAYLAR
        // ------------------------------------------------------------------
        public void OnUnitKilled(Unit victim, Unit killer)
        {
            if (victim == player)
            {
                save.deaths++;
                respawnTimer = 6f;
                hangarOpen = false;
                string by = killer != null ? killer.displayName : "Bilinmeyen";
                AddMsg(by + " seni düşürdü!", new Color(1f, 0.4f, 0.35f));
                save.Store();
                return;
            }

            var vs = victim as Ship;
            if (vs == null) return;

            if (killer != null && killer == player)
            {
                if (vs.isMonster)
                {
                    int xp = MonsterXp(vs.level);
                    int cr = 6 + 4 * vs.level;
                    save.credits += cr;
                    AddMsg(vs.displayName + " yok edildi   +" + xp + " XP   +" + cr + " kredi", new Color(0.8f, 1f, 0.6f));
                    AddXp(xp);
                }
                else
                {
                    save.kills++;
                    int xp = 50 + 18 * vs.level;
                    int cr = 30 + 8 * vs.level;
                    save.credits += cr;
                    AddMsg("Düşman pilot " + vs.displayName + " düşürüldü!   +" + xp + " XP   +" + cr + " kredi", new Color(1f, 0.9f, 0.4f));
                    AddXp(xp);
                }
            }
            else if (!vs.isMonster && killer != null)
            {
                var ks = killer as Ship;
                if (ks != null && !ks.isMonster)
                    AddMsg(killer.displayName + " ► " + victim.displayName, Color.Lerp(Nations.Main[ks.nation], Color.white, 0.4f), true);
            }
        }

        int MonsterXp(int lv)
        {
            float f = Mathf.Clamp(1f + 0.12f * (lv - save.level), 0.15f, 1.6f);
            return Mathf.Max(1, Mathf.RoundToInt((22 + 12 * lv) * f));
        }

        public void AddXp(int xp)
        {
            if (save.level >= Progression.MaxLevel) return;
            save.xp += xp;
            bool up = false;
            while (save.level < Progression.MaxLevel && save.xp >= Progression.XpToNext(save.level))
            {
                save.xp -= Progression.XpToNext(save.level);
                save.level++;
                up = true;
            }
            if (save.level >= Progression.MaxLevel) save.xp = 0;
            if (up)
            {
                player.level = save.level;
                player.ApplyStats(PlayerStats(), true);
                Banner("SEVİYE " + save.level + "!", "Gemin güçlendi, canın yenilendi.", new Color(1f, 0.9f, 0.3f));
                Sfx.Play2D(Sfx.LevelUp, 0.7f);
                save.Store();
            }
        }

        public void OnOutpostChanged(Outpost o, int oldOwner, int newOwner)
        {
            if (world == null) return;
            if (newOwner < 0)
            {
                AddMsg(o.label + " tarafsız hale geldi.", Nations.NeutralColor);
            }
            else
            {
                AddMsg(o.label + " artık " + Nations.Names[newOwner] + " kontrolünde!", Nations.Main[newOwner]);
                if (newOwner == save.nation && player != null && player.Alive)
                {
                    Vector3 d = player.transform.position - o.transform.position;
                    d.y = 0f;
                    if (d.magnitude < Outpost.CaptureRadius * 1.3f)
                    {
                        int cr = 80 + 12 * save.level;
                        int xp = 60 + 15 * save.level;
                        save.credits += cr;
                        AddMsg("Üs ele geçirme ödülü   +" + xp + " XP   +" + cr + " kredi", new Color(1f, 0.9f, 0.4f));
                        AddXp(xp);
                    }
                }
                if (world.AllOutpostsOwnedBy(newOwner))
                {
                    if (newOwner == save.nation)
                        Banner("DÜŞMAN KALKANI DÜŞTÜ!", "Tüm üsler bizde. Düşman kalesine saldır!", Nations.Main[newOwner]);
                    else
                        Banner("KALEMİZİN KALKANI DÜŞTÜ!", "Üsleri hemen geri al, kaleyi savun!", new Color(1f, 0.35f, 0.3f));
                }
            }
        }

        public void OnFortressDestroyed(Fortress f, Unit attacker)
        {
            int winner = Nations.Enemy(f.nation);
            if (winner == save.nation)
            {
                save.warWins++;
                int cr = 800 + 80 * save.level;
                int xp = 300 + 50 * save.level;
                save.credits += cr;
                Banner("ZAFER!", Nations.Names[winner] + " ulus savaşını kazandı!   +" + xp + " XP   +" + cr + " kredi", Nations.Main[winner]);
                AddXp(xp);
            }
            else
            {
                save.warLosses++;
                Banner("YENİLGİ", "Kalemiz düştü... Bir sonraki savaşta intikam alacağız.", new Color(1f, 0.35f, 0.3f));
            }
            warResetTimer = 10f;
            save.Store();
        }

        public void OnPlayerHit(float dmg)
        {
            hitFlash = 0.25f;
        }

        public void ShowDamage(Vector3 pos, float amount)
        {
            if (damageTexts.Count > 30) damageTexts.RemoveAt(0);
            damageTexts.Add(new DmgText { pos = pos, amount = amount, time = Time.time });
        }

        public void ShieldHint()
        {
            if (Time.time - shieldHintTime < 4f) return;
            shieldHintTime = Time.time;
            AddMsg("Kale kalkanı aktif! Önce 3 üssün hepsini ele geçirin.", new Color(1f, 0.85f, 0.4f));
        }

        public void SafeZoneHint()
        {
            if (Time.time - safeHintTime < 4f) return;
            safeHintTime = Time.time;
            AddMsg("Ana Şehir güvenli bölgedir, burada ateş edilemez.", new Color(0.7f, 0.95f, 1f));
        }

        public void AddMsg(string text, Color c, bool small = false)
        {
            if (messages.Count > 7) messages.RemoveAt(0);
            messages.Add(new Msg { text = text, color = small ? new Color(c.r, c.g, c.b, 0.85f) : c, time = Time.time });
        }

        public void Banner(string title, string sub, Color c)
        {
            bannerTitle = title;
            bannerSub = sub;
            bannerColor = c;
            bannerTime = Time.time;
        }

        // ------------------------------------------------------------------
        // HANGAR
        // ------------------------------------------------------------------
        public bool InHangarZone { get { return world != null && world.SafeZone; } }

        public void BuyUpgrade(int i)
        {
            int lv = save.GetUpgrade(i);
            if (lv >= Progression.MaxUpgrade) return;
            int cost = Progression.UpgradeCost(lv);
            if (save.credits < cost)
            {
                AddMsg("Yeterli kredin yok! (" + cost + " gerekli)", new Color(1f, 0.5f, 0.4f));
                return;
            }
            save.credits -= cost;
            save.SetUpgrade(i, lv + 1);
            player.ApplyStats(PlayerStats(), true);
            Sfx.Play2D(Sfx.LevelUp, 0.4f);
            AddMsg(Progression.UpgradeNames[i] + " seviye " + (lv + 1) + " oldu!", new Color(0.6f, 1f, 0.6f));
            save.Store();
        }

        public void ChangeClass(int cls)
        {
            if (cls == save.shipClass) return;
            save.shipClass = cls;
            player.ChangeClass(cls, PlayerStats());
            AddMsg("Yeni gemin: " + ShipClasses.All[cls].name, new Color(0.6f, 1f, 0.6f));
            save.Store();
        }
    }
}

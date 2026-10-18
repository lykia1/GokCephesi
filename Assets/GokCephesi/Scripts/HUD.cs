using UnityEngine;

namespace GokCephesi
{
    // ------------------------------------------------------------------
    // ARAYÜZ YARDIMCILARI (her ekran boyutuna göre ölçeklenir; sanal yükseklik 720)
    // ------------------------------------------------------------------
    public static class UI
    {
        public static float Scale = 1f, W = 1280f, H = 720f, SafeLeft, SafeRight, SafeTop;
        static GUIStyle label, button;

        public static void Recalc()
        {
            Scale = Mathf.Max(0.1f, Screen.height / 720f);
            W = Screen.width / Scale;
            H = 720f;
            var sa = Screen.safeArea;
            SafeLeft = sa.xMin / Scale;
            SafeRight = (Screen.width - sa.xMax) / Scale;
            SafeTop = (Screen.height - sa.yMax) / Scale;
        }

        public static void Begin()
        {
            Recalc();
            GUI.matrix = Matrix4x4.Scale(new Vector3(Scale, Scale, 1f));
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label);
                label.richText = false;
                label.clipping = TextClipping.Overflow;
                button = new GUIStyle(GUIStyle.none);
            }
        }

        public static Vector2 ToGui(Vector2 screenPx)
        {
            return new Vector2(screenPx.x / Scale, (Screen.height - screenPx.y) / Scale);
        }

        public static bool WorldToGui(Camera cam, Vector3 world, out Vector2 g)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            g = ToGui(new Vector2(sp.x, sp.y));
            return sp.z > 0.5f;
        }

        public static void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        public static void Frame(Rect r, Color c, float t = 2f)
        {
            Fill(new Rect(r.x, r.y, r.width, t), c);
            Fill(new Rect(r.x, r.yMax - t, r.width, t), c);
            Fill(new Rect(r.x, r.y, t, r.height), c);
            Fill(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        public static void Circle(Vector2 c, float r, Color col)
        {
            var old = GUI.color;
            GUI.color = col;
            GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), Gfx.Circle);
            GUI.color = old;
        }

        public static void Ring(Vector2 c, float r, Color col)
        {
            var old = GUI.color;
            GUI.color = col;
            GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), Gfx.Ring);
            GUI.color = old;
        }

        public static void Bar(Rect r, float pct, Color fg, Color bg)
        {
            Fill(r, bg);
            Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(pct), r.height), fg);
        }

        public static void Text(Rect r, string s, int size, Color c, TextAnchor anchor = TextAnchor.UpperLeft, bool shadow = false, bool bold = false, bool wrap = false)
        {
            if (label == null) return;
            label.fontSize = size;
            label.alignment = anchor;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.wordWrap = wrap;
            if (shadow)
            {
                label.normal.textColor = new Color(0f, 0f, 0f, c.a * 0.8f);
                GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), s, label);
            }
            label.normal.textColor = c;
            GUI.Label(r, s, label);
        }

        public static bool Button(Rect r, string s, int size, Color c, bool enabled = true)
        {
            Color bg = enabled ? new Color(c.r * 0.35f, c.g * 0.35f, c.b * 0.35f, 0.92f) : new Color(0.2f, 0.2f, 0.2f, 0.8f);
            Fill(r, bg);
            Frame(r, enabled ? c : new Color(0.4f, 0.4f, 0.4f), 2f);
            Text(r, s, size, enabled ? Color.white : new Color(0.6f, 0.6f, 0.6f), TextAnchor.MiddleCenter, true, true);
            return GUI.Button(r, GUIContent.none, button) && enabled;
        }
    }

    // ------------------------------------------------------------------
    // TÜM EKRANLAR: ulus seçimi, gemi seçimi, oyun içi göstergeler, hangar, menü
    // ------------------------------------------------------------------
    public class HUD : MonoBehaviour
    {
        int hangarTab;
        bool confirmReset;
        static readonly Color PanelBg = new Color(0.04f, 0.06f, 0.10f, 0.93f);
        static readonly Color Accent = new Color(0.4f, 0.9f, 1f);

        void OnGUI()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            UI.Begin();
            GUI.depth = 0;

            switch (gm.screen)
            {
                case GameScreen.NationSelect: DrawNationSelect(gm); break;
                case GameScreen.ClassSelect: DrawClassSelect(gm); break;
                case GameScreen.Playing: DrawPlaying(gm); break;
            }
        }

        // ==================================================================
        // ULUS SEÇİMİ
        // ==================================================================
        void DrawNationSelect(GameManager gm)
        {
            float W = UI.W, H = UI.H;
            UI.Fill(new Rect(0, 0, W, H), new Color(0.03f, 0.05f, 0.09f, 1f));
            for (int i = 0; i < 30; i++)
            {
                float x = Mathf.Repeat(i * 137.5f + Time.time * (10 + i % 5 * 6), W);
                float y = (i * 71.3f) % H;
                UI.Fill(new Rect(x, y, 2, 2), new Color(1, 1, 1, 0.35f));
            }

            UI.Text(new Rect(0, 30, W, 70), "GÖK CEPHESİ", 60, Accent, TextAnchor.MiddleCenter, true, true);
            UI.Text(new Rect(0, 100, W, 30), "Gökyüzünde iki ulus, tek bir zafer. Tarafını seç.", 20, new Color(0.8f, 0.85f, 0.9f), TextAnchor.MiddleCenter);

            float cw = Mathf.Min(500f, (W - 100f) / 2f);
            float ch = 430f;
            float total = cw * 2f + 40f;
            float x0 = (W - total) / 2f;
            for (int n = 0; n < 2; n++)
            {
                Rect r = new Rect(x0 + n * (cw + 40f), 160f, cw, ch);
                Color main = Nations.Main[n];
                UI.Fill(r, new Color(Nations.Dark[n].r, Nations.Dark[n].g, Nations.Dark[n].b, 0.75f));
                UI.Fill(new Rect(r.x, r.y, r.width, 8), main);
                UI.Frame(r, main, 2f);

                // amblem
                Vector2 c = new Vector2(r.center.x, r.y + 85f);
                UI.Circle(c, 52f, main);
                UI.Circle(c, 40f, Nations.Dark[n]);
                UI.Text(new Rect(c.x - 50, c.y - 25, 100, 50), n == 0 ? "▲" : "◆", 40, main, TextAnchor.MiddleCenter, false, true);

                UI.Text(new Rect(r.x, r.y + 150, r.width, 40), Nations.Names[n], 30, Color.white, TextAnchor.MiddleCenter, true, true);
                UI.Text(new Rect(r.x + 30, r.y + 200, r.width - 60, 140), Nations.Desc[n], 18, new Color(0.88f, 0.9f, 0.95f), TextAnchor.UpperCenter, false, false, true);

                if (UI.Button(new Rect(r.x + 60, r.yMax - 80, r.width - 120, 56), Nations.Short[n] + " SAFLARINA KATIL", 22, main))
                    gm.ChooseNation(n);
            }
            UI.Text(new Rect(0, H - 40, W, 30), "Seçtiğin ulus kalıcıdır. Karşı ulus senin düşmanın olacak.", 16, new Color(1, 1, 1, 0.55f), TextAnchor.MiddleCenter);
        }

        // ==================================================================
        // GEMİ SEÇİMİ
        // ==================================================================
        void DrawClassSelect(GameManager gm)
        {
            float W = UI.W, H = UI.H;
            int n = Mathf.Clamp(gm.pendingNation, 0, 1);
            Color main = Nations.Main[n];
            UI.Fill(new Rect(0, 0, W, H), new Color(0.03f, 0.05f, 0.09f, 1f));
            UI.Text(new Rect(0, 25, W, 50), "GEMİ SINIFINI SEÇ", 40, main, TextAnchor.MiddleCenter, true, true);
            UI.Text(new Rect(0, 75, W, 30), Nations.Names[n] + " • Daha sonra Hangar'dan ücretsiz değiştirebilirsin.", 18, new Color(0.8f, 0.85f, 0.9f), TextAnchor.MiddleCenter);

            int count = ShipClasses.All.Length;
            float gap = 18f;
            float cw = Mathf.Min(290f, (W - 60f - gap * (count - 1)) / count);
            float x0 = (W - (cw * count + gap * (count - 1))) / 2f;
            for (int i = 0; i < count; i++)
            {
                Rect r = new Rect(x0 + i * (cw + gap), 125f, cw, 520f);
                if (DrawClassCard(r, i, main, false))
                    gm.ChooseClass(i);
            }
            if (UI.Button(new Rect(UI.SafeLeft + 20, 20, 170, 46), "◄ Geri", 18, new Color(0.7f, 0.7f, 0.75f)))
                gm.BackToNationSelect();
        }

        bool DrawClassCard(Rect r, int i, Color main, bool active)
        {
            var c = ShipClasses.All[i];
            UI.Fill(r, new Color(0.08f, 0.1f, 0.15f, 0.95f));
            UI.Frame(r, active ? Color.white : main, active ? 3f : 2f);
            UI.Fill(new Rect(r.x, r.y, r.width, 6), main);

            // basit siluet
            Vector2 sc = new Vector2(r.center.x, r.y + 70f);
            float bw = c.bodyScale.x * 9f, bl = c.bodyScale.z * 11f, span = c.wingSpan * 13f;
            UI.Fill(new Rect(sc.x - span / 2f, sc.y - 6f, span, 12f), main);
            UI.Fill(new Rect(sc.x - bw / 2f, sc.y - bl / 2f, bw, bl), new Color(0.8f, 0.82f, 0.86f));
            UI.Fill(new Rect(sc.x - bw / 4f, sc.y - bl / 2f + 6f, bw / 2f, bl * 0.25f), new Color(0.3f, 0.85f, 1f));

            UI.Text(new Rect(r.x, r.y + 120, r.width, 36), c.name, 28, Color.white, TextAnchor.MiddleCenter, true, true);
            UI.Text(new Rect(r.x + 14, r.y + 160, r.width - 28, 70), c.desc, 15, new Color(0.85f, 0.88f, 0.92f), TextAnchor.UpperCenter, false, false, true);

            float y = r.y + 240f;
            StatRow(r, ref y, "Dayanıklılık", c.hp / 330f, main);
            StatRow(r, ref y, "Hız", c.speed / 58f, main);
            StatRow(r, ref y, "Çeviklik", c.turn / 105f, main);
            StatRow(r, ref y, "Ateş gücü", (c.laserDmg / c.fireInterval) / 60f, main);
            StatRow(r, ref y, "Füze", c.missileDmg / 95f, main);
            StatRow(r, ref y, "Kale hasarı", c.structureBonus / 2.5f, main);

            if (active)
            {
                UI.Text(new Rect(r.x, r.yMax - 70, r.width, 50), "KULLANILIYOR", 20, new Color(0.6f, 1f, 0.6f), TextAnchor.MiddleCenter, true, true);
                return false;
            }
            return UI.Button(new Rect(r.x + 20, r.yMax - 70, r.width - 40, 52), "SEÇ", 22, main);
        }

        void StatRow(Rect r, ref float y, string name, float v, Color c)
        {
            UI.Text(new Rect(r.x + 16, y, 120, 22), name, 15, new Color(0.8f, 0.82f, 0.86f));
            UI.Bar(new Rect(r.x + 130, y + 6, r.width - 146, 10), v, c, new Color(1, 1, 1, 0.1f));
            y += 30f;
        }

        // ==================================================================
        // OYUN İÇİ
        // ==================================================================
        void DrawPlaying(GameManager gm)
        {
            var cam = Camera.main;
            var p = gm.player;
            var w = gm.world;
            if (cam == null || p == null || w == null) return;
            float W = UI.W, H = UI.H;

            DrawWorldLabels(cam, w, p);
            DrawUnitOverlays(cam, gm, p);
            DrawTargetReticle(cam, gm, p);
            DrawDamageTexts(cam, gm);

            if (gm.hitFlash > 0f)
                UI.Fill(new Rect(0, 0, W, H), new Color(1f, 0f, 0f, gm.hitFlash * 0.6f));

            DrawPlayerPanel(gm, p, w);
            DrawRadar(gm, p, w);
            if (w.IsWar) DrawWarStatus(gm, w);
            DrawTopButtons(gm, w);
            DrawMessages(gm);
            DrawBanner(gm);

            if (w.OutOfBoundsWarning(p.transform.position))
                UI.Text(new Rect(0, H * 0.42f, W, 40), "⚠ HARİTA SINIRI", 26, new Color(1f, 0.5f, 0.3f), TextAnchor.MiddleCenter, true, true);

            if (p.Alive && !gm.hangarOpen && !gm.menuOpen)
            {
                gm.controls.Draw(p.MissileReady01, p.MissileSecondsLeft, gm.pilot != null && gm.pilot.target != null);
                UI.Text(new Rect(W / 2 - 100, H - 34, 200, 26), "HIZ " + Mathf.RoundToInt(p.speedCur) + "   İRTİFA " + Mathf.RoundToInt(p.transform.position.y), 15, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleCenter, true);
            }

            if (!p.Alive) DrawDeath(gm);
            if (gm.hangarOpen) DrawHangar(gm);
            if (gm.menuOpen) DrawMenu(gm);
        }

        void DrawWorldLabels(Camera cam, World w, Ship p)
        {
            for (int i = 0; i < w.labels.Count; i++)
            {
                var l = w.labels[i];
                float d = Vector3.Distance(cam.transform.position, l.pos);
                if (d > l.maxDist) continue;
                Vector2 g;
                if (!UI.WorldToGui(cam, l.pos, out g)) continue;
                float a = Mathf.Clamp01(1.2f - d / l.maxDist);
                UI.Text(new Rect(g.x - 200, g.y - 15, 400, 30), l.text, 18, new Color(l.color.r, l.color.g, l.color.b, a), TextAnchor.MiddleCenter, true, true);
            }
        }

        void DrawUnitOverlays(Camera cam, GameManager gm, Ship p)
        {
            var all = Unit.All;
            for (int i = 0; i < all.Count; i++)
            {
                var u = all[i];
                if (u == null || u == p || !u.Alive) continue;
                float d = Vector3.Distance(p.transform.position, u.AimPoint);
                bool structure = u.IsStructure;
                if (d > (structure ? 1400f : 520f)) continue;
                Vector2 g;
                if (!UI.WorldToGui(cam, u.AimPoint + Vector3.up * (u.radius + 3f), out g)) continue;

                bool hostile = Unit.Hostile(p.nation, u.nation);
                Color nameC = u.nation < 0 ? new Color(0.85f, 1f, 0.6f) : Color.Lerp(Nations.Main[u.nation], Color.white, 0.35f);
                Color barC = hostile ? new Color(1f, 0.3f, 0.25f) : new Color(0.35f, 1f, 0.45f);

                if (structure)
                {
                    var f = u as Fortress;
                    float bw = 220f;
                    UI.Text(new Rect(g.x - 150, g.y - 46, 300, 24), u.displayName, 18, nameC, TextAnchor.MiddleCenter, true, true);
                    UI.Bar(new Rect(g.x - bw / 2, g.y - 20, bw, 12), u.hp / u.maxHp, barC, new Color(0, 0, 0, 0.6f));
                    if (f != null && f.ShieldUp)
                        UI.Text(new Rect(g.x - 150, g.y - 6, 300, 22), "KALKAN AKTİF", 15, new Color(0.7f, 0.95f, 1f), TextAnchor.MiddleCenter, true, true);
                    continue;
                }

                float alpha = Mathf.Clamp01(1.3f - d / 520f);
                string label = u.displayName + "  Sv." + u.level;
                UI.Text(new Rect(g.x - 120, g.y - 30, 240, 20), label, 14, new Color(nameC.r, nameC.g, nameC.b, alpha), TextAnchor.MiddleCenter, true);
                UI.Bar(new Rect(g.x - 30, g.y - 10, 60, 6), u.hp / u.maxHp, new Color(barC.r, barC.g, barC.b, alpha), new Color(0, 0, 0, 0.5f * alpha));
            }
        }

        void DrawTargetReticle(Camera cam, GameManager gm, Ship p)
        {
            if (gm.pilot == null) return;
            var t = gm.pilot.target;
            if (t == null || !t.Alive) return;
            Vector2 g;
            float d = Vector3.Distance(p.transform.position, t.AimPoint);
            if (UI.WorldToGui(cam, t.AimPoint, out g))
            {
                float s = Mathf.Clamp(1600f / Mathf.Max(d, 1f) * t.radius * 0.6f, 22f, 90f);
                bool inRange = d <= p.stats.range;
                Color c = inRange ? new Color(1f, 0.25f, 0.2f) : new Color(1f, 0.8f, 0.3f);
                float L = s * 0.45f, T = 3f;
                Rect r = new Rect(g.x - s, g.y - s, s * 2f, s * 2f);
                UI.Fill(new Rect(r.x, r.y, L, T), c); UI.Fill(new Rect(r.x, r.y, T, L), c);
                UI.Fill(new Rect(r.xMax - L, r.y, L, T), c); UI.Fill(new Rect(r.xMax - T, r.y, T, L), c);
                UI.Fill(new Rect(r.x, r.yMax - T, L, T), c); UI.Fill(new Rect(r.x, r.yMax - L, T, L), c);
                UI.Fill(new Rect(r.xMax - L, r.yMax - T, L, T), c); UI.Fill(new Rect(r.xMax - T, r.yMax - L, T, L), c);
                UI.Text(new Rect(g.x - 100, r.yMax + 2, 200, 20), Mathf.RoundToInt(d) + " m" + (inRange ? "" : "  (menzil dışı)"), 13, c, TextAnchor.MiddleCenter, true);
            }
            else
            {
                // Hedef arkada: ekran kenarında ok
                Vector3 local = cam.transform.InverseTransformPoint(t.AimPoint);
                Vector2 dir = new Vector2(local.x, -local.y).normalized;
                if (dir.sqrMagnitude < 0.01f) dir = Vector2.down;
                Vector2 c = new Vector2(UI.W / 2f, UI.H / 2f);
                Vector2 pos = c + dir * (UI.H * 0.38f);
                UI.Circle(pos, 16f, new Color(1f, 0.3f, 0.2f, 0.85f));
                UI.Text(new Rect(pos.x - 20, pos.y - 12, 40, 24), "!", 18, Color.white, TextAnchor.MiddleCenter, false, true);
            }

            // Hedef bilgi kutusu
            Rect box = new Rect(UI.W / 2f - 160f, 18f + UI.SafeTop + (gm.world.IsWar ? 92f : 52f), 320f, 46f);
            UI.Fill(box, new Color(0, 0, 0, 0.5f));
            Color nc = t.nation < 0 ? Nations.MonsterColor : Nations.Main[t.nation];
            UI.Text(new Rect(box.x + 10, box.y + 2, box.width - 20, 22), "HEDEF: " + t.displayName + "  Sv." + t.level, 15, nc, TextAnchor.MiddleLeft, true, true);
            UI.Bar(new Rect(box.x + 10, box.y + 28, box.width - 20, 10), t.hp / t.maxHp, new Color(1f, 0.3f, 0.25f), new Color(1, 1, 1, 0.12f));
        }

        void DrawDamageTexts(Camera cam, GameManager gm)
        {
            for (int i = 0; i < gm.damageTexts.Count; i++)
            {
                var dt = gm.damageTexts[i];
                float k = (Time.time - dt.time);
                Vector2 g;
                if (!UI.WorldToGui(cam, dt.pos, out g)) continue;
                g.y -= k * 50f;
                UI.Text(new Rect(g.x - 60, g.y - 12, 120, 24), Mathf.RoundToInt(dt.amount).ToString(), 18, new Color(1f, 0.95f, 0.4f, 1f - k), TextAnchor.MiddleCenter, true, true);
            }
        }

        void DrawPlayerPanel(GameManager gm, Ship p, World w)
        {
            var s = gm.save;
            float x = UI.SafeLeft + 14f, y = 12f + UI.SafeTop;
            Rect r = new Rect(x, y, 330f, 128f);
            Color main = Nations.Main[s.nation];
            UI.Fill(r, new Color(0, 0, 0, 0.5f));
            UI.Fill(new Rect(r.x, r.y, 5f, r.height), main);

            UI.Text(new Rect(x + 14, y + 4, 310, 24), Nations.Short[s.nation] + " • " + ShipClasses.All[s.shipClass].name + " • Sv. " + s.level, 17, Color.white, TextAnchor.MiddleLeft, true, true);

            UI.Text(new Rect(x + 14, y + 30, 40, 18), "CAN", 13, new Color(0.7f, 1f, 0.7f));
            UI.Bar(new Rect(x + 56, y + 33, 200, 13), p.hp / p.maxHp, new Color(0.3f, 0.95f, 0.4f), new Color(1, 1, 1, 0.1f));
            UI.Text(new Rect(x + 260, y + 30, 70, 18), Mathf.CeilToInt(p.hp) + "/" + Mathf.CeilToInt(p.maxHp), 12, Color.white);

            bool max = s.level >= Progression.MaxLevel;
            float xpPct = max ? 1f : s.xp / (float)Progression.XpToNext(s.level);
            UI.Text(new Rect(x + 14, y + 52, 40, 18), "XP", 13, new Color(0.7f, 0.85f, 1f));
            UI.Bar(new Rect(x + 56, y + 55, 200, 9), xpPct, new Color(0.35f, 0.7f, 1f), new Color(1, 1, 1, 0.1f));
            UI.Text(new Rect(x + 260, y + 51, 70, 18), max ? "MAKS" : Mathf.FloorToInt(xpPct * 100f) + "%", 12, Color.white);

            UI.Text(new Rect(x + 14, y + 74, 310, 22), "Kredi: " + s.credits, 15, new Color(1f, 0.9f, 0.4f), TextAnchor.MiddleLeft, true);
            UI.Text(new Rect(x + 14, y + 98, 310, 22), w.def.name, 15, Accent, TextAnchor.MiddleLeft, true, true);
        }

        void DrawRadar(GameManager gm, Ship p, World w)
        {
            const float R = 72f, Range = 750f;
            Vector2 c = new Vector2(UI.W - UI.SafeRight - R - 18f, R + 18f + UI.SafeTop);
            UI.Circle(c, R + 4, new Color(0, 0, 0, 0.55f));
            UI.Ring(c, R + 4, new Color(0.5f, 0.9f, 1f, 0.6f));
            UI.Ring(c, R * 0.5f, new Color(0.5f, 0.9f, 1f, 0.2f));
            Quaternion inv = Quaternion.Euler(0, -p.yaw, 0);

            for (int i = 0; i < w.gates.Count; i++)
                RadarDot(c, R, Range, inv, p, w.gates[i].transform.position, Accent, 5f, true);
            for (int i = 0; i < w.outposts.Count; i++)
                RadarDot(c, R, Range, inv, p, w.outposts[i].transform.position, w.outposts[i].OwnerColor, 6f, true);

            var all = Unit.All;
            for (int i = 0; i < all.Count; i++)
            {
                var u = all[i];
                if (u == null || u == p || !u.Alive) continue;
                Color col = u.nation < 0 ? new Color(0.9f, 0.9f, 0.3f) : (u.nation == p.nation ? new Color(0.3f, 1f, 0.4f) : new Color(1f, 0.25f, 0.2f));
                RadarDot(c, R, Range, inv, p, u.transform.position, col, u.IsStructure ? 7f : 3.5f, u.IsStructure);
            }
            UI.Fill(new Rect(c.x - 1.5f, c.y - 7, 3, 12), Color.white);
            UI.Fill(new Rect(c.x - 5, c.y - 1, 10, 3), Color.white);
            UI.Text(new Rect(c.x - 20, c.y - R - 2, 40, 16), "K", 12, new Color(1, 1, 1, 0.6f), TextAnchor.MiddleCenter);
        }

        void RadarDot(Vector2 c, float R, float range, Quaternion inv, Ship p, Vector3 wp, Color col, float size, bool clampEdge)
        {
            Vector3 rel = inv * (wp - p.transform.position);
            Vector2 v = new Vector2(rel.x, -rel.z) / range * R;
            if (v.magnitude > R)
            {
                if (!clampEdge) return;
                v = v.normalized * R;
            }
            UI.Circle(c + v, size, col);
        }

        void DrawWarStatus(GameManager gm, World w)
        {
            float bw = 420f;
            float x = UI.W / 2f - bw / 2f, y = 12f + UI.SafeTop;
            UI.Fill(new Rect(x, y, bw, 76f), new Color(0, 0, 0, 0.5f));

            int me = gm.save.nation, en = Nations.Enemy(me);
            var myF = w.fortresses[me];
            var enF = w.fortresses[en];
            if (myF != null)
            {
                UI.Text(new Rect(x + 8, y + 4, 140, 18), "Kalemiz" + (myF.ShieldUp ? " (kalkan)" : " (AÇIK!)"), 13, Nations.Main[me], TextAnchor.MiddleLeft, true, true);
                UI.Bar(new Rect(x + 8, y + 24, 130, 8), myF.hp / myF.maxHp, Nations.Main[me], new Color(1, 1, 1, 0.12f));
            }
            if (enF != null)
            {
                UI.Text(new Rect(x + bw - 148, y + 4, 140, 18), "Düşman" + (enF.ShieldUp ? " (kalkan)" : " (AÇIK!)"), 13, Nations.Main[en], TextAnchor.MiddleRight, true, true);
                UI.Bar(new Rect(x + bw - 138, y + 24, 130, 8), enF.hp / enF.maxHp, Nations.Main[en], new Color(1, 1, 1, 0.12f));
            }

            float ox = x + 150f;
            for (int i = 0; i < w.outposts.Count; i++)
            {
                var o = w.outposts[i];
                Rect r = new Rect(ox + i * 42f, y + 6f, 34f, 34f);
                UI.Fill(r, new Color(0, 0, 0, 0.5f));
                UI.Fill(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6), o.OwnerColor);
                // ele geçirme ilerlemesi
                float prog = o.progress;
                Color pc = prog < 0f ? Nations.Main[0] : Nations.Main[1];
                UI.Bar(new Rect(r.x, r.yMax + 3, r.width, 5), Mathf.Abs(prog), pc, new Color(1, 1, 1, 0.15f));
                string letter = i == 0 ? "A" : i == 1 ? "M" : "D";
                UI.Text(r, letter, 18, Color.white, TextAnchor.MiddleCenter, true, true);
                if (o.Contested) UI.Frame(r, Color.white, 2f);
            }
            UI.Text(new Rect(x, y + 52, bw, 20), "Üsler: A=Alfa  M=Merkez  D=Delta  • 3 üs = düşman kalkanı düşer", 12, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleCenter);
        }

        void DrawTopButtons(GameManager gm, World w)
        {
            float bx = UI.W - UI.SafeRight - 190f - 120f;
            float y = 14f + UI.SafeTop;
            if (gm.hangarOpen || gm.menuOpen || !gm.player.Alive) return;
            if (UI.Button(new Rect(bx, y, 110, 40), "MENÜ", 16, new Color(0.7f, 0.75f, 0.8f)))
            {
                gm.menuOpen = true;
                confirmReset = false;
            }
            if (gm.InHangarZone)
            {
                if (UI.Button(new Rect(bx, y + 48, 110, 40), "HANGAR", 16, new Color(1f, 0.85f, 0.3f)))
                {
                    gm.hangarOpen = true;
                    hangarTab = 0;
                }
            }
            else if (gm.CanReturnToCity)
            {
                if (UI.Button(new Rect(bx, y + 48, 110, 40), "ŞEHRE DÖN", 14, new Color(0.5f, 0.9f, 1f)))
                    gm.ReturnToCity();
            }
        }

        void DrawMessages(GameManager gm)
        {
            float y = 150f + UI.SafeTop;
            float x = UI.SafeLeft + 14f;
            for (int i = 0; i < gm.messages.Count; i++)
            {
                var m = gm.messages[i];
                float age = Time.time - m.time;
                float a = Mathf.Clamp01(6f - age) * m.color.a;
                UI.Text(new Rect(x, y, 560, 22), m.text, 15, new Color(m.color.r, m.color.g, m.color.b, a), TextAnchor.MiddleLeft, true);
                y += 22f;
            }
        }

        void DrawBanner(GameManager gm)
        {
            float age = Time.time - gm.bannerTime;
            if (age > 4f || string.IsNullOrEmpty(gm.bannerTitle)) return;
            float a = Mathf.Clamp01(age * 4f) * Mathf.Clamp01(4f - age);
            float y = UI.H * 0.24f;
            UI.Fill(new Rect(0, y - 8, UI.W, 92), new Color(0, 0, 0, 0.45f * a));
            UI.Text(new Rect(0, y, UI.W, 50), gm.bannerTitle, 40, new Color(gm.bannerColor.r, gm.bannerColor.g, gm.bannerColor.b, a), TextAnchor.MiddleCenter, true, true);
            UI.Text(new Rect(0, y + 50, UI.W, 28), gm.bannerSub, 18, new Color(1, 1, 1, a * 0.9f), TextAnchor.MiddleCenter, true);
        }

        void DrawDeath(GameManager gm)
        {
            float W = UI.W, H = UI.H;
            UI.Fill(new Rect(0, 0, W, H), new Color(0.15f, 0f, 0f, 0.55f));
            UI.Text(new Rect(0, H * 0.32f, W, 60), "GEMİN DÜŞÜRÜLDÜ", 46, new Color(1f, 0.4f, 0.35f), TextAnchor.MiddleCenter, true, true);
            UI.Text(new Rect(0, H * 0.32f + 64, W, 30), "Yeniden doğuş: " + Mathf.CeilToInt(Mathf.Max(0f, gm.respawnTimer)) + " sn", 22, Color.white, TextAnchor.MiddleCenter, true);
            if (gm.world != null && gm.world.def.id != Maps.Home)
            {
                if (UI.Button(new Rect(W / 2 - 140, H * 0.32f + 120, 280, 54), "ŞEHİRDE DOĞ", 20, new Color(0.5f, 0.9f, 1f)))
                    gm.RespawnPlayer(true);
            }
        }

        // ==================================================================
        // HANGAR
        // ==================================================================
        void DrawHangar(GameManager gm)
        {
            float W = UI.W, H = UI.H;
            UI.Fill(new Rect(0, 0, W, H), new Color(0, 0, 0, 0.55f));
            float pw = Mathf.Min(1100f, W - 40f), ph = 640f;
            Rect r = new Rect((W - pw) / 2f, (H - ph) / 2f, pw, ph);
            Color main = Nations.Main[gm.save.nation];
            UI.Fill(r, PanelBg);
            UI.Frame(r, main, 2f);
            UI.Text(new Rect(r.x + 24, r.y + 14, 400, 40), "HANGAR", 32, main, TextAnchor.MiddleLeft, true, true);
            UI.Text(new Rect(r.xMax - 324, r.y + 14, 300, 40), "Kredi: " + gm.save.credits, 22, new Color(1f, 0.9f, 0.4f), TextAnchor.MiddleRight, true, true);

            if (UI.Button(new Rect(r.x + 24, r.y + 64, 220, 42), "Geliştirmeler", 17, hangarTab == 0 ? main : new Color(0.5f, 0.5f, 0.55f))) hangarTab = 0;
            if (UI.Button(new Rect(r.x + 254, r.y + 64, 220, 42), "Gemi Değiştir", 17, hangarTab == 1 ? main : new Color(0.5f, 0.5f, 0.55f))) hangarTab = 1;

            if (hangarTab == 0)
            {
                float y = r.y + 130f;
                for (int i = 0; i < 4; i++)
                {
                    int lv = gm.save.GetUpgrade(i);
                    Rect row = new Rect(r.x + 24, y, r.width - 48, 98);
                    UI.Fill(row, new Color(1, 1, 1, 0.04f));
                    UI.Text(new Rect(row.x + 16, row.y + 8, 300, 30), Progression.UpgradeNames[i], 24, Color.white, TextAnchor.MiddleLeft, true, true);
                    UI.Text(new Rect(row.x + 16, row.y + 40, 420, 24), Progression.UpgradeDesc[i], 15, new Color(0.8f, 0.83f, 0.88f));
                    for (int k = 0; k < Progression.MaxUpgrade; k++)
                        UI.Fill(new Rect(row.x + 16 + k * 26, row.y + 70, 22, 12), k < lv ? main : new Color(1, 1, 1, 0.12f));

                    bool maxed = lv >= Progression.MaxUpgrade;
                    int cost = Progression.UpgradeCost(lv);
                    bool afford = gm.save.credits >= cost;
                    string label = maxed ? "MAKSİMUM" : "YÜKSELT  •  " + cost;
                    if (UI.Button(new Rect(row.xMax - 260, row.y + 22, 240, 54), label, 18, afford ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.45f, 0.4f), !maxed))
                        gm.BuyUpgrade(i);
                    y += 108f;
                }
            }
            else
            {
                int count = ShipClasses.All.Length;
                float gap = 14f;
                float cw = (r.width - 48f - gap * (count - 1)) / count;
                for (int i = 0; i < count; i++)
                {
                    Rect cr = new Rect(r.x + 24 + i * (cw + gap), r.y + 120f, cw, 440f);
                    if (DrawClassCard(cr, i, main, gm.save.shipClass == i))
                        gm.ChangeClass(i);
                }
            }

            if (UI.Button(new Rect(r.xMax - 184, r.yMax - 62, 160, 46), "KAPAT", 18, new Color(0.75f, 0.78f, 0.82f)))
                gm.hangarOpen = false;
        }

        // ==================================================================
        // MENÜ
        // ==================================================================
        void DrawMenu(GameManager gm)
        {
            float W = UI.W, H = UI.H;
            UI.Fill(new Rect(0, 0, W, H), new Color(0, 0, 0, 0.55f));
            float pw = Mathf.Min(760f, W - 40f), ph = 600f;
            Rect r = new Rect((W - pw) / 2f, (H - ph) / 2f, pw, ph);
            var s = gm.save;
            Color main = Nations.Main[s.nation];
            UI.Fill(r, PanelBg);
            UI.Frame(r, main, 2f);
            UI.Text(new Rect(r.x + 24, r.y + 14, 400, 40), "MENÜ", 32, main, TextAnchor.MiddleLeft, true, true);

            float y = r.y + 66f;
            string stats =
                "Ulus: " + Nations.Names[s.nation] + "\n" +
                "Seviye: " + s.level + "    Kredi: " + s.credits + "\n" +
                "Düşürülen düşman: " + s.kills + "    Düşülme: " + s.deaths + "\n" +
                "Kazanılan ulus savaşı: " + s.warWins + "    Kaybedilen: " + s.warLosses;
            UI.Text(new Rect(r.x + 24, y, r.width - 48, 110), stats, 17, Color.white, TextAnchor.UpperLeft, false, false, true);
            y += 118f;

            if (UI.Button(new Rect(r.x + 24, y, 320, 46), "Ters dikey kontrol: " + (s.invertY ? "AÇIK" : "KAPALI"), 16, Accent))
            {
                s.invertY = !s.invertY;
                s.Store();
            }
            if (UI.Button(new Rect(r.x + 360, y, 220, 46), "Ses: " + (s.sound ? "AÇIK" : "KAPALI"), 16, Accent))
            {
                s.sound = !s.sound;
                Sfx.Enabled = s.sound;
                s.Store();
            }
            y += 62f;

            string help =
                "KONTROLLER\n" +
                "• Ekranın sol yarısına dokunup sürükle: gemiyi yönlendir.\n" +
                "• ATEŞ: basılı tut, lazer otomatik olarak kilitli hedefe yönelir.\n" +
                "• FÜZE: hedefe güdümlü füze (bekleme süresi var).  HIZ / FREN: hızlan, yavaşla.\n" +
                "• HEDEF: sıradaki düşmana kilitlen.\n" +
                "• Bilgisayarda: WASD/oklar, Space ateş, E füze, Shift hız, Q fren, Tab hedef, Esc menü.\n\n" +
                "ULUS SAVAŞI: Sınır Cephesi'nde 3 üssün hepsini ele geçir → düşman kalesinin kalkanı düşer → kaleyi yık!";
            UI.Text(new Rect(r.x + 24, y, r.width - 48, 220), help, 15, new Color(0.85f, 0.88f, 0.92f), TextAnchor.UpperLeft, false, false, true);

            if (!confirmReset)
            {
                if (UI.Button(new Rect(r.x + 24, r.yMax - 62, 260, 46), "Kaydı sil, baştan başla", 15, new Color(1f, 0.4f, 0.35f)))
                    confirmReset = true;
            }
            else
            {
                UI.Text(new Rect(r.x + 24, r.yMax - 92, 400, 24), "Tüm ilerleme silinecek. Emin misin?", 15, new Color(1f, 0.6f, 0.5f));
                if (UI.Button(new Rect(r.x + 24, r.yMax - 62, 130, 46), "EVET, SİL", 15, new Color(1f, 0.3f, 0.25f)))
                {
                    confirmReset = false;
                    gm.ResetProgress();
                    return;
                }
                if (UI.Button(new Rect(r.x + 164, r.yMax - 62, 120, 46), "Vazgeç", 15, new Color(0.7f, 0.7f, 0.75f)))
                    confirmReset = false;
            }

            if (UI.Button(new Rect(r.xMax - 184, r.yMax - 62, 160, 46), "KAPAT", 18, new Color(0.75f, 0.78f, 0.82f)))
                gm.menuOpen = false;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace GokCephesi
{
    /// <summary>
    /// Ekrandaki sanal joystick ve butonlar. Çoklu dokunmayı destekler:
    /// sol başparmak ile yön, sağ başparmak ile ateş/füze/hız.
    /// Bilgisayarda klavye (WASD, Space, E, Shift, Q, Tab) ve fare de çalışır.
    /// </summary>
    public class MobileControls
    {
        public Vector2 stick;
        public bool fire, boost, brake, missileDown, targetDown;
        public bool blocked;

        const int NoPointer = int.MinValue;
        public const float StickRadius = 85f;

        int stickId = NoPointer;
        Vector2 stickCenter, stickPos;
        readonly HashSet<int> prevIds = new HashSet<int>();
        readonly HashSet<int> curIds = new HashSet<int>();
        bool prevMissile, prevTarget;
        bool touchFire, touchBoost, touchBrake, touchMissile, touchTarget;

        // Buton yerleşimi (sanal 720 piksel yüksekliğe göre)
        public Vector2 FireC { get { return new Vector2(UI.W - UI.SafeRight - 150f, UI.H - 150f); } }
        public Vector2 MissileC { get { return new Vector2(UI.W - UI.SafeRight - 305f, UI.H - 95f); } }
        public Vector2 BoostC { get { return new Vector2(UI.W - UI.SafeRight - 95f, UI.H - 335f); } }
        public Vector2 BrakeC { get { return new Vector2(UI.W - UI.SafeRight - 220f, UI.H - 305f); } }
        public Vector2 TargetC { get { return new Vector2(UI.W - UI.SafeRight - 335f, UI.H - 235f); } }
        public const float FireR = 82f, MissileR = 56f, BoostR = 50f, BrakeR = 44f, TargetR = 44f;

        Vector2 DefaultStickCenter { get { return new Vector2(UI.SafeLeft + 175f, UI.H - 175f); } }

        static bool In(Vector2 p, Vector2 c, float r)
        {
            float rr = r * 1.15f;
            return (p - c).sqrMagnitude <= rr * rr;
        }

        public void Update()
        {
            var ptrs = InputBridge.Pointers();
            curIds.Clear();
            touchFire = touchBoost = touchBrake = touchMissile = touchTarget = false;
            bool stickAlive = false;

            for (int i = 0; i < ptrs.Count; i++)
            {
                var p = ptrs[i];
                Vector2 g = UI.ToGui(p.pos);
                curIds.Add(p.id);
                if (blocked) continue;

                if (p.id == stickId)
                {
                    stickAlive = true;
                    stickPos = g;
                    continue;
                }
                if (In(g, FireC, FireR)) touchFire = true;
                else if (In(g, MissileC, MissileR)) touchMissile = true;
                else if (In(g, BoostC, BoostR)) touchBoost = true;
                else if (In(g, BrakeC, BrakeR)) touchBrake = true;
                else if (In(g, TargetC, TargetR)) touchTarget = true;
                else if (stickId == NoPointer && !prevIds.Contains(p.id) && g.x < UI.W * 0.5f && g.y > UI.H * 0.3f)
                {
                    stickId = p.id;
                    stickCenter = g;
                    stickPos = g;
                    stickAlive = true;
                }
            }
            if (!stickAlive) stickId = NoPointer;

            prevIds.Clear();
            foreach (var id in curIds) prevIds.Add(id);

            Vector2 s = Vector2.zero;
            if (stickId != NoPointer)
            {
                Vector2 d = stickPos - stickCenter;
                s = new Vector2(d.x, -d.y) / StickRadius;
                if (s.magnitude > 1f) s.Normalize();
                if (s.magnitude < 0.08f) s = Vector2.zero;
            }

            if (!blocked)
            {
                if (InputBridge.Key(GameKey.Left)) s.x -= 1f;
                if (InputBridge.Key(GameKey.Right)) s.x += 1f;
                if (InputBridge.Key(GameKey.Up)) s.y += 1f;
                if (InputBridge.Key(GameKey.Down)) s.y -= 1f;
            }
            s.x = Mathf.Clamp(s.x, -1f, 1f);
            s.y = Mathf.Clamp(s.y, -1f, 1f);
            stick = s;

            bool kb = !blocked;
            fire = touchFire || (kb && InputBridge.Key(GameKey.Fire));
            boost = touchBoost || (kb && InputBridge.Key(GameKey.Boost));
            brake = touchBrake || (kb && InputBridge.Key(GameKey.Brake));

            bool m = touchMissile || (kb && InputBridge.Key(GameKey.Missile));
            missileDown = m && !prevMissile;
            prevMissile = m;

            bool t = touchTarget || (kb && InputBridge.Key(GameKey.Target));
            targetDown = t && !prevTarget;
            prevTarget = t;
        }

        public void Reset()
        {
            stickId = NoPointer;
            stick = Vector2.zero;
            fire = boost = brake = missileDown = targetDown = false;
        }

        // ------------------------------------------------------------------
        public void Draw(float missileReady, float missileSecondsLeft, bool hasTarget)
        {
            // Joystick
            Vector2 c = stickId != NoPointer ? stickCenter : DefaultStickCenter;
            float alpha = stickId != NoPointer ? 0.55f : 0.25f;
            UI.Circle(c, StickRadius + 10f, new Color(0f, 0f, 0f, alpha * 0.6f));
            UI.Ring(c, StickRadius + 10f, new Color(1f, 1f, 1f, alpha));
            Vector2 knob = c;
            if (stickId != NoPointer)
            {
                Vector2 d = stickPos - stickCenter;
                if (d.magnitude > StickRadius) d = d.normalized * StickRadius;
                knob = c + d;
            }
            UI.Circle(knob, 38f, new Color(1f, 1f, 1f, alpha + 0.15f));
            if (stickId == NoPointer)
                UI.Text(new Rect(c.x - 100f, c.y + StickRadius + 12f, 200f, 24f), "Yön için sürükle", 15, new Color(1, 1, 1, 0.5f), TextAnchor.MiddleCenter);

            // Butonlar
            DrawButton(FireC, FireR, "ATEŞ", touchFire || fire, new Color(1f, 0.35f, 0.25f), 1f);
            DrawButton(MissileC, MissileR, missileReady >= 1f ? "FÜZE" : Mathf.CeilToInt(missileSecondsLeft).ToString(), touchMissile, new Color(1f, 0.75f, 0.2f), missileReady);
            DrawButton(BoostC, BoostR, "HIZ", touchBoost || boost, new Color(0.3f, 0.8f, 1f), 1f);
            DrawButton(BrakeC, BrakeR, "FREN", touchBrake || brake, new Color(0.7f, 0.7f, 0.75f), 1f);
            DrawButton(TargetC, TargetR, "HEDEF", touchTarget, hasTarget ? new Color(1f, 0.3f, 0.3f) : new Color(0.75f, 0.75f, 0.75f), 1f);
        }

        void DrawButton(Vector2 c, float r, string label, bool pressed, Color col, float fill)
        {
            float a = pressed ? 0.75f : 0.4f;
            UI.Circle(c, r, new Color(0f, 0f, 0f, 0.35f));
            float rf = r * Mathf.Clamp01(fill);
            UI.Circle(c, Mathf.Max(4f, rf), new Color(col.r, col.g, col.b, a * (fill >= 1f ? 1f : 0.6f)));
            UI.Ring(c, r, new Color(1f, 1f, 1f, pressed ? 0.95f : 0.6f));
            UI.Text(new Rect(c.x - r, c.y - 14f, r * 2f, 28f), label, r > 60f ? 24 : 16, Color.white, TextAnchor.MiddleCenter, true);
        }
    }
}

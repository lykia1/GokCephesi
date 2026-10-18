using System.Collections.Generic;
using UnityEngine;

namespace GokCephesi
{
    /// <summary>Hasar alabilen her şey: gemiler, yaratıklar, kaleler.</summary>
    public abstract class Unit : MonoBehaviour
    {
        public static readonly List<Unit> All = new List<Unit>();

        public int nation;            // 0, 1 veya -1 (yaratık)
        public float hp;
        public float maxHp;
        public float radius = 4f;
        public int level = 1;
        public string displayName = "";

        public bool Alive { get { return hp > 0f; } }
        public virtual bool IsStructure { get { return false; } }
        public virtual Vector3 Velocity { get { return Vector3.zero; } }
        public virtual Vector3 AimPoint { get { return transform.position; } }

        protected virtual void OnEnable() { All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }

        public static bool Hostile(int a, int b)
        {
            return a != b;
        }

        public abstract void TakeDamage(float dmg, Unit attacker);

        /// <summary>Verilen noktaya en yakın düşman birimi.</summary>
        public static Unit NearestHostile(int myNation, Vector3 from, float maxDist, bool includeMonsters, bool includeStructures, Unit except = null)
        {
            Unit best = null;
            float bestD = maxDist * maxDist;
            for (int i = 0; i < All.Count; i++)
            {
                var u = All[i];
                if (u == null || u == except || !u.Alive) continue;
                if (!Hostile(myNation, u.nation)) continue;
                if (!includeMonsters && u.nation < 0) continue;
                if (!includeStructures && u.IsStructure) continue;
                float d = (u.AimPoint - from).sqrMagnitude;
                if (d < bestD) { bestD = d; best = u; }
            }
            return best;
        }
    }
}

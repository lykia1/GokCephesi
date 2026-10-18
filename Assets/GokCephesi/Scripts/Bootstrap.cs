using UnityEngine;

namespace GokCephesi
{
    /// <summary>
    /// Oyunu otomatik başlatır. Sahneye hiçbir şey eklemen gerekmez:
    /// Unity'de Play'e bastığın an bu kod çalışır ve tüm oyunu kurar.
    /// </summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (Object.FindFirstObjectByType<GameManager>() != null) return;
            var go = new GameObject("GokCephesi_Oyun");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<GameManager>();
        }
    }
}

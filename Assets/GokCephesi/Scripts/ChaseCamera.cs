using UnityEngine;

namespace GokCephesi
{
    /// <summary>Oyuncu gemisini arkadan ve yukarıdan yumuşakça takip eden kamera.</summary>
    [RequireComponent(typeof(Camera))]
    public class ChaseCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 7f, -24f);
        Camera cam;

        void Awake()
        {
            cam = GetComponent<Camera>();
        }

        public void Snap()
        {
            if (target == null) return;
            transform.position = target.position + target.rotation * offset;
            transform.rotation = Quaternion.LookRotation(target.position + target.forward * 25f - transform.position);
        }

        void LateUpdate()
        {
            var gm = GameManager.I;
            if (target == null || gm == null || gm.screen != GameScreen.Playing)
            {
                transform.Rotate(0f, 4f * Time.deltaTime, 0f, Space.World);
                return;
            }
            float dt = Time.deltaTime;
            Vector3 desired = target.position + target.rotation * offset;
            if (desired.y < 3f) desired.y = 3f;
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-7f * dt));

            Vector3 look = target.position + target.forward * 25f + Vector3.up * 2f;
            var want = Quaternion.LookRotation(look - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, want, 1f - Mathf.Exp(-12f * dt));

            var ship = GameManager.PlayerUnit;
            if (cam != null && ship != null)
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, ship.boosting ? 72f : 62f, dt * 3f);

            if (gm.hitFlash > 0f)
                transform.position += Random.insideUnitSphere * gm.hitFlash * 1.5f;
        }
    }
}

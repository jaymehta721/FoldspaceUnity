using Foldspace.Environment;
using UnityEngine;

namespace Foldspace.Feedback
{
    /// <summary>
    /// Frames the whole lens, leans a little toward the ship, and adds trauma shake, kicks and zoom punches.
    /// Runs on unscaled time so it keeps moving through hitstop.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        const float Depth = -10f;

        [Tooltip("How far the camera drifts toward the ship, as a fraction of its distance from the center.")]
        [SerializeField] float lean = 0.05f;
        [SerializeField] float maxShake = 0.3f;
        [SerializeField] float maxShakeAngle = 2f;
        [SerializeField] float traumaDecay = 1.5f;

        GameContext context;
        Camera cam;
        float trauma;
        float zoom;
        float seed;
        Vector2 kick;
        Vector2 leanOffset;
        Vector2 zoomFocus;

        public void Init(GameContext ctx)
        {
            context = ctx;
            cam = GetComponent<Camera>();
            seed = Random.value * 100f;
        }

        /// <summary>Adds trauma (0 to 1). Shake grows with trauma squared, so small hits stay small.</summary>
        public void Shake(float amount) => trauma = Mathf.Clamp01(trauma + amount);

        public void Kick(Vector2 impulse) => kick += impulse;

        public void Zoom(float amount, Vector2 focus)
        {
            if (amount <= zoom) return;
            zoom = amount;
            zoomFocus = focus;
        }

        public void Calm()
        {
            trauma = 0f;
            zoom = 0f;
            kick = Vector2.zero;
        }

        /// <summary>Orthographic size that fits the lens and its rim on screen at <paramref name="aspect"/>.</summary>
        public static float FramingSize(float arenaRadius, float screenFill, float aspect)
        {
            float extent = (arenaRadius + Arena.OuterExtent) / Mathf.Max(0.1f, screenFill);
            return extent * Mathf.Max(1f, 1f / Mathf.Max(0.01f, aspect));
        }

        void LateUpdate()
        {
            if (context == null) return;
            float dt = Time.unscaledDeltaTime;
            trauma = Mathf.Max(0f, trauma - traumaDecay * dt);
            kick *= Mathf.Exp(-9f * dt);
            zoom *= Mathf.Exp(-5f * dt);

            var player = context.Player;
            Vector2 target = player != null && player.Alive && context.IsPlaying ? player.Position * lean : Vector2.zero;
            leanOffset = Vector2.Lerp(leanOffset, target, 1f - Mathf.Exp(-2.5f * dt));

            float shake = trauma * trauma;
            float t = Time.unscaledTime * 24f;
            var jitter = new Vector2(Mathf.PerlinNoise(seed, t) - 0.5f, Mathf.PerlinNoise(seed + 7.3f, t) - 0.5f) * (2f * maxShake * shake);
            float roll = (Mathf.PerlinNoise(seed + 13.1f, t) - 0.5f) * 2f * maxShakeAngle * shake;

            var arena = context.Config.arena;
            cam.orthographicSize = FramingSize(arena.radius, arena.screenFill, cam.aspect) * (1f - zoom);

            Vector2 focus = Vector2.Lerp(leanOffset, zoomFocus, Mathf.Clamp01(zoom * 2.5f));
            Vector2 position = focus + jitter + kick;
            transform.SetPositionAndRotation(new Vector3(position.x, position.y, Depth), Quaternion.Euler(0f, 0f, roll));
        }
    }
}

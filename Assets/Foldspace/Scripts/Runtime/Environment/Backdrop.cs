using System;
using Foldspace.Rendering;
using Foldspace.Utilities;
using UnityEngine;

namespace Foldspace.Environment
{
    /// <summary>
    /// Deep space behind the lens: an animated nebula, parallax star layers, drifting debris and the odd shooting star.
    /// The stars and debris are static children of the prefab; this component only animates them.
    /// </summary>
    public class Backdrop : MonoBehaviour
    {
        /// <summary>Half-size of the square the star field wraps around in.</summary>
        public const float Field = 11f;

        [Serializable]
        public struct Star
        {
            public SpriteRenderer sprite;
            public Vector2 home;
            [Tooltip("0 = infinitely far (moves with the camera), 1 = on the play plane.")]
            public float depth;
            public float alpha;
            public float twinkleSpeed;
            public float phase;
        }

        [Serializable]
        public struct Drifter
        {
            public Transform transform;
            [Tooltip("Position as a fraction of the screen's half-extents.")]
            public Vector2 anchor;
            public float depth;
            public float spin;
            public float phase;
        }

        [SerializeField] Star[] stars = Array.Empty<Star>();
        [SerializeField] Drifter[] drifters = Array.Empty<Drifter>();
        [SerializeField] Vector2 starDrift = new Vector2(-0.18f, -0.07f);
        [SerializeField] Transform nebula;
        [SerializeField] float nebulaDistance = 40f;

        [Header("Shooting stars")]
        [SerializeField] SpriteRenderer comet;
        [SerializeField] float firstCometDelay = 3f;
        [SerializeField] Vector2 cometInterval = new Vector2(4f, 10f);

        readonly System.Random rng = new System.Random(11);
        Camera cam;
        Vector2 cometVelocity;
        float cometAge;
        float cometLife;
        float nextComet;

        public void Init(Camera worldCamera)
        {
            cam = worldCamera;
            comet.enabled = false;
            nextComet = Time.unscaledTime + firstCometDelay;
        }

        float Next() => (float)rng.NextDouble();

        void LateUpdate()
        {
            if (cam == null) return;
            float dt = Time.unscaledDeltaTime;
            float t = Time.unscaledTime;
            var camTransform = cam.transform;
            Vector2 camPos = camTransform.position;

            if (nebula != null)
                nebula.SetPositionAndRotation(camTransform.position + camTransform.forward * nebulaDistance, camTransform.rotation);

            for (int i = 0; i < stars.Length; i++)
            {
                ref var s = ref stars[i];
                s.home += starDrift * (s.depth * dt);
                if (s.home.x < -Field) s.home.x += Field * 2f;
                if (s.home.y < -Field) s.home.y += Field * 2f;
                s.sprite.transform.position = s.home + camPos * (1f - s.depth);
                s.sprite.color = s.sprite.color.WithAlpha(s.alpha * (0.72f + 0.28f * Mathf.Sin(t * s.twinkleSpeed + s.phase)));
            }

            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            foreach (var d in drifters)
            {
                var bob = new Vector2(Mathf.Sin(t * 0.13f + d.phase), Mathf.Cos(t * 0.11f + d.phase)) * 0.25f;
                d.transform.position = new Vector2(d.anchor.x * halfWidth, d.anchor.y * halfHeight) + bob + camPos * (1f - d.depth);
                d.transform.Rotate(0f, 0f, d.spin * dt);
            }

            UpdateComet(dt, t, camPos, halfWidth, halfHeight);
        }

        void UpdateComet(float dt, float t, Vector2 camPos, float halfWidth, float halfHeight)
        {
            if (!comet.enabled)
            {
                if (t < nextComet) return;
                comet.enabled = true;
                cometAge = 0f;
                cometLife = 0.8f;
                bool fromLeft = Next() < 0.5f;
                var start = new Vector2(fromLeft ? -halfWidth * 0.95f : halfWidth * 0.95f, Mathf.Lerp(0.2f, 0.9f, Next()) * halfHeight);
                comet.transform.position = camPos + start;
                cometVelocity = new Vector2(fromLeft ? 1f : -1f, -Mathf.Lerp(0.35f, 0.7f, Next())).normalized * Mathf.Lerp(10f, 15f, Next());
                comet.transform.rotation = Quaternion.Euler(0f, 0f, Geometry.HeadingOf(cometVelocity));
                comet.transform.localScale = new Vector3(0.07f, 1.6f, 1f);
                return;
            }

            cometAge += dt;
            if (cometAge >= cometLife)
            {
                comet.enabled = false;
                nextComet = t + Mathf.Lerp(cometInterval.x, cometInterval.y, Next());
                return;
            }
            comet.transform.position += (Vector3)(cometVelocity * dt);
            float k = cometAge / cometLife;
            comet.color = Color.Lerp(Color.white, Palette.HaloLight, k).WithAlpha(Mathf.Sin(k * Mathf.PI) * 0.8f);
        }
    }
}

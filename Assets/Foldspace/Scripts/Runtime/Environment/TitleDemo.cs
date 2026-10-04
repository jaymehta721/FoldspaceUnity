using System.Collections.Generic;
using Foldspace.Feedback;
using Foldspace.Rendering;
using Foldspace.Utilities;
using UnityEngine;

namespace Foldspace.Environment
{
    /// <summary>
    /// Attract mode behind the title: a ship circles three Ticks and folds them, over and over,
    /// so the core move is on screen before the first run. The ship and Ticks are static children of the prefab.
    /// </summary>
    public class TitleDemo : MonoBehaviour
    {
        const float StartAngle = -90f;
        const float FoldAwayTime = 0.2f;
        const float RespawnDelay = 0.7f;
        const float PopInTime = 0.35f;
        const int LoopSegments = 48;

        [SerializeField] Transform ship;
        [SerializeField] LineRenderer trail;
        [SerializeField] LineRenderer trailGlow;
        [SerializeField] Transform[] ticks;
        [SerializeField] Vector2 center = new Vector2(0f, -0.1f);
        [SerializeField] float radius = 1.15f;
        [SerializeField] float lapTime = 2.2f;
        [SerializeField] float trailTime = 2f;

        struct TrailPoint
        {
            public Vector2 position;
            public float time;
        }

        readonly List<TrailPoint> points = new List<TrailPoint>();
        Vector2[] tickHomes;
        GameContext context;
        float angle;
        float lapProgress;
        float foldedAt;
        float startedAt;

        void Awake()
        {
            tickHomes = new Vector2[ticks.Length];
            for (int i = 0; i < ticks.Length; i++) tickHomes[i] = (Vector2)ticks[i].localPosition - center;
        }

        public void Play(GameContext ctx)
        {
            context = ctx;
            angle = StartAngle;
            lapProgress = 0f;
            foldedAt = -10f;
            startedAt = Time.unscaledTime;
            points.Clear();
            gameObject.SetActive(true);
        }

        public void Stop() => gameObject.SetActive(false);

        static Vector2 OnCircle(Vector2 center, float radius, float degrees)
        {
            float a = degrees * Mathf.Deg2Rad;
            return center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
        }

        void Update()
        {
            if (context == null) return;
            float dt = Time.unscaledDeltaTime;
            float now = Time.unscaledTime;
            float step = 360f / lapTime * dt;
            angle += step;
            lapProgress += step;

            float a = angle * Mathf.Deg2Rad;
            Vector2 position = OnCircle(center, radius, angle);
            Vector2 tangent = new Vector2(-Mathf.Sin(a), Mathf.Cos(a));
            ship.localPosition = position;
            ship.localRotation = Quaternion.Euler(0f, 0f, Geometry.HeadingOf(tangent));

            points.Add(new TrailPoint { position = transform.TransformPoint(position), time = now });
            int expired = 0;
            while (expired < points.Count && now - points[expired].time > trailTime) expired++;
            if (expired > 0) points.RemoveRange(0, expired);
            trail.positionCount = points.Count;
            trailGlow.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++)
            {
                trail.SetPosition(i, points[i].position);
                trailGlow.SetPosition(i, points[i].position);
            }

            if (lapProgress >= 360f)
            {
                lapProgress -= 360f;
                if (now - startedAt > 1f) Fold(now);
            }

            for (int i = 0; i < ticks.Length; i++)
            {
                float since = now - foldedAt;
                float scale;
                if (since < FoldAwayTime)
                {
                    float k = since / FoldAwayTime;
                    ticks[i].localPosition = center + Geometry.Rotate(tickHomes[i], 200f * k) * (1f - Ease.InCubic(k));
                    scale = 1f - 0.85f * k;
                }
                else if (since < RespawnDelay)
                {
                    scale = 0f;
                }
                else
                {
                    ticks[i].localPosition = center + tickHomes[i] + new Vector2(Mathf.Sin(now * 2f + i), Mathf.Cos(now * 1.7f + i * 2f)) * 0.04f;
                    scale = Ease.OutBack((since - RespawnDelay) / PopInTime) * (1f + 0.06f * Mathf.Sin(now * 7f + i));
                }
                ticks[i].localScale = Vector3.one * Mathf.Max(0.001f, scale);
            }
        }

        void Fold(float now)
        {
            foldedAt = now;
            Vector2 worldCenter = transform.TransformPoint(center);
            context.Vfx.PlayFold(MeshFactory.CirclePoints(radius, LoopSegments, worldCenter), true);
            context.Arena.Grid.Impulse(worldCenter, 2.6f, -3.5f);
            context.Audio.Play(SfxId.Fold, 1f, 0.2f);
            points.Clear();
        }
    }
}

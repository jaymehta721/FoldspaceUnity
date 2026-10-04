using System.Collections.Generic;
using Foldspace.Utilities;
using UnityEngine;

namespace Foldspace.Gameplay
{
    /// <summary>
    /// The glowing trail behind the ship. Records the recent path, lets enemies test contact against it,
    /// and raises <see cref="GameEvents.LoopClosed"/> when the ship crosses (or comes back to) an older part of the trail.
    /// </summary>
    public class Wake : MonoBehaviour
    {
        struct Point
        {
            public Vector2 position;
            public float time;
        }

        [SerializeField] LineRenderer ribbon;
        [SerializeField] LineRenderer glow;
        [SerializeField] float glowWidth = 0.55f;

        readonly List<Point> points = new List<Point>();
        readonly List<Vector2> loopBuffer = new List<Vector2>();
        GameContext context;
        float flash;

        public int PointCount => points.Count;

        public void Init(GameContext ctx)
        {
            context = ctx;
            points.Clear();
            flash = 0f;
            ribbon.positionCount = 0;
            glow.positionCount = 0;
        }

        /// <summary>Briefly widens and brightens the trail, for the dash.</summary>
        public void Flash() => flash = 1f;

        public void Record(Vector2 head)
        {
            var settings = context.Config.wake;
            float now = Time.time;
            float cutoff = now - settings.duration;
            int expired = 0;
            while (expired < points.Count && points[expired].time < cutoff) expired++;
            if (expired > 0) points.RemoveRange(0, expired);

            float spacing = settings.sampleSpacing;
            bool farEnough = points.Count == 0 || (head - points[points.Count - 1].position).sqrMagnitude >= spacing * spacing;
            if (farEnough)
            {
                if (points.Count >= 2) TryCloseLoop(head, now);
                points.Add(new Point { position = head, time = now });
            }
            Draw(head, settings.width);
        }

        public bool Touches(Vector2 position, float radius)
        {
            float r = radius + context.Config.wake.width * 0.5f;
            float rSq = r * r;
            for (int i = 0; i + 1 < points.Count; i++)
                if (Geometry.SqrDistanceToSegment(position, points[i].position, points[i + 1].position) <= rSq)
                    return true;
            return false;
        }

        void TryCloseLoop(Vector2 head, float now)
        {
            var fold = context.Config.fold;
            Vector2 tail = points[points.Count - 1].position;
            float closeSq = fold.loopCloseDistance * fold.loopCloseDistance;

            // Newest first, so the most recent crossing wins and figure-eights fold their newest lobe.
            for (int i = points.Count - 2; i >= 0; i--)
            {
                Vector2 a = points[i].position;
                Vector2 closePoint;
                if (i + 1 < points.Count - 1 && Geometry.SegmentIntersection(tail, head, a, points[i + 1].position, out var hit))
                    closePoint = hit;
                else if (now - points[i].time >= fold.minLoopAge && (head - a).sqrMagnitude <= closeSq)
                    closePoint = a;
                else
                    continue;

                loopBuffer.Clear();
                loopBuffer.Add(closePoint);
                for (int j = i + 1; j < points.Count; j++) loopBuffer.Add(points[j].position);
                loopBuffer.Add(head);
                if (Mathf.Abs(Geometry.SignedArea(loopBuffer)) < fold.minArea) continue;

                var loop = new List<Vector2>(loopBuffer);
                points.Clear();
                points.Add(new Point { position = closePoint, time = now });
                context.Events.RaiseLoopClosed(loop);
                return;
            }
        }

        void Draw(Vector2 head, float width)
        {
            flash = Mathf.Max(0f, flash - Time.deltaTime * 2.5f);
            ribbon.widthMultiplier = width * (1f + 0.7f * flash);
            glow.widthMultiplier = glowWidth * (1f + 0.9f * flash);
            int count = points.Count + 1;
            ribbon.positionCount = count;
            glow.positionCount = count;
            for (int i = 0; i < points.Count; i++)
            {
                ribbon.SetPosition(i, points[i].position);
                glow.SetPosition(i, points[i].position);
            }
            ribbon.SetPosition(points.Count, head);
            glow.SetPosition(points.Count, head);
        }
    }
}

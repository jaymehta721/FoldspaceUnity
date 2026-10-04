using System.Collections.Generic;
using UnityEngine;

namespace Foldspace.Utilities
{
    public static class Geometry
    {
        /// <summary>Unit vector for a heading in degrees, where 0 points up and positive turns counter-clockwise.</summary>
        public static Vector2 Direction(float heading)
        {
            float r = heading * Mathf.Deg2Rad;
            return new Vector2(-Mathf.Sin(r), Mathf.Cos(r));
        }

        public static float HeadingOf(Vector2 v) => Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg - 90f;

        public static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        public static bool SegmentIntersection(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2, out Vector2 hit)
        {
            hit = default;
            Vector2 r = p2 - p1;
            Vector2 s = q2 - q1;
            float denom = Cross(r, s);
            if (Mathf.Abs(denom) < 1e-9f) return false;
            Vector2 qp = q1 - p1;
            float t = Cross(qp, s) / denom;
            float u = Cross(qp, r) / denom;
            if (t < 0f || t > 1f || u < 0f || u > 1f) return false;
            hit = p1 + t * r;
            return true;
        }

        public static float SignedArea(IReadOnlyList<Vector2> polygon)
        {
            float area = 0f;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
                area += Cross(polygon[j], polygon[i]);
            return area * 0.5f;
        }

        public static bool PointInPolygon(Vector2 p, IReadOnlyList<Vector2> polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }

        public static Vector2 Centroid(IReadOnlyList<Vector2> polygon)
        {
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < polygon.Count; i++) sum += polygon[i];
            return polygon.Count > 0 ? sum / polygon.Count : sum;
        }

        public static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r);
            float s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        public static float SqrDistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-9f));
            return (a + ab * t - p).sqrMagnitude;
        }
    }
}

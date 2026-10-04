using System.Collections.Generic;
using UnityEngine;

namespace Foldspace.Rendering
{
    /// <summary>Vertex-colored 2D meshes and circle helpers.</summary>
    public static class MeshFactory
    {
        /// <summary>Radius of the ring in the baked ring sprite at scale 1.</summary>
        public const float RingSpriteRadius = 0.41f;

        public static List<Vector2> CirclePoints(float radius, int segments, Vector2 center = default)
        {
            var points = new List<Vector2>(segments);
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                points.Add(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
            }
            return points;
        }

        public static void SetCircle(LineRenderer line, Vector2 center, float radius, int segments)
        {
            line.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                line.SetPosition(i, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
            }
        }

        /// <summary>Disc with one color at the center blending to another at the edge.</summary>
        public static Mesh RadialDisc(string name, float radius, int segments, Color center, Color edge)
        {
            var mesh = new Mesh { name = name };
            FillFan(mesh, CirclePoints(radius, segments), Vector2.zero, edge);
            var colors = mesh.colors;
            colors[0] = center;
            mesh.colors = colors;
            return mesh;
        }

        /// <summary>Flat ring between two radii, colored from the inner edge to the outer edge.</summary>
        public static Mesh RingMesh(string name, float inner, float outer, int segments, Color innerColor, Color outerColor)
        {
            var vertices = new Vector3[segments * 2];
            var colors = new Color[segments * 2];
            var triangles = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                vertices[i * 2] = dir * inner;
                vertices[i * 2 + 1] = dir * outer;
                colors[i * 2] = innerColor;
                colors[i * 2 + 1] = outerColor;
                int next = (i + 1) % segments;
                int t = i * 6;
                triangles[t] = i * 2;
                triangles[t + 1] = i * 2 + 1;
                triangles[t + 2] = next * 2 + 1;
                triangles[t + 3] = i * 2;
                triangles[t + 4] = next * 2 + 1;
                triangles[t + 5] = next * 2;
            }
            var mesh = new Mesh { name = name, vertices = vertices, colors = colors, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Rewrites <paramref name="mesh"/> as a triangle fan around <paramref name="center"/>.
        /// Fine for the near-convex loops a turn-limited ship draws.
        /// </summary>
        public static void FillFan(Mesh mesh, IReadOnlyList<Vector2> outline, Vector2 center, Color color)
        {
            int n = outline.Count;
            var vertices = new Vector3[n + 1];
            var colors = new Color[n + 1];
            var triangles = new int[n * 3];
            vertices[0] = center;
            colors[0] = color;
            for (int i = 0; i < n; i++)
            {
                vertices[i + 1] = outline[i];
                colors[i + 1] = color;
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % n + 1;
            }
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Foldspace.Environment
{
    /// <summary>
    /// A spring lattice inside the lens. Folds pull it in, impacts push it out, and the ship dents it as it flies.
    /// Lines brighten where the lattice is displaced, so ripples read as light. The mesh is rebuilt every frame.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SpaceGrid : MonoBehaviour
    {
        const float Step = 1f / 90f;

        [SerializeField] Color color = new Color(0.282f, 0.78f, 1f);
        [SerializeField] float spacing = 0.3f;
        [SerializeField] float lineWidth = 0.018f;

        [Header("Springs")]
        [SerializeField] float stiffness = 260f;
        [SerializeField] float anchor = 16f;
        [SerializeField] float damping = 3.4f;
        [SerializeField] float maxOffset = 0.7f;

        int n;
        Vector2[] rest;
        Vector2[] offset;
        Vector2[] velocity;
        bool[] inside;
        float[] rimFade;
        Color32[] pointColors;
        int[] segA;
        int[] segB;
        Vector3[] vertices;
        Color32[] colors;
        Mesh mesh;
        float accumulator;

        public void Init(float radius)
        {
            n = Mathf.CeilToInt(radius * 2f / spacing) + 1;
            if (n % 2 == 0) n++;
            int count = n * n;
            rest = new Vector2[count];
            offset = new Vector2[count];
            velocity = new Vector2[count];
            inside = new bool[count];
            rimFade = new float[count];
            pointColors = new Color32[count];

            float start = -(n - 1) * 0.5f * spacing;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int i = y * n + x;
                rest[i] = new Vector2(start + x * spacing, start + y * spacing);
                float d = rest[i].magnitude;
                inside[i] = d < radius - 0.04f;
                rimFade[i] = Mathf.Clamp01((radius - d) / 0.9f);
            }

            var a = new List<int>();
            var b = new List<int>();
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int i = y * n + x;
                if (!inside[i]) continue;
                if (x + 1 < n && inside[i + 1]) { a.Add(i); b.Add(i + 1); }
                if (y + 1 < n && inside[i + n]) { a.Add(i); b.Add(i + n); }
            }
            segA = a.ToArray();
            segB = b.ToArray();

            vertices = new Vector3[segA.Length * 4];
            colors = new Color32[segA.Length * 4];
            var triangles = new int[segA.Length * 6];
            for (int s = 0; s < segA.Length; s++)
            {
                int v = s * 4;
                int t = s * 6;
                triangles[t] = v;
                triangles[t + 1] = v + 1;
                triangles[t + 2] = v + 2;
                triangles[t + 3] = v;
                triangles[t + 4] = v + 2;
                triangles[t + 5] = v + 3;
            }

            if (mesh != null) Destroy(mesh);
            mesh = new Mesh { name = "SpaceGrid" };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.colors32 = colors;
            mesh.triangles = triangles;
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(radius * 2f + 2f, radius * 2f + 2f, 1f));
            GetComponent<MeshFilter>().sharedMesh = mesh;
            Rebuild();
        }

        /// <summary>Kicks the lattice away from <paramref name="center"/>, or toward it when strength is negative.</summary>
        public void Impulse(Vector2 center, float reach, float strength)
        {
            if (rest == null) return;
            float reachSq = reach * reach;
            for (int i = 0; i < rest.Length; i++)
            {
                if (!inside[i]) continue;
                Vector2 d = rest[i] + offset[i] - center;
                float sq = d.sqrMagnitude;
                if (sq >= reachSq || sq < 1e-6f) continue;
                float dist = Mathf.Sqrt(sq);
                float falloff = 1f - dist / reach;
                velocity[i] += d / dist * (strength * falloff * falloff);
            }
        }

        public void Calm()
        {
            if (rest == null) return;
            System.Array.Clear(offset, 0, offset.Length);
            System.Array.Clear(velocity, 0, velocity.Length);
        }

        void Update()
        {
            if (rest == null) return;
            accumulator = Mathf.Min(accumulator + Time.unscaledDeltaTime, 0.1f);
            while (accumulator >= Step)
            {
                Simulate(Step);
                accumulator -= Step;
            }
            Rebuild();
        }

        void Simulate(float h)
        {
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int i = y * n + x;
                if (!inside[i]) continue;
                Vector2 o = offset[i];
                Vector2 laplacian = -4f * o;
                if (x > 0) laplacian += offset[i - 1];
                if (x < n - 1) laplacian += offset[i + 1];
                if (y > 0) laplacian += offset[i - n];
                if (y < n - 1) laplacian += offset[i + n];
                velocity[i] += (stiffness * laplacian - anchor * o - damping * velocity[i]) * h;
            }
            for (int i = 0; i < offset.Length; i++)
                if (inside[i]) offset[i] = Vector2.ClampMagnitude(offset[i] + velocity[i] * h, maxOffset);
        }

        void Rebuild()
        {
            for (int i = 0; i < offset.Length; i++)
            {
                if (!inside[i]) continue;
                float energy = Mathf.Clamp01(offset[i].magnitude * 3.2f);
                Color c = Color.Lerp(color, Color.white, energy * 0.55f);
                c.a = (0.075f + 0.6f * energy) * rimFade[i];
                pointColors[i] = c;
            }

            for (int s = 0; s < segA.Length; s++)
            {
                int a = segA[s];
                int b = segB[s];
                Vector2 pa = rest[a] + offset[a];
                Vector2 pb = rest[b] + offset[b];
                Vector2 dir = pb - pa;
                float length = dir.magnitude;
                Vector2 side = length > 1e-5f ? new Vector2(-dir.y, dir.x) * (lineWidth * 0.5f / length) : Vector2.zero;
                int v = s * 4;
                vertices[v] = pa - side;
                vertices[v + 1] = pa + side;
                vertices[v + 2] = pb + side;
                vertices[v + 3] = pb - side;
                colors[v] = pointColors[a];
                colors[v + 1] = pointColors[a];
                colors[v + 2] = pointColors[b];
                colors[v + 3] = pointColors[b];
            }
            mesh.vertices = vertices;
            mesh.colors32 = colors;
        }

        void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
        }
    }
}

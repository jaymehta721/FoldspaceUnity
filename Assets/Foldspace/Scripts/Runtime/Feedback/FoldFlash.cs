using System.Collections.Generic;
using Foldspace.Rendering;
using Foldspace.Utilities;
using UnityEngine;

namespace Foldspace.Feedback
{
    /// <summary>
    /// The signature effect: a closed loop's outline and fill collapse into its center, then pop.
    /// Runs on unscaled time so it plays during the fold hitstop. Pooled and driven by <see cref="Vfx"/>.
    /// </summary>
    public class FoldFlash : MonoBehaviour
    {
        const float CollapseTime = 0.2f;
        const float FadeTime = 0.22f;

        [SerializeField] LineRenderer glow;
        [SerializeField] LineRenderer outline;
        [SerializeField] MeshFilter fill;

        [Header("Widths")]
        [SerializeField] float caughtGlowWidth = 0.6f;
        [SerializeField] float emptyGlowWidth = 0.32f;
        [SerializeField] float caughtOutlineWidth = 0.07f;
        [SerializeField] float emptyOutlineWidth = 0.035f;

        readonly List<Vector2> loop = new List<Vector2>();
        MeshRenderer fillRenderer;
        Mesh fillMesh;
        Vector3[] fillVertices;
        Color[] fillColors;
        float bornAt;
        bool popped;

        public Vector2 Center { get; private set; }
        public float Radius { get; private set; }
        public bool Caught { get; private set; }

        void Awake()
        {
            fillMesh = new Mesh { name = "FoldFill" };
            fillMesh.MarkDynamic();
            fill.sharedMesh = fillMesh;
            fillRenderer = fill.GetComponent<MeshRenderer>();
        }

        void OnDestroy()
        {
            if (fillMesh != null) Destroy(fillMesh);
        }

        public void Play(IReadOnlyList<Vector2> points, bool caught)
        {
            loop.Clear();
            for (int i = 0; i < points.Count; i++) loop.Add(points[i]);
            Center = Geometry.Centroid(loop);
            Radius = Mathf.Sqrt(Mathf.Abs(Geometry.SignedArea(loop)) / Mathf.PI);
            Caught = caught;
            bornAt = Time.unscaledTime;
            popped = false;

            glow.widthMultiplier = caught ? caughtGlowWidth : emptyGlowWidth;
            outline.widthMultiplier = caught ? caughtOutlineWidth : emptyOutlineWidth;
            glow.positionCount = loop.Count;
            outline.positionCount = loop.Count;
            fillRenderer.enabled = caught;
            if (caught)
            {
                MeshFactory.FillFan(fillMesh, loop, Center, Palette.You.WithAlpha(0.4f));
                fillVertices = fillMesh.vertices;
                fillColors = fillMesh.colors;
            }
            Tick(bornAt, out _);
        }

        /// <summary>Advances the effect. Returns false once it has faded out; <paramref name="poppedNow"/> is true on the frame it pops.</summary>
        public bool Tick(float now, out bool poppedNow)
        {
            float age = now - bornAt;
            float collapse = Mathf.Clamp01(age / CollapseTime);
            float squeeze = Ease.InCubic(collapse) * (Caught ? 0.94f : 0.55f);
            for (int i = 0; i < loop.Count; i++)
            {
                Vector2 p = Vector2.LerpUnclamped(loop[i], Center, squeeze);
                outline.SetPosition(i, p);
                glow.SetPosition(i, p);
                if (Caught) fillVertices[i + 1] = p;
            }

            float fade = age < CollapseTime ? 1f : 1f - Mathf.Clamp01((age - CollapseTime) / FadeTime);
            Color tint = Caught ? Palette.You : Palette.Ore;
            Color line = Color.Lerp(Color.white, tint, collapse);
            line.a = (Caught ? 1f : 0.7f) * fade;
            outline.startColor = line;
            outline.endColor = line;
            Color halo = tint.WithAlpha((Caught ? 0.85f : 0.5f) * fade);
            glow.startColor = halo;
            glow.endColor = halo;

            if (Caught)
            {
                Color fillColor = Color.Lerp(Palette.You, Palette.YouHot, collapse).WithAlpha((0.3f + 0.45f * collapse) * fade);
                for (int i = 0; i < fillColors.Length; i++) fillColors[i] = fillColor;
                fillMesh.vertices = fillVertices;
                fillMesh.colors = fillColors;
            }

            poppedNow = !popped && collapse >= 1f;
            if (poppedNow) popped = true;
            return age < CollapseTime + FadeTime;
        }
    }
}

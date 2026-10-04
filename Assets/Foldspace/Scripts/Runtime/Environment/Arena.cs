using Foldspace.Rendering;
using UnityEngine;

namespace Foldspace.Environment
{
    /// <summary>
    /// The play lens: dark glass over deep space with a warping grid inside, set in a bezel whose rim
    /// doubles as the XP bar. The meshes and rim circles saved in the prefab are built for <see cref="radius"/>;
    /// they're regenerated at runtime if the config asks for a different radius.
    /// </summary>
    public class Arena : MonoBehaviour
    {
        public const float RimWidth = 0.16f;
        /// <summary>How far the rim, bezel and glow reach past the arena radius. Used to frame the camera.</summary>
        public const float OuterExtent = 0.42f;
        public const int Segments = 160;

        const float RimGlowWidth = 0.9f;
        const float XpGlowWidth = 0.42f;
        const float XpCoreWidth = RimWidth * 0.42f;
        const float XpHeadSize = 0.36f;

        [SerializeField] float radius = 4.4f;

        [Header("Lens")]
        [SerializeField] MeshFilter lens;
        [SerializeField] MeshFilter lensShade;
        [SerializeField] MeshFilter bezel;
        [SerializeField] SpaceGrid grid;

        [Header("Rim")]
        [SerializeField] LineRenderer rimGlow;
        [SerializeField] LineRenderer rim;
        [SerializeField] LineRenderer rimLight;
        [SerializeField] SpriteRenderer bump;

        [Header("XP")]
        [SerializeField] LineRenderer xpGlow;
        [SerializeField] LineRenderer xpCore;
        [SerializeField] SpriteRenderer xpHead;

        float xpShown = -1f;
        float xpTarget;
        float pulse;
        float rimFlash;
        float bumpFlash;
        Color rimFlashColor = Color.white;

        public SpaceGrid Grid => grid;
        public float Radius => radius;
        float RimRadius => radius + RimWidth * 0.5f;

        public void Init(float configuredRadius)
        {
            if (!Mathf.Approximately(radius, configuredRadius)) Build(configuredRadius);
            grid.Init(radius);
            xpShown = -1f;
            SetXp(0f);
        }

        /// <summary>Rebuilds the lens and grid if the configured radius changed since the last run.</summary>
        public void EnsureRadius(float configuredRadius)
        {
            if (Mathf.Approximately(radius, configuredRadius)) return;
            Build(configuredRadius);
            grid.Init(radius);
        }

        /// <summary>Regenerates the lens meshes and rim circles for <paramref name="newRadius"/>.</summary>
        void Build(float newRadius)
        {
            radius = newRadius;
            float r = radius;
            lens.sharedMesh = MeshFactory.RadialDisc("Lens", r + 0.02f, Segments, Palette.LensCenter.WithAlpha(0.84f), Palette.LensEdge.WithAlpha(0.93f));
            lensShade.sharedMesh = MeshFactory.RingMesh("LensShade", r - 1f, r + 0.02f, Segments, new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0.02f, 0.5f));
            bezel.sharedMesh = BezelMesh(r + RimWidth + 0.05f);
            MeshFactory.SetCircle(rimGlow, Vector2.zero, RimRadius + 0.04f, Segments);
            MeshFactory.SetCircle(rim, Vector2.zero, RimRadius, Segments);
            MeshFactory.SetCircle(rimLight, Vector2.zero, r + 0.012f, Segments);
            xpShown = -1f;
            DrawXp(0f);
        }

        public void SetXp(float fraction) => xpTarget = Mathf.Clamp01(fraction);

        /// <summary>Level-up flash around the whole rim.</summary>
        public void Pulse() => pulse = 1f;

        public void FlashRim(Color color)
        {
            rimFlash = 1f;
            rimFlashColor = color;
        }

        /// <summary>Local flash where the ship bounced off the rim.</summary>
        public void Bump(Vector2 at)
        {
            bump.transform.position = at.normalized * RimRadius;
            bumpFlash = 1f;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float t = Time.unscaledTime;
            pulse = Mathf.Max(0f, pulse - dt * 1.8f);
            rimFlash = Mathf.Max(0f, rimFlash - dt * 2.5f);
            bumpFlash = Mathf.Max(0f, bumpFlash - dt * 4f);

            Color rimColor = Color.Lerp(Color.Lerp(Palette.Rim, Palette.Ore, pulse * 0.7f), rimFlashColor, rimFlash * 0.8f);
            rim.startColor = rimColor;
            rim.endColor = rimColor;

            Color glowColor = Palette.Ore.WithAlpha(0.18f + 0.04f * Mathf.Sin(t * 1.3f));
            glowColor = Color.Lerp(glowColor, Palette.Ore.WithAlpha(0.7f), pulse);
            glowColor = Color.Lerp(glowColor, rimFlashColor.WithAlpha(0.8f), rimFlash);
            rimGlow.startColor = glowColor;
            rimGlow.endColor = glowColor;
            rimGlow.widthMultiplier = RimGlowWidth * (1f + pulse * 0.8f + rimFlash * 0.5f);

            bump.enabled = bumpFlash > 0f;
            if (bump.enabled) bump.color = Color.white.WithAlpha(bumpFlash * 0.8f);

            float previous = xpShown;
            xpShown = xpTarget < xpShown ? xpTarget : Mathf.MoveTowards(Mathf.Max(xpShown, 0f), xpTarget, dt * 1.2f);
            if (previous != xpShown) DrawXp(xpShown);

            float thickness = 1f + pulse * 0.8f;
            xpCore.widthMultiplier = XpCoreWidth * thickness;
            xpGlow.widthMultiplier = XpGlowWidth * thickness;
            xpHead.transform.localScale = Vector3.one * (XpHeadSize + 0.08f * Mathf.Sin(t * 6f) + pulse * 0.5f);
        }

        void DrawXp(float fraction)
        {
            bool visible = fraction > 0.002f;
            xpHead.enabled = visible;
            if (!visible)
            {
                xpCore.positionCount = 0;
                xpGlow.positionCount = 0;
                return;
            }
            int count = Mathf.Max(2, Mathf.CeilToInt(Segments * fraction) + 1);
            xpCore.positionCount = count;
            xpGlow.positionCount = count;
            Vector2 last = default;
            for (int i = 0; i < count; i++)
            {
                float a = (90f - fraction * 360f * i / (count - 1)) * Mathf.Deg2Rad;
                last = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * RimRadius;
                xpCore.SetPosition(i, last);
                xpGlow.SetPosition(i, last);
            }
            xpHead.transform.position = last;
        }

        /// <summary>Instrument-style tick marks around the outside of the rim, with a major tick every 30 degrees.</summary>
        static Mesh BezelMesh(float inner)
        {
            const int ticks = 72;
            var vertices = new Vector3[ticks * 4];
            var colors = new Color[ticks * 4];
            var triangles = new int[ticks * 6];
            for (int i = 0; i < ticks; i++)
            {
                bool major = i % 6 == 0;
                float a = i * Mathf.PI * 2f / ticks;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var side = new Vector2(-dir.y, dir.x) * (major ? 0.022f : 0.011f);
                float length = major ? 0.2f : 0.09f;
                int v = i * 4;
                vertices[v] = dir * inner - side;
                vertices[v + 1] = dir * inner + side;
                vertices[v + 2] = dir * (inner + length) + side;
                vertices[v + 3] = dir * (inner + length) - side;
                var c = (major ? Palette.RimLight : Palette.Bezel).WithAlpha(major ? 0.9f : 0.7f);
                var tip = c.WithAlpha(0.15f);
                colors[v] = c;
                colors[v + 1] = c;
                colors[v + 2] = tip;
                colors[v + 3] = tip;
                int t = i * 6;
                triangles[t] = v;
                triangles[t + 1] = v + 1;
                triangles[t + 2] = v + 2;
                triangles[t + 3] = v;
                triangles[t + 4] = v + 2;
                triangles[t + 5] = v + 3;
            }
            var mesh = new Mesh { name = "Bezel", vertices = vertices, colors = colors, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

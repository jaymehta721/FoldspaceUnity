using System;
using UnityEngine;

namespace Foldspace.Rendering
{
    /// <summary>
    /// Post-processing for the built-in pipeline: bloom, shockwave distortion, chromatic aberration,
    /// color grading, vignette, a hurt vignette and film grain. Gameplay feedback fires the short pulses.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PostFx : MonoBehaviour
    {
        [SerializeField] Shader shader;

        [Serializable]
        public class Settings
        {
            public bool enabled = true;

            [Header("Bloom")]
            public bool bloom = true;
            [Range(0f, 2f)] public float bloomThreshold = 0.95f;
            [Range(0f, 1f)] public float bloomKnee = 0.4f;
            [Range(0f, 4f)] public float bloomIntensity = 1f;
            [Range(2, 8)] public int bloomIterations = 6;
            public Color bloomTint = Color.white;

            [Header("Color")]
            [Range(0.5f, 2f)] public float exposure = 1f;
            [Range(0.5f, 1.5f)] public float contrast = 1.06f;
            [Range(0f, 2f)] public float saturation = 1.12f;

            [Header("Lens")]
            [Range(0f, 0.05f)] public float chromaticAberration = 0.0025f;
            [Range(0f, 1f)] public float vignette = 0.45f;
            public Color vignetteColor = new Color(0f, 0f, 0.015f);
            [Tooltip("Inner and outer vignette radius, in screen heights from the center.")]
            public Vector2 vignetteRange = new Vector2(0.5f, 1.05f);
            [Range(0f, 0.2f)] public float grain = 0.03f;

            [Header("Gameplay pulses")]
            public bool shockwaves = true;
            [Range(0f, 3f)] public float shockwaveStrength = 1f;
            [Range(0f, 3f)] public float impactAberration = 1f;
            [Range(0f, 1f)] public float lowHullPulse = 0.3f;
            public Color hurtColor = new Color(0.55f, 0.02f, 0.12f);
        }

        struct Wave
        {
            public Vector2 world;
            public float bornAt;
            public float strength;
            public float speed;
            public float life;
        }

        const int PassPrefilter = 0, PassDown = 1, PassUp = 2, PassComposite = 3;
        static readonly int BloomTexId = Shader.PropertyToID("_BloomTex");
        static readonly int ThresholdId = Shader.PropertyToID("_Threshold");
        static readonly int BloomTintId = Shader.PropertyToID("_BloomTint");
        static readonly int WavesId = Shader.PropertyToID("_Waves");
        static readonly int AspectId = Shader.PropertyToID("_Aspect");
        static readonly int AberrationId = Shader.PropertyToID("_Aberration");
        static readonly int FlashId = Shader.PropertyToID("_Flash");
        static readonly int VignetteId = Shader.PropertyToID("_Vignette");
        static readonly int VignetteRangeId = Shader.PropertyToID("_VignetteRange");
        static readonly int DamageId = Shader.PropertyToID("_Damage");
        static readonly int GradeId = Shader.PropertyToID("_Grade");
        static readonly int GrainSeedId = Shader.PropertyToID("_GrainSeed");

        public Settings settings = new Settings();

        readonly Wave[] waves = new Wave[4];
        readonly Vector4[] waveData = new Vector4[4];
        readonly RenderTexture[] chain = new RenderTexture[8];
        Material material;
        Camera cam;
        int nextWave;
        float aberrationPulse;
        float hurt;
        float flash;
        Color flashColor = Color.white;

        /// <summary>Pulses a red vignette while true.</summary>
        public bool LowHull { get; set; }

        void OnEnable()
        {
            cam = GetComponent<Camera>();
            if (material != null) return;
            if (shader != null && shader.isSupported) material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            else Debug.LogWarning("[Foldspace] Post-processing shader missing or unsupported; rendering without it.", this);
        }

        void OnDestroy()
        {
            if (material != null) Destroy(material);
        }

        /// <summary>Expanding ring of distortion. Negative strength pinches instead of bulging.</summary>
        public void Shockwave(Vector2 world, float strength, float speed = 1.3f, float life = 0.6f)
        {
            waves[nextWave] = new Wave { world = world, bornAt = Time.unscaledTime, strength = strength, speed = speed, life = life };
            nextWave = (nextWave + 1) % waves.Length;
        }

        public void Aberration(float amount) => aberrationPulse = Mathf.Max(aberrationPulse, amount);
        public void Hurt(float amount) => hurt = Mathf.Max(hurt, amount);

        public void Flash(Color color, float amount)
        {
            if (amount < flash) return;
            flash = amount;
            flashColor = color;
        }

        public void ResetPulses()
        {
            aberrationPulse = 0f;
            hurt = 0f;
            flash = 0f;
            LowHull = false;
            for (int i = 0; i < waves.Length; i++) waves[i].strength = 0f;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            aberrationPulse *= Mathf.Exp(-5f * dt);
            hurt *= Mathf.Exp(-3f * dt);
            flash *= Mathf.Exp(-10f * dt);
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!settings.enabled || material == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            var s = settings;
            RenderTexture bloom = s.bloom && s.bloomIntensity > 0f ? RenderBloom(source) : null;
            material.SetTexture(BloomTexId, bloom != null ? (Texture)bloom : Texture2D.blackTexture);
            material.SetColor(BloomTintId, s.bloomTint * (bloom != null ? s.bloomIntensity : 0f));

            float now = Time.unscaledTime;
            for (int i = 0; i < waves.Length; i++)
            {
                var w = waves[i];
                float age = now - w.bornAt;
                if (!s.shockwaves || w.strength == 0f || age >= w.life)
                {
                    waveData[i] = Vector4.zero;
                    continue;
                }
                Vector3 viewport = cam.WorldToViewportPoint(w.world);
                float fade = 1f - age / w.life;
                waveData[i] = new Vector4(viewport.x, viewport.y, age * w.speed, w.strength * fade * fade * s.shockwaveStrength);
            }
            material.SetVectorArray(WavesId, waveData);

            float hurtAmount = hurt;
            if (LowHull) hurtAmount = Mathf.Max(hurtAmount, s.lowHullPulse * (0.55f + 0.45f * Mathf.Sin(now * 5f)));

            material.SetFloat(AspectId, source.width / (float)Mathf.Max(1, source.height));
            material.SetFloat(AberrationId, s.chromaticAberration + aberrationPulse * s.impactAberration);
            material.SetColor(FlashId, new Color(flashColor.r, flashColor.g, flashColor.b, flash));
            material.SetColor(VignetteId, new Color(s.vignetteColor.r, s.vignetteColor.g, s.vignetteColor.b, s.vignette));
            material.SetVector(VignetteRangeId, s.vignetteRange);
            material.SetColor(DamageId, new Color(s.hurtColor.r, s.hurtColor.g, s.hurtColor.b, Mathf.Clamp01(hurtAmount)));
            material.SetVector(GradeId, new Vector4(s.exposure, s.contrast, s.saturation, s.grain));
            material.SetFloat(GrainSeedId, UnityEngine.Random.value * 100f);

            Graphics.Blit(source, destination, material, PassComposite);
            if (bloom != null) RenderTexture.ReleaseTemporary(bloom);
        }

        RenderTexture RenderBloom(RenderTexture source)
        {
            var s = settings;
            float knee = Mathf.Max(s.bloomThreshold * s.bloomKnee, 0.0001f);
            material.SetVector(ThresholdId, new Vector4(s.bloomThreshold, s.bloomThreshold - knee, 2f * knee, 0.25f / knee));

            var format = source.format;
            int width = Mathf.Max(1, source.width / 2);
            int height = Mathf.Max(1, source.height / 2);
            var current = RenderTexture.GetTemporary(width, height, 0, format);
            Graphics.Blit(source, current, material, PassPrefilter);
            chain[0] = current;

            int levels = 1;
            for (; levels < Mathf.Min(s.bloomIterations, chain.Length); levels++)
            {
                width /= 2;
                height /= 2;
                if (width < 2 || height < 2) break;
                var next = RenderTexture.GetTemporary(width, height, 0, format);
                Graphics.Blit(current, next, material, PassDown);
                chain[levels] = next;
                current = next;
            }

            for (int i = levels - 2; i >= 0; i--)
            {
                var target = chain[i];
                Graphics.Blit(current, target, material, PassUp);
                RenderTexture.ReleaseTemporary(current);
                chain[i + 1] = null;
                current = target;
            }
            chain[0] = null;
            return current;
        }
    }
}

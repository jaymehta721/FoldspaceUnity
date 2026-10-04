using Foldspace.Rendering;
using UnityEngine;

namespace Foldspace.Gameplay
{
    /// <summary>Slow, bullet-proof shield carrier. Only a fold can kill it, and its shield makes nearby enemies bullet-proof.</summary>
    public class Warden : Enemy
    {
        const float SpinSpeed = 50f;
        const float OrbitSpeed = 40f;
        const float DeflectReach = 0.3f;

        [SerializeField] SpriteRenderer shieldRing;
        [Tooltip("Unit-radius disc, scaled to the shield radius.")]
        [SerializeField] Transform shieldFill;
        [SerializeField] SpriteRenderer[] orbiters;

        float shieldFlash;
        float orbit;

        float ShieldRadius => Definition.shieldRadius;

        protected override void OnSpawned()
        {
            shieldFlash = 0f;
            orbit = 0f;
            shieldFill.localScale = Vector3.one * ShieldRadius;
            AnimateShield(0f);
        }

        protected override void Think(float dt)
        {
            Seek(dt);
            body.transform.Rotate(0f, 0f, SpinSpeed * dt);
            AnimateShield(dt);
        }

        public override void OnDeflected(Vector2 point)
        {
            float reach = ShieldRadius + DeflectReach;
            if ((point - Position).sqrMagnitude <= reach * reach) shieldFlash = 1f;
        }

        void AnimateShield(float dt)
        {
            shieldFlash = Mathf.Max(0f, shieldFlash - dt * 3f);
            float breathe = 1f + 0.02f * Mathf.Sin(Time.time * 3f);
            shieldRing.transform.localScale = Vector3.one * (ShieldRadius / MeshFactory.RingSpriteRadius * breathe);
            shieldRing.color = Color.Lerp(Palette.Halo, Color.white, shieldFlash).WithAlpha(0.45f + 0.5f * shieldFlash);

            orbit += dt * OrbitSpeed;
            for (int i = 0; i < orbiters.Length; i++)
            {
                float a = (orbit + i * 360f / orbiters.Length) * Mathf.Deg2Rad;
                orbiters[i].transform.localPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * ShieldRadius;
                orbiters[i].transform.localScale = Vector3.one * (0.2f + 0.15f * shieldFlash);
            }
        }
    }
}

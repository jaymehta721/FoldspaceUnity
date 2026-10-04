using Foldspace.Rendering;
using Foldspace.Utilities;
using UnityEngine;

namespace Foldspace.Feedback
{
    /// <summary>Pulsing warning where a pack is about to arrive. Pooled and driven by <see cref="Vfx"/>.</summary>
    public class SpawnTelegraph : MonoBehaviour
    {
        [SerializeField] SpriteRenderer ring;
        [SerializeField] SpriteRenderer core;
        [SerializeField] SpriteRenderer icon;
        [SerializeField] float iconScale = 0.55f;

        float bornAt;
        float duration;

        public void Play(Vector2 at, float seconds)
        {
            transform.position = at;
            bornAt = Time.time;
            duration = Mathf.Max(0.01f, seconds);
            Tick();
        }

        /// <summary>Advances the effect on scaled time. Returns false when it's done.</summary>
        public bool Tick()
        {
            float age = Time.time - bornAt;
            if (age >= duration) return false;

            float beat = Mathf.Repeat(age * 2.2f, 1f);
            float radius = Mathf.Lerp(1.1f, 0.25f, Ease.OutCubic(beat));
            ring.transform.localScale = Vector3.one * (radius / MeshFactory.RingSpriteRadius);
            ring.color = Palette.HaloLight.WithAlpha(0.85f * (1f - beat));

            float grow = age / duration;
            core.transform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.3f, grow);
            core.color = Palette.Halo.WithAlpha(0.25f + 0.4f * grow);

            float wobble = Mathf.Sin(age * 18f);
            icon.transform.localScale = Vector3.one * (iconScale + 0.08f * wobble);
            icon.color = Palette.HaloLight.WithAlpha(0.7f + 0.3f * wobble);
            return true;
        }
    }
}

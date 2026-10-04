using UnityEngine;
using UnityEngine.UI;

namespace Foldspace.UI
{
    /// <summary>Small helpers shared by the HUD views.</summary>
    public static class UiMotion
    {
        /// <summary>Moves a group's alpha toward <paramref name="target"/>, deactivating it once fully hidden.</summary>
        public static void Fade(CanvasGroup group, float target, float dt, float speed = 6f)
        {
            group.alpha = Mathf.MoveTowards(group.alpha, target, dt * speed);
            bool visible = group.alpha > 0.001f || target > 0f;
            if (group.gameObject.activeSelf != visible) group.gameObject.SetActive(visible);
        }

        public static void Hide(CanvasGroup group)
        {
            group.alpha = 0f;
            group.gameObject.SetActive(false);
        }

        /// <summary>Sets a bar made by anchoring a fill image inside its track.</summary>
        public static void SetFill(Image bar, float fraction)
        {
            fraction = Mathf.Clamp01(fraction);
            bar.enabled = fraction > 0.001f;
            bar.rectTransform.anchorMax = new Vector2(fraction, 1f);
        }
    }
}

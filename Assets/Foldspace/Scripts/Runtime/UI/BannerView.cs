using Foldspace.Rendering;
using Foldspace.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Foldspace.UI
{
    /// <summary>Big centered callouts: enemy arrivals and level-ups.</summary>
    public class BannerView : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] Text title;
        [SerializeField] Text subtitle;
        [SerializeField] float life = 1.6f;

        float shownAt = -10f;

        public void Init(GameContext ctx)
        {
            ctx.Events.Announced += Show;
            ctx.Events.LevelUp += (level, _) => Show("LEVEL " + level, "keep folding", Palette.Ore);
            UiMotion.Hide(group);
        }

        public void Show(string text, string sub, Color color)
        {
            title.text = text;
            title.color = color;
            subtitle.text = sub ?? "";
            shownAt = Time.unscaledTime;
        }

        public void Clear() => shownAt = -10f;

        public void Tick(float now)
        {
            float age = now - shownAt;
            if (age > life)
            {
                if (group.gameObject.activeSelf) UiMotion.Hide(group);
                return;
            }
            if (!group.gameObject.activeSelf) group.gameObject.SetActive(true);
            group.alpha = age < 0.08f ? age / 0.08f : age > life - 0.4f ? (life - age) / 0.4f : 1f;
            float scale = age < 0.25f ? Mathf.LerpUnclamped(1.7f, 1f, Ease.OutBack(age / 0.25f)) : 1f + 0.03f * (age - 0.25f);
            title.rectTransform.localScale = Vector3.one * scale;
        }
    }
}

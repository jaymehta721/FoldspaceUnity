using Foldspace.Rendering;
using Foldspace.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Foldspace.UI
{
    /// <summary>Logo, tagline, control chips and the launch prompt shown over the attract-mode demo.</summary>
    public class TitleScreenView : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform logo;
        [SerializeField] Text prompt;
        [SerializeField] Text best;

        [Header("Control chips")]
        [SerializeField] Text steerKeys;
        [SerializeField] Text dashKeys;

        GameContext context;
        Vector2 logoHome;
        float shownAt;
        bool visible;

        public void Init(GameContext ctx)
        {
            context = ctx;
            logoHome = logo.anchoredPosition;
            if (Application.isMobilePlatform)
            {
                steerKeys.text = "DRAG ANYWHERE";
                dashKeys.text = "SECOND FINGER";
                prompt.text = "TAP TO LAUNCH";
            }
            UiMotion.Hide(group);
        }

        public void Tick(bool show, float now, float dt)
        {
            if (show && !visible)
            {
                visible = true;
                shownAt = now;
                int bestScore = context.HighScores.Best;
                best.text = bestScore > 0 ? "BEST  " + bestScore.ToString("N0") : "";
            }
            if (!show) visible = false;
            UiMotion.Fade(group, show ? 1f : 0f, dt, show ? 3f : 6f);
            if (!group.gameObject.activeSelf) return;

            float drop = Ease.OutBack((now - shownAt) / 0.6f);
            logo.anchoredPosition = logoHome + new Vector2(0f, (1f - drop) * 120f + 8f * Mathf.Sin(now * 1.3f));
            logo.localScale = Vector3.one * (0.9f + 0.1f * drop);
            prompt.color = Palette.You.WithAlpha(0.55f + 0.45f * Mathf.Sin(now * 4f));
        }
    }
}

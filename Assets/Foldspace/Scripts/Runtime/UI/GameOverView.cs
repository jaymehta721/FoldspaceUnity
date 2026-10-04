using Foldspace.Rendering;
using Foldspace.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Foldspace.UI
{
    /// <summary>The run-over card: slam-in title, counting score, run stats and the restart prompt.</summary>
    public class GameOverView : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform title;
        [SerializeField] RectTransform card;
        [SerializeField] CanvasGroup cardGroup;
        [SerializeField] RectTransform newBestBadge;
        [SerializeField] Text score;
        [Tooltip("Time, level, folds and best chain, in that order.")]
        [SerializeField] Text[] stats = new Text[4];
        [SerializeField] Text detail;
        [SerializeField] Text prompt;
        [SerializeField] float promptDelay = 1.2f;

        GameContext context;
        Vector2 cardHome;
        float shownAt;
        bool visible;
        int shownScore = -1;

        public void Init(GameContext ctx)
        {
            context = ctx;
            cardHome = card.anchoredPosition;
            if (Application.isMobilePlatform) prompt.text = "TAP TO FLY AGAIN";
            UiMotion.Hide(group);
        }

        public void Tick(bool show, float now, float dt)
        {
            if (show && !visible) Show(now);
            if (!show) visible = false;
            UiMotion.Fade(group, show ? 1f : 0f, dt, show ? 4f : 8f);
            if (!show) return;

            float age = now - shownAt;
            title.localScale = Vector3.one * Mathf.LerpUnclamped(1.6f, 1f, Ease.OutBack(age / 0.4f));
            float slide = Mathf.Clamp01((age - 0.15f) / 0.45f);
            card.anchoredPosition = cardHome + new Vector2(0f, Mathf.Lerp(-80f, 0f, Ease.OutCubic(slide)));
            cardGroup.alpha = slide;
            int counted = Mathf.RoundToInt(context.Session.Score * Ease.OutCubic((age - 0.35f) / 0.9f));
            if (counted != shownScore)
            {
                shownScore = counted;
                score.text = counted.ToString("N0");
            }
            newBestBadge.localScale = Vector3.one * (slide * (1f + 0.07f * Mathf.Sin(now * 6f)));
            prompt.color = Palette.You.WithAlpha(age < promptDelay ? 0f : 0.55f + 0.45f * Mathf.Sin(now * 4f));
        }

        void Show(float now)
        {
            visible = true;
            shownAt = now;
            shownScore = -1;
            var session = context.Session;
            var metrics = context.Metrics;
            stats[0].text = Format.Clock(session.RunTime);
            stats[1].text = session.Level.ToString();
            stats[2].text = session.Folds.ToString();
            stats[3].text = "×" + session.BestChain;
            string firstLoop = metrics.FirstLoopTime < 0f ? "none" : metrics.FirstLoopTime.ToString("0.0") + "s";
            detail.text = $"Enemies folded {metrics.EnemiesFolded}   ·   Fold kills {metrics.FoldKillPercent}%   ·   First loop {firstLoop}";
            newBestBadge.gameObject.SetActive(session.NewBest);
        }
    }
}

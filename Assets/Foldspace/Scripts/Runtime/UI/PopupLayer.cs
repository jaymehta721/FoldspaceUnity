using System.Collections.Generic;
using Foldspace.Rendering;
using Foldspace.Utilities;
using UnityEngine;

namespace Foldspace.UI
{
    /// <summary>Floating "FOLD ×3" and "+75" callouts that track the world point they came from.</summary>
    public class PopupLayer : MonoBehaviour
    {
        [SerializeField] RectTransform layer;
        [SerializeField] PopupLabel labelPrefab;
        [SerializeField] float life = 0.95f;
        [SerializeField] float rise = 80f;
        [Tooltip("World units below the fold center for the points line.")]
        [SerializeField] float pointsOffset = 0.7f;

        readonly List<PopupLabel> active = new List<PopupLabel>();
        readonly Stack<PopupLabel> pool = new Stack<PopupLabel>();

        public void Init(GameContext ctx)
        {
            ctx.Events.FoldResolved += (_, fold) =>
            {
                if (fold.Empty) return;
                int chain = fold.Chain;
                Show(fold.Center, chain > 1 ? "FOLD ×" + chain : "FOLD", Palette.ChainColor(chain), 1f + 0.08f * Mathf.Min(chain, 6));
                if (fold.Points > 0) Show(fold.Center + Vector2.down * pointsOffset, "+" + fold.Points, Color.white, 0.65f);
            };
        }

        public void Show(Vector2 world, string text, Color color, float size = 1f)
        {
            var label = pool.Count > 0 ? pool.Pop() : Instantiate(labelPrefab, layer);
            label.gameObject.SetActive(true);
            label.Show(text, world, color, size, Time.unscaledTime);
            active.Add(label);
        }

        public void Clear()
        {
            foreach (var label in active) Recycle(label);
            active.Clear();
        }

        public void Tick(float now, Camera worldCamera, Camera uiCamera)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                float age = now - p.BornAt;
                if (age >= life)
                {
                    Recycle(p);
                    active.RemoveAt(i);
                    continue;
                }
                float k = age / life;
                float scale = age < 0.12f
                    ? Mathf.Lerp(0.3f, 1.3f, Ease.OutCubic(age / 0.12f))
                    : age < 0.26f ? Mathf.Lerp(1.3f, 1f, (age - 0.12f) / 0.14f) : 1f;
                Vector2 screen = worldCamera.WorldToScreenPoint(p.World);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, uiCamera, out var local);
                p.Rect.anchoredPosition = local + new Vector2(0f, rise * Ease.OutCubic(k));
                p.Rect.localScale = Vector3.one * scale;
                p.Text.color = p.Color.WithAlpha(k < 0.65f ? 1f : 1f - (k - 0.65f) / 0.35f);
            }
        }

        void Recycle(PopupLabel label)
        {
            label.gameObject.SetActive(false);
            pool.Push(label);
        }
    }
}

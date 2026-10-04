using UnityEngine;
using UnityEngine.UI;

namespace Foldspace.UI
{
    /// <summary>One floating world-space callout. Pooled by <see cref="PopupLayer"/>.</summary>
    [RequireComponent(typeof(Text))]
    public class PopupLabel : MonoBehaviour
    {
        [SerializeField] Text text;

        int baseFontSize;

        public RectTransform Rect => text.rectTransform;
        public Text Text => text;
        public Vector2 World { get; private set; }
        public Color Color { get; private set; }
        public float BornAt { get; private set; }

        void Awake() => baseFontSize = text.fontSize;

        public void Show(string message, Vector2 world, Color color, float size, float now)
        {
            text.text = message;
            text.fontSize = Mathf.RoundToInt(baseFontSize * size);
            World = world;
            Color = color;
            BornAt = now;
            Rect.localScale = Vector3.one * 0.3f;
        }
    }
}

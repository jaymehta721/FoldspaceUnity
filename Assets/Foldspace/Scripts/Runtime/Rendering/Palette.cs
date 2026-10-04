using UnityEngine;

namespace Foldspace.Rendering
{
    /// <summary>
    /// The game's color language: yellow is you, purple is threat, cyan is reward, green is hull, red is damage.
    /// </summary>
    public static class Palette
    {
        public static readonly Color You = Hex("FFE632");
        public static readonly Color YouHot = Hex("FFF7C2");
        public static readonly Color Halo = Hex("7A32FF");
        public static readonly Color HaloLight = Hex("A77BFF");
        public static readonly Color Ore = Hex("48C7FF");
        public static readonly Color Repair = Hex("5BD68A");
        public static readonly Color Danger = Hex("FF4D6D");
        public static readonly Color Heat = Hex("FF9F2E");
        public static readonly Color Pink = Hex("FF5AC8");

        public static readonly Color Space = Hex("04050C");
        public static readonly Color LensCenter = Hex("151A30");
        public static readonly Color LensEdge = Hex("080A14");
        public static readonly Color Rim = Hex("252A45");
        public static readonly Color RimLight = Hex("5B67A6");
        public static readonly Color Bezel = Hex("3A4170");
        public static readonly Color Silhouette = Hex("1A2146");

        public static readonly Color Panel = Hex("0C0F22");
        public static readonly Color PanelEdge = Hex("2E3562");
        public static readonly Color TextDim = new Color(0.78f, 0.82f, 1f, 0.62f);
        public static readonly Color TextShadow = new Color(0f, 0f, 0.03f, 0.65f);

        /// <summary>Fold chains heat up from yellow through orange to pink.</summary>
        public static Color ChainColor(int chain)
        {
            if (chain <= 1) return You;
            if (chain <= 4) return Color.Lerp(You, Heat, (chain - 1) / 3f);
            return Color.Lerp(Heat, Pink, Mathf.Clamp01((chain - 4) / 4f));
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }

        public static Color WithAlpha(this Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}

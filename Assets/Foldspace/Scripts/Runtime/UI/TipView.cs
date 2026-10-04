using UnityEngine;
using UnityEngine.UI;

namespace Foldspace.UI
{
    /// <summary>Onboarding tips: controls at the start of the first run, and a fold hint if the player hasn't folded yet.</summary>
    public class TipView : MonoBehaviour
    {
        const string DesktopControls = "Steer with the mouse or A / D   ·   Dash with Space or right-click";
        const string TouchControls = "Drag anywhere to steer   ·   Tap with a second finger to dash";
        const string FoldHint = "<color=#FFE632>TIP</color>   Loop around enemies and cross your own trail to <color=#FFE632>FOLD</color> them";

        [SerializeField] CanvasGroup group;
        [SerializeField] Text text;

        GameContext context;
        string shown;

        public void Init(GameContext ctx)
        {
            context = ctx;
            UiMotion.Hide(group);
        }

        public void Tick(float dt)
        {
            string tip = PickTip();
            if (tip != null && tip != shown)
            {
                shown = tip;
                text.text = tip;
            }
            UiMotion.Fade(group, tip != null ? 1f : 0f, dt, 4f);
        }

        string PickTip()
        {
            if (!context.IsPlaying) return null;
            var session = context.Session;
            var onboarding = context.Config.onboarding;
            if (session.RunId == 1 && session.RunTime < onboarding.controlsTipDuration)
                return context.Input.IsTouch ? TouchControls : DesktopControls;
            if (context.Metrics.FirstLoopTime < 0f && session.RunTime >= onboarding.foldHintDelay)
            {
                context.Metrics.MarkFoldHintShown();
                return FoldHint;
            }
            return null;
        }
    }
}

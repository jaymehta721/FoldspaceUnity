using UnityEngine;
using UnityEngine.UI;

namespace Foldspace.UI
{
    /// <summary>
    /// Root of the HUD prefab. Decides which screens are visible for the current <see cref="GameState"/>
    /// and drives the views. Drawn by its own camera after post-processing, so text stays crisp.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        public const int UiLayer = 5;
        static readonly Vector2 Landscape = new Vector2(1920f, 1080f);
        static readonly Vector2 Portrait = new Vector2(1080f, 1920f);

        [SerializeField] Canvas canvas;
        [SerializeField] CanvasScaler scaler;

        [Header("Views")]
        [SerializeField] RunHudView run;
        [SerializeField] TitleScreenView title;
        [SerializeField] CanvasGroup pause;
        [SerializeField] GameOverView gameOver;
        [SerializeField] BannerView banner;
        [SerializeField] TipView tip;
        [SerializeField] PopupLayer popups;

        GameContext context;
        int runId = -1;

        public Camera UiCamera => canvas.worldCamera;

        public void Init(GameContext ctx)
        {
            context = ctx;
            run.Init(ctx);
            title.Init(ctx);
            gameOver.Init(ctx);
            banner.Init(ctx);
            tip.Init(ctx);
            popups.Init(ctx);
            UiMotion.Hide(pause);
        }

        void LateUpdate()
        {
            if (context == null) return;
            float dt = Time.unscaledDeltaTime;
            float now = Time.unscaledTime;
            AdaptToOrientation();

            var state = context.State;
            if (context.Session.RunId != runId && state != GameState.Title)
            {
                runId = context.Session.RunId;
                run.ResetRun();
                popups.Clear();
                banner.Clear();
            }

            bool inRun = state != GameState.Title && context.Player != null;
            UiMotion.Fade(run.Group, inRun ? (state == GameState.GameOver ? 0.3f : 1f) : 0f, dt, 5f);
            if (inRun) run.Tick(now, dt);
            UiMotion.Fade(pause, state == GameState.Paused ? 1f : 0f, dt, 8f);
            title.Tick(state == GameState.Title, now, dt);
            gameOver.Tick(state == GameState.GameOver, now, dt);
            tip.Tick(dt);
            banner.Tick(now);
            popups.Tick(now, context.WorldCamera, UiCamera);
        }

        void AdaptToOrientation()
        {
            var cam = UiCamera;
            if (cam == null) return;
            bool portrait = cam.pixelHeight > cam.pixelWidth;
            var reference = portrait ? Portrait : Landscape;
            if (scaler.referenceResolution == reference) return;
            scaler.referenceResolution = reference;
            scaler.matchWidthOrHeight = portrait ? 0f : 1f;
        }
    }
}

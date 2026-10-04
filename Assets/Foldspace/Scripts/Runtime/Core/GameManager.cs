using Foldspace.Config;
using Foldspace.Controls;
using Foldspace.Environment;
using Foldspace.Feedback;
using Foldspace.Gameplay;
using Foldspace.UI;
using UnityEngine;

namespace Foldspace
{
    /// <summary>
    /// Composition root and game flow. Builds the <see cref="GameContext"/>, initializes every system in a fixed
    /// order (so nothing depends on Awake order), and runs the Title → Playing ⇄ Paused → GameOver state machine.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        const float RestartDelay = 1.2f;

        [SerializeField] GameConfig config;
        [Tooltip("Start a run immediately instead of showing the title screen.")]
        [SerializeField] bool skipTitle;

        [Header("Systems")]
        [SerializeField] InputReader input;
        [SerializeField] EntitySpawner spawner;
        [SerializeField] Director director;
        [SerializeField] GameFeel feel;
        [SerializeField] Vfx vfx;
        [SerializeField] AudioManager audioManager;

        [Header("Scene")]
        [SerializeField] Camera worldCamera;
        [SerializeField] CameraRig cameraRig;
        [SerializeField] Arena arena;
        [SerializeField] Backdrop backdrop;
        [SerializeField] TitleDemo titleDemo;
        [SerializeField] HudController hud;

        GameContext context;
        float gameOverAt;
        bool initialized;

        public GameContext Context => context;
        public GameState State => context != null ? context.State : GameState.Title;
        public GameConfig Config => context != null ? context.Config : config;
        public Camera WorldCamera => worldCamera;
        public Camera UiCamera => hud.UiCamera;

        void Awake()
        {
            if (!HasReferences())
            {
                enabled = false;
                return;
            }

            context = new GameContext
            {
                Config = config,
                Input = input,
                Spawner = spawner,
                Vfx = vfx,
                Audio = audioManager,
                Arena = arena,
                WorldCamera = worldCamera,
            };

            input.Init(worldCamera);
            audioManager.Init();
            vfx.Init();
            arena.Init(config.arena.radius);
            backdrop.Init(worldCamera);
            cameraRig.Init(context);
            spawner.Init(context);
            director.Init(context);

            // Gameplay listeners subscribe first so feedback and UI always see the updated state.
            context.Rules = new RunRules(context);
            _ = new FoldResolver(context);
            context.Metrics.Bind(context.Events, () => context.Session.RunTime);
            feel.Init(context);
            hud.Init(context);

            context.Events.PlayerDied += OnPlayerDied;
            initialized = true;
        }

        void Start()
        {
            if (!initialized) return;
            if (skipTitle) StartRun();
            else ShowTitle();
        }

        void OnDestroy() => Time.timeScale = 1f;

        void OnApplicationFocus(bool focused)
        {
            if (!focused && Application.isMobilePlatform && State == GameState.Playing) SetPaused(true);
        }

        /// <summary>Swaps the config for the next run. Tests use this with tuned copies of the shipped asset.</summary>
        public void Configure(GameConfig next)
        {
            context.Config = next != null ? next : config;
        }

        public void ShowTitle()
        {
            ClearRun();
            SetState(GameState.Title);
            titleDemo.Play(context);
        }

        public void StartRun()
        {
            ClearRun();
            titleDemo.Stop();
            var cfg = context.Config;
            arena.EnsureRadius(cfg.arena.radius);
            context.Session.Begin(cfg);
            context.Metrics.Begin();
            var player = spawner.SpawnPlayer(cfg.ship.spawnPosition);
            director.BeginRun();
            SetState(GameState.Playing);
            context.Events.RaiseRunStarted(player);
        }

        void ClearRun()
        {
            director.EndRun();
            spawner.DespawnAll();
            feel.ResetEffects();
            context.Clock.Reset();
        }

        void SetPaused(bool paused)
        {
            if (paused && State == GameState.Playing) SetState(GameState.Paused);
            else if (!paused && State == GameState.Paused) SetState(GameState.Playing);
        }

        void SetState(GameState next)
        {
            context.State = next;
            context.Clock.SetState(next);
            context.Events.RaiseStateChanged(next);
        }

        void OnPlayerDied(Vector2 at)
        {
            gameOverAt = Time.unscaledTime;
            director.EndRun();
            if (context.HighScores.Submit(context.Session.Score)) context.Session.MarkNewBest();
            SetState(GameState.GameOver);
            Debug.Log(context.Metrics.Summary(context.Session));
        }

        void Update()
        {
            switch (State)
            {
                case GameState.Title:
                    if (input.LaunchPressed()) StartRun();
                    break;
                case GameState.Playing:
                    if (input.PausePressed())
                    {
                        SetPaused(true);
                        break;
                    }
                    context.Session.Tick(Time.deltaTime, Time.time);
                    break;
                case GameState.Paused:
                    if (input.PausePressed()) SetPaused(false);
                    break;
                case GameState.GameOver:
                    if (Time.unscaledTime > gameOverAt + RestartDelay && input.LaunchPressed()) StartRun();
                    break;
            }
            context.Clock.Tick();
        }

        bool HasReferences()
        {
            bool ok = true;
            void Check(Object reference, string label)
            {
                if (reference != null) return;
                Debug.LogError($"[Foldspace] GameManager is missing its {label} reference. Assign it in the Inspector.", this);
                ok = false;
            }

            Check(config, "config");
            if (config != null) Check(config.director, "config's director profile");
            Check(input, "input");
            Check(spawner, "spawner");
            Check(director, "director");
            Check(feel, "game feel");
            Check(vfx, "vfx");
            Check(audioManager, "audio");
            Check(worldCamera, "world camera");
            Check(cameraRig, "camera rig");
            Check(arena, "arena");
            Check(backdrop, "backdrop");
            Check(titleDemo, "title demo");
            Check(hud, "hud");
            return ok;
        }
    }
}

using Foldspace.Config;
using Foldspace.Controls;
using Foldspace.Environment;
using Foldspace.Feedback;
using Foldspace.Gameplay;
using UnityEngine;

namespace Foldspace
{
    /// <summary>
    /// The services a run is made of, built once by <see cref="GameManager"/> and handed to every system and entity.
    /// Passing this around (instead of a singleton) keeps dependencies visible and lets tests swap the config.
    /// </summary>
    public sealed class GameContext
    {
        public GameConfig Config { get; internal set; }
        public GameState State { get; internal set; } = GameState.Title;

        public GameEvents Events { get; } = new GameEvents();
        public RunSession Session { get; } = new RunSession();
        public RunMetrics Metrics { get; } = new RunMetrics();
        public HighScoreStore HighScores { get; } = new HighScoreStore();
        public EntityRegistry Registry { get; } = new EntityRegistry();
        public TimeController Clock { get; } = new TimeController();

        public RunRules Rules { get; internal set; }
        public EntitySpawner Spawner { get; internal set; }
        public InputReader Input { get; internal set; }
        public Vfx Vfx { get; internal set; }
        public AudioManager Audio { get; internal set; }
        public Arena Arena { get; internal set; }
        public Camera WorldCamera { get; internal set; }

        public bool IsPlaying => State == GameState.Playing;
        public PlayerShip Player => Registry.Player;

        /// <summary>Keeps a circle of <paramref name="radius"/> inside the arena.</summary>
        public Vector2 ClampToArena(Vector2 position, float radius)
        {
            float limit = Config.arena.radius - radius;
            return position.sqrMagnitude > limit * limit ? position.normalized * limit : position;
        }
    }
}

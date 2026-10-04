using UnityEngine;

namespace Foldspace
{
    /// <summary>The only thing that writes <see cref="Time.timeScale"/>: hitstop, slow motion, pause and the run-over slowdown.</summary>
    public sealed class TimeController
    {
        const float GameOverStartScale = 0.3f;
        const float GameOverSettleScale = 0.6f;
        const float GameOverSettleRate = 0.25f;

        GameState state = GameState.Title;
        float hitstopUntil;
        float slowMotionUntil;
        float slowMotionScale = 1f;

        /// <summary>Freezes gameplay for a moment. Ignored outside a run.</summary>
        public void Hitstop(float seconds)
        {
            if (state != GameState.Playing || seconds <= 0f) return;
            hitstopUntil = Mathf.Max(hitstopUntil, Time.unscaledTime + seconds);
            Time.timeScale = 0f;
        }

        /// <summary>Slows gameplay for <paramref name="seconds"/>, starting after any hitstop that's already running.</summary>
        public void SlowMotion(float seconds, float scale)
        {
            if (state != GameState.Playing || seconds <= 0f) return;
            slowMotionUntil = Mathf.Max(hitstopUntil, Time.unscaledTime) + seconds;
            slowMotionScale = Mathf.Clamp(scale, 0.05f, 1f);
        }

        public void Reset()
        {
            hitstopUntil = 0f;
            slowMotionUntil = 0f;
            Time.timeScale = 1f;
        }

        public void SetState(GameState next)
        {
            state = next;
            switch (next)
            {
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.GameOver:
                    Time.timeScale = GameOverStartScale;
                    break;
                default:
                    Tick();
                    break;
            }
        }

        public void Tick()
        {
            float now = Time.unscaledTime;
            switch (state)
            {
                case GameState.Playing:
                    Time.timeScale = now < hitstopUntil ? 0f : now < slowMotionUntil ? slowMotionScale : 1f;
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.GameOver:
                    Time.timeScale = Mathf.MoveTowards(Time.timeScale, GameOverSettleScale, Time.unscaledDeltaTime * GameOverSettleRate);
                    break;
                default:
                    Time.timeScale = 1f;
                    break;
            }
        }
    }
}

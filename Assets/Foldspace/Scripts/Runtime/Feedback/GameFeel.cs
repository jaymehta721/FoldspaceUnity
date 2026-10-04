using System.Collections.Generic;
using Foldspace.Environment;
using Foldspace.Gameplay;
using Foldspace.Rendering;
using UnityEngine;

namespace Foldspace.Feedback
{
    /// <summary>
    /// All the juice in one place. Listens to <see cref="GameEvents"/> and turns each gameplay moment into
    /// camera shake, post-processing pulses, particles, grid ripples, hitstop and sound.
    /// </summary>
    public class GameFeel : MonoBehaviour
    {
        [SerializeField] CameraRig cameraRig;
        [SerializeField] PostFx postFx;

        [Header("Intensity")]
        [Range(0f, 2f)] [SerializeField] float shakeScale = 1f;
        [Range(0f, 2f)] [SerializeField] float hitstopScale = 1f;

        [Header("Fold hitstop")]
        [SerializeField] float foldHitstopBase = 0.07f;
        [SerializeField] float foldHitstopPerEnemy = 0.01f;
        [SerializeField] float foldHitstopMax = 0.14f;
        [Tooltip("Folds that catch at least this many enemies also get a moment of slow motion.")]
        [SerializeField] int bigFoldCount = 5;
        [SerializeField] float bigFoldSlowMotion = 0.35f;
        [SerializeField] float slowMotionScale = 0.45f;

        [Header("Ship hit")]
        [SerializeField] float playerHitHitstop = 0.08f;

        GameContext context;
        Vfx vfx;
        AudioManager audioManager;
        Arena arena;

        SpaceGrid Grid => arena.Grid;

        public void Init(GameContext ctx)
        {
            context = ctx;
            vfx = ctx.Vfx;
            audioManager = ctx.Audio;
            arena = ctx.Arena;

            var events = ctx.Events;
            events.RunStarted += OnRunStarted;
            events.FoldResolved += OnFoldResolved;
            events.SpawnTelegraphed += OnSpawnTelegraphed;
            events.EnemySpawned += OnEnemySpawned;
            events.EnemyKilled += OnEnemyKilled;
            events.PlayerHit += OnPlayerHit;
            events.PlayerDied += OnPlayerDied;
            events.PlayerDashed += OnPlayerDashed;
            events.RimBounced += OnRimBounced;
            events.BulletFired += at => vfx.Glow(at, Palette.You, 0.45f, 0.08f);
            events.BulletHit += OnBulletHit;
            events.BulletExpired += at => vfx.Glow(at, Palette.You, 0.3f, 0.1f);
            events.BulletDeflected += OnBulletDeflected;
            events.ShardCollected += OnShardCollected;
            events.LevelUp += OnLevelUp;
            vfx.FoldPopped += OnFoldPopped;
        }

        /// <summary>Drops every lingering effect, for a clean start.</summary>
        public void ResetEffects()
        {
            vfx.Clear();
            postFx.ResetPulses();
            cameraRig.Calm();
            Grid.Calm();
        }

        void LateUpdate()
        {
            if (context == null) return;
            var player = context.Player;
            postFx.LowHull = context.IsPlaying && player != null && player.Alive && player.Hull == 1 && context.Config.ship.hullPips > 1;
            arena.SetXp(context.State == GameState.Title ? 0f : context.Session.XpFraction);
        }

        void Shake(float trauma) => cameraRig.Shake(trauma * shakeScale);
        void Kick(Vector2 impulse) => cameraRig.Kick(impulse * shakeScale);
        void Hitstop(float seconds) => context.Clock.Hitstop(seconds * hitstopScale);

        void OnRunStarted(PlayerShip player)
        {
            Vector2 at = player.Position;
            vfx.Ring(at, Palette.You, 0.1f, 1.6f, 0.55f);
            vfx.Glow(at, Palette.YouHot, 1.8f, 0.35f, true);
            Grid.Impulse(at, 2.8f, 3.5f);
            postFx.Shockwave(at, 0.7f);
            postFx.Flash(Palette.YouHot, 0.12f);
            audioManager.Play(SfxId.Launch, 1f, 0.8f);
        }

        void OnFoldResolved(IReadOnlyList<Vector2> loop, FoldResult fold)
        {
            vfx.PlayFold(loop, !fold.Empty);
            Grid.Impulse(fold.Center, fold.Size * 2.2f + 0.8f, fold.Empty ? -1.8f : -5f);
            if (fold.Empty)
            {
                audioManager.Play(SfxId.FoldEmpty, 1f, 0.6f);
                return;
            }

            int n = fold.Caught;
            Shake(0.22f + 0.05f * n);
            cameraRig.Zoom(Mathf.Min(0.08f, 0.03f + 0.01f * n), fold.Center);
            postFx.Aberration(0.012f + 0.003f * n);
            postFx.Flash(Palette.YouHot, Mathf.Min(0.18f, 0.05f + 0.02f * n));
            audioManager.PlayFold(fold.Chain);
            audioManager.Play(SfxId.Implode, 1f, 0.8f);
            Hitstop(Mathf.Min(foldHitstopMax, foldHitstopBase + foldHitstopPerEnemy * n));
            if (n >= bigFoldCount) context.Clock.SlowMotion(bigFoldSlowMotion, slowMotionScale);
        }

        void OnFoldPopped(Vector2 center, float size, bool caught)
        {
            Grid.Impulse(center, size * 2.5f + 1.2f, caught ? 6f : 1.5f);
            if (!caught) return;
            postFx.Shockwave(center, 1f, 1.3f, 0.6f);
            Kick(Random.insideUnitCircle * 0.05f);
        }

        void OnSpawnTelegraphed(Vector2 at, float duration)
        {
            vfx.PlayTelegraph(at, duration);
            audioManager.Play(SfxId.Warn, 1f, 0.4f);
        }

        void OnEnemySpawned(Enemy enemy)
        {
            vfx.Ring(enemy.Position, Palette.HaloLight, 0.05f, 0.6f, 0.3f, false);
            audioManager.Play(SfxId.Pop, Random.Range(0.85f, 1.2f), 0.2f);
        }

        void OnEnemyKilled(Enemy enemy, DamageSource source)
        {
            Vector2 p = enemy.Position;
            switch (source)
            {
                case DamageSource.Fold:
                    return;
                case DamageSource.Contact:
                    vfx.Sparks(p, Palette.HaloLight, 8, 5f);
                    vfx.Glow(p, Palette.Halo, 1.2f, 0.25f);
                    return;
            }
            vfx.Sparks(p, Palette.HaloLight, 10, 5.5f);
            vfx.Glow(p, Palette.Halo, 1.3f, 0.22f);
            vfx.Ring(p, Palette.HaloLight, 0.1f, 0.75f, 0.3f, false);
            Grid.Impulse(p, 1.1f, 1.6f);
            Shake(0.05f);
            audioManager.Play(SfxId.Kill, Random.Range(0.9f, 1.1f), 0.6f);
        }

        void OnPlayerHit(Vector2 at)
        {
            Shake(0.55f);
            Kick(Random.insideUnitCircle.normalized * 0.15f);
            postFx.Aberration(0.03f);
            postFx.Hurt(0.75f);
            postFx.Shockwave(at, -0.8f, 1.6f, 0.5f);
            vfx.Sparks(at, Color.white, 14, 7f, 0.3f, true);
            vfx.Ring(at, Palette.Danger, 0.2f, 1.6f, 0.4f);
            vfx.Glow(at, Palette.Danger, 2f, 0.3f, true);
            arena.FlashRim(Palette.Danger);
            Grid.Impulse(at, 2.2f, 4f);
            audioManager.Play(SfxId.Hit);
            Hitstop(playerHitHitstop);
        }

        void OnPlayerDied(Vector2 at)
        {
            vfx.Sparks(at, Palette.YouHot, 30, 9f, 0.45f, true);
            vfx.Burst(at, Palette.You, 18, 4f, 0.25f);
            vfx.Ring(at, Palette.You, 0.2f, 3f, 0.8f);
            vfx.Ring(at, Color.white, 0.1f, 1.8f, 0.5f);
            vfx.Glow(at, Palette.YouHot, 3.5f, 0.5f, true);
            Shake(0.9f);
            cameraRig.Zoom(0.1f, at);
            postFx.Flash(Color.white, 0.45f);
            postFx.Shockwave(at, 1.6f, 1.2f, 0.9f);
            postFx.Aberration(0.04f);
            postFx.LowHull = false;
            Grid.Impulse(at, 4f, 9f);
            audioManager.Play(SfxId.Death);
        }

        void OnPlayerDashed(Vector2 at, Vector2 forward)
        {
            vfx.Ring(at, Palette.YouHot, 0.2f, 1.1f, 0.3f, false);
            postFx.Aberration(0.012f);
            postFx.Shockwave(at, 0.4f, 1.6f, 0.35f);
            Kick(forward * 0.1f);
            audioManager.Play(SfxId.Dash, 1f, 0.7f);
        }

        void OnRimBounced(Vector2 at)
        {
            arena.Bump(at);
            Grid.Impulse(at, 1.2f, 2.2f);
            vfx.Sparks(at, Color.white, 6, 3.5f, 0.16f);
            Shake(0.08f);
            Kick(at.normalized * 0.05f);
            audioManager.Play(SfxId.Bump, Random.Range(0.9f, 1.1f), 0.5f);
        }

        void OnBulletHit(Vector2 at)
        {
            vfx.Sparks(at, Palette.You, 4, 3.5f, 0.18f);
            vfx.Glow(at, Palette.You, 0.5f, 0.12f);
        }

        void OnBulletDeflected(Vector2 at)
        {
            vfx.Sparks(at, Palette.HaloLight, 4, 3.5f, 0.14f);
            vfx.Ring(at, Palette.HaloLight, 0.05f, 0.35f, 0.18f, false);
            audioManager.Play(SfxId.Deflect, Random.Range(0.9f, 1.15f), 0.5f);
        }

        void OnShardCollected(Vector2 at, int combo)
        {
            audioManager.Play(SfxId.Shard, 1f + 0.045f * Mathf.Min(combo, 16), 0.35f);
            vfx.Glow(at, Palette.Ore, 0.7f, 0.12f);
        }

        void OnLevelUp(int level, Vector2 at)
        {
            arena.Pulse();
            Grid.Impulse(at, 3.5f, 4f);
            vfx.Ring(at, Palette.Ore, 0.2f, 2.4f, 0.6f);
            postFx.Flash(Palette.Ore, 0.08f);
            postFx.Shockwave(at, 0.7f);
            audioManager.Play(SfxId.LevelUp);
        }
    }
}

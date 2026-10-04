using System;
using System.Collections.Generic;
using Foldspace.Gameplay;
using UnityEngine;

namespace Foldspace
{
    /// <summary>Outcome of a closed loop. <see cref="Caught"/> is 0 for an empty fold.</summary>
    public readonly struct FoldResult
    {
        public readonly Vector2 Center;
        public readonly float Size;
        public readonly int Caught;
        public readonly int Killed;
        public readonly int Chain;
        public readonly int Points;

        public FoldResult(Vector2 center, float size, int caught, int killed, int chain, int points)
        {
            Center = center;
            Size = size;
            Caught = caught;
            Killed = killed;
            Chain = chain;
            Points = points;
        }

        public bool Empty => Caught == 0;
    }

    /// <summary>
    /// Messages from gameplay to everything that reacts to it. Gameplay raises them; feedback, UI and metrics listen,
    /// so the simulation never calls into presentation code.
    /// </summary>
    public sealed class GameEvents
    {
        public event Action<GameState> StateChanged;
        public event Action<PlayerShip> RunStarted;

        public event Action<IReadOnlyList<Vector2>> LoopClosed;
        public event Action<IReadOnlyList<Vector2>, FoldResult> FoldResolved;

        public event Action<Vector2, float> SpawnTelegraphed;
        public event Action<string, string, Color> Announced;
        public event Action<Enemy> EnemySpawned;
        public event Action<Enemy, DamageSource> EnemyKilled;

        public event Action<Vector2> PlayerHit;
        public event Action<Vector2> PlayerDied;
        public event Action<Vector2, Vector2> PlayerDashed;
        public event Action<Vector2> RimBounced;

        public event Action<Vector2> BulletFired;
        public event Action<Vector2> BulletHit;
        public event Action<Vector2> BulletExpired;
        public event Action<Vector2> BulletDeflected;

        public event Action<Vector2, int> ShardCollected;
        public event Action<int, Vector2> LevelUp;

        internal void RaiseStateChanged(GameState state) => StateChanged?.Invoke(state);
        internal void RaiseRunStarted(PlayerShip player) => RunStarted?.Invoke(player);

        internal void RaiseLoopClosed(IReadOnlyList<Vector2> loop) => LoopClosed?.Invoke(loop);
        internal void RaiseFoldResolved(IReadOnlyList<Vector2> loop, FoldResult result) => FoldResolved?.Invoke(loop, result);

        internal void RaiseSpawnTelegraphed(Vector2 at, float duration) => SpawnTelegraphed?.Invoke(at, duration);
        internal void RaiseAnnounced(string title, string subtitle, Color color) => Announced?.Invoke(title, subtitle, color);
        internal void RaiseEnemySpawned(Enemy enemy) => EnemySpawned?.Invoke(enemy);
        internal void RaiseEnemyKilled(Enemy enemy, DamageSource source) => EnemyKilled?.Invoke(enemy, source);

        internal void RaisePlayerHit(Vector2 at) => PlayerHit?.Invoke(at);
        internal void RaisePlayerDied(Vector2 at) => PlayerDied?.Invoke(at);
        internal void RaisePlayerDashed(Vector2 at, Vector2 forward) => PlayerDashed?.Invoke(at, forward);
        internal void RaiseRimBounced(Vector2 at) => RimBounced?.Invoke(at);

        internal void RaiseBulletFired(Vector2 muzzle) => BulletFired?.Invoke(muzzle);
        internal void RaiseBulletHit(Vector2 at) => BulletHit?.Invoke(at);
        internal void RaiseBulletExpired(Vector2 at) => BulletExpired?.Invoke(at);
        internal void RaiseBulletDeflected(Vector2 at) => BulletDeflected?.Invoke(at);

        internal void RaiseShardCollected(Vector2 at, int combo) => ShardCollected?.Invoke(at, combo);
        internal void RaiseLevelUp(int level, Vector2 at) => LevelUp?.Invoke(level, at);
    }
}

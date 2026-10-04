using System.Collections.Generic;
using Foldspace.Config;
using Foldspace.Utilities;
using UnityEngine;

namespace Foldspace.Gameplay
{
    /// <summary>Everything alive in the current run, plus the spatial queries gameplay needs. Maintained by <see cref="EntitySpawner"/>.</summary>
    public sealed class EntityRegistry
    {
        const float MaxSeparation = 3f;

        readonly List<Enemy> enemies = new List<Enemy>();
        readonly List<Shard> shards = new List<Shard>();

        public PlayerShip Player { get; internal set; }
        public IReadOnlyList<Enemy> Enemies => enemies;
        public IReadOnlyList<Shard> Shards => shards;

        internal void Add(Enemy enemy) => enemies.Add(enemy);
        internal void Remove(Enemy enemy) => enemies.Remove(enemy);
        internal void Add(Shard shard) => shards.Add(shard);
        internal void Remove(Shard shard) => shards.Remove(shard);

        internal void Clear()
        {
            enemies.Clear();
            shards.Clear();
            Player = null;
        }

        public int CountAlive(EnemyDefinition definition)
        {
            int count = 0;
            foreach (var e in enemies)
                if (!e.Dying && e.Definition == definition) count++;
            return count;
        }

        public Enemy FindEnemyAt(Vector2 point, float radius)
        {
            foreach (var e in enemies)
            {
                if (e.Dying) continue;
                float r = e.Radius + radius;
                if ((e.Position - point).sqrMagnitude <= r * r) return e;
            }
            return null;
        }

        /// <summary>Closest bullet-vulnerable enemy inside the aim cone, or null.</summary>
        public Enemy FindAutoAimTarget(Vector2 from, Vector2 forward, float cone, float range)
        {
            Enemy best = null;
            float bestSq = range * range;
            foreach (var e in enemies)
            {
                if (e.Dying || e.Definition.bulletProof) continue;
                Vector2 d = e.Position - from;
                float sq = d.sqrMagnitude;
                if (sq > bestSq || Vector2.Angle(forward, d) > cone) continue;
                best = e;
                bestSq = sq;
            }
            return best;
        }

        /// <summary>Another enemy whose shield covers <paramref name="target"/>, or null.</summary>
        public Enemy FindShieldFor(Enemy target)
        {
            foreach (var e in enemies)
            {
                float r = e.Definition.shieldRadius;
                if (e == target || e.Dying || r <= 0f) continue;
                if ((e.Position - target.Position).sqrMagnitude <= r * r) return e;
            }
            return null;
        }

        /// <summary>Push away from nearby enemies, stronger the closer they are.</summary>
        public Vector2 Separation(Enemy self, float radius)
        {
            Vector2 push = Vector2.zero;
            float rSq = radius * radius;
            foreach (var e in enemies)
            {
                if (e == self) continue;
                Vector2 d = self.Position - e.Position;
                float sq = d.sqrMagnitude;
                if (sq >= rSq || sq < 1e-6f) continue;
                push += d / sq * radius;
            }
            return Vector2.ClampMagnitude(push, MaxSeparation);
        }

        public void EnemiesInside(IReadOnlyList<Vector2> loop, List<Enemy> results)
        {
            results.Clear();
            foreach (var e in enemies)
                if (!e.Dying && Geometry.PointInPolygon(e.Position, loop)) results.Add(e);
        }

        public void AttractShardsInside(IReadOnlyList<Vector2> loop)
        {
            foreach (var s in shards)
                if (Geometry.PointInPolygon(s.Position, loop)) s.Attract();
        }
    }
}

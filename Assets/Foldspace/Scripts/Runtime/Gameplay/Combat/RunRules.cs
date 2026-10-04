using UnityEngine;

namespace Foldspace.Gameplay
{
    /// <summary>Rewards and consequences: kill drops and score, shard pickups and level-ups, and the knockback when the ship is hit.</summary>
    public sealed class RunRules
    {
        const float HitKnockbackRadius = 1.5f;
        const float HitKnockback = 4f;

        readonly GameContext context;

        public RunRules(GameContext ctx)
        {
            context = ctx;
            ctx.Events.EnemyKilled += OnEnemyKilled;
            ctx.Events.PlayerHit += OnPlayerHit;
        }

        /// <summary>Folded enemies drop extra shards (multiplied by the chain) that fly straight to the ship.</summary>
        void OnEnemyKilled(Enemy enemy, DamageSource source)
        {
            if (source == DamageSource.Contact) return;
            var definition = enemy.Definition;
            bool folded = source == DamageSource.Fold;
            int count = folded
                ? Mathf.Min(context.Config.progression.maxShardsPerFold, definition.shardValue * Mathf.Max(1, context.Session.Chain))
                : definition.shardValue;
            context.Spawner.SpawnShards(enemy.Position, count, folded);
            if (!folded) context.Session.AddScore(definition.scoreValue);
        }

        void OnPlayerHit(Vector2 at)
        {
            float rSq = HitKnockbackRadius * HitKnockbackRadius;
            foreach (var e in context.Registry.Enemies)
            {
                Vector2 away = e.Position - at;
                if (away.sqrMagnitude < rSq) e.Push(away.normalized * HitKnockback);
            }
        }

        public void CollectShard(Shard shard)
        {
            context.Spawner.Despawn(shard);
            var player = context.Player;
            if (!context.IsPlaying || player == null) return;

            var session = context.Session;
            int combo = session.CollectShard(Time.time);
            context.Events.RaiseShardCollected(player.Position, combo);
            int gained = session.ApplyLevelUps(context.Config.progression);
            for (int i = gained - 1; i >= 0; i--)
                context.Events.RaiseLevelUp(session.Level - i, player.Position);
        }
    }
}

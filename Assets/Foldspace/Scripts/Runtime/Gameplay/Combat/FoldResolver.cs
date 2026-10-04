using System.Collections.Generic;
using Foldspace.Utilities;
using UnityEngine;

namespace Foldspace.Gameplay
{
    /// <summary>The core mechanic: a closed wake loop folds every enemy inside it, and quick successive folds build a chain.</summary>
    public sealed class FoldResolver
    {
        readonly GameContext context;
        readonly List<Enemy> caught = new List<Enemy>();

        public FoldResolver(GameContext ctx)
        {
            context = ctx;
            ctx.Events.LoopClosed += Resolve;
        }

        void Resolve(IReadOnlyList<Vector2> loop)
        {
            if (!context.IsPlaying) return;
            Vector2 center = Geometry.Centroid(loop);
            float size = Mathf.Sqrt(Mathf.Abs(Geometry.SignedArea(loop)) / Mathf.PI);

            context.Registry.EnemiesInside(loop, caught);
            context.Registry.AttractShardsInside(loop);
            if (caught.Count == 0)
            {
                context.Events.RaiseFoldResolved(loop, new FoldResult(center, size, 0, 0, 0, 0));
                return;
            }

            var session = context.Session;
            var fold = context.Config.fold;
            int chain = session.RegisterFold(Time.time);
            int killed = 0;
            foreach (var e in caught)
                if (e.TakeDamage(fold.damage, DamageSource.Fold, center)) killed++;
            int points = session.ScoreFold(killed, fold.pointsPerKill);
            context.Events.RaiseFoldResolved(loop, new FoldResult(center, size, caught.Count, killed, chain, points));
            caught.Clear();
        }
    }
}

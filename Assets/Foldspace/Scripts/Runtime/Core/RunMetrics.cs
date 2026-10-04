using System;
using System.Collections.Generic;
using System.Text;
using Foldspace.Gameplay;
using Foldspace.Utilities;
using UnityEngine;

namespace Foldspace
{
    /// <summary>
    /// Playtest numbers for the milestone's exit test: do players fold unprompted, and does folding beat shooting?
    /// Fed entirely by <see cref="GameEvents"/>; the summary is logged when a run ends.
    /// </summary>
    public sealed class RunMetrics
    {
        readonly int[] killsBySource = new int[Enum.GetValues(typeof(DamageSource)).Length];
        Func<float> runTime = () => 0f;

        public float FirstLoopTime { get; private set; } = -1f;
        public float FoldHintShownAt { get; private set; } = -1f;
        public int EnemiesSpawned { get; private set; }
        public int HitsTaken { get; private set; }
        public int LoopsClosed { get; private set; }
        public int EmptyLoops { get; private set; }
        public int EnemiesFolded { get; private set; }
        public int Deflections { get; private set; }

        public void Bind(GameEvents events, Func<float> currentRunTime)
        {
            runTime = currentRunTime;
            events.FoldResolved += OnFold;
            events.EnemySpawned += _ => EnemiesSpawned++;
            events.EnemyKilled += (_, source) => killsBySource[(int)source]++;
            events.PlayerHit += _ => HitsTaken++;
            events.BulletDeflected += _ => Deflections++;
        }

        public void Begin()
        {
            FirstLoopTime = -1f;
            FoldHintShownAt = -1f;
            EnemiesSpawned = 0;
            HitsTaken = 0;
            LoopsClosed = 0;
            EmptyLoops = 0;
            EnemiesFolded = 0;
            Deflections = 0;
            Array.Clear(killsBySource, 0, killsBySource.Length);
        }

        public int KillsBy(DamageSource source) => killsBySource[(int)source];

        /// <summary>Share of non-contact kills that came from folds, 0 to 100.</summary>
        public int FoldKillPercent
        {
            get
            {
                int fold = KillsBy(DamageSource.Fold);
                int total = fold + KillsBy(DamageSource.Bullet) + KillsBy(DamageSource.Wake);
                return total > 0 ? 100 * fold / total : 0;
            }
        }

        public void MarkFoldHintShown()
        {
            if (FoldHintShownAt < 0f) FoldHintShownAt = runTime();
        }

        void OnFold(IReadOnlyList<Vector2> loop, FoldResult result)
        {
            LoopsClosed++;
            if (result.Empty) EmptyLoops++;
            EnemiesFolded += result.Caught;
            if (FirstLoopTime >= 0f) return;
            FirstLoopTime = runTime();
            Debug.Log($"[Foldspace] First loop closed at {FirstLoopTime:F1}s, {(FoldHintShownAt < 0f ? "before" : "after")} the fold tip appeared.");
        }

        public string Summary(RunSession session)
        {
            int fold = KillsBy(DamageSource.Fold);
            int bullet = KillsBy(DamageSource.Bullet);
            int wake = KillsBy(DamageSource.Wake);
            int total = Mathf.Max(1, fold + bullet + wake);
            int folds = LoopsClosed - EmptyLoops;

            var sb = new StringBuilder();
            sb.AppendLine("[Foldspace] Run over");
            sb.AppendLine($"  Survived {Format.Clock(session.RunTime)}, score {session.Score}, level {session.Level}");
            sb.AppendLine($"  Loops closed: {LoopsClosed} ({EmptyLoops} empty). First loop: {(FirstLoopTime < 0f ? "never" : FirstLoopTime.ToString("F1") + "s")}");
            sb.AppendLine($"  Best chain: {session.BestChain}. Enemies per fold: {(folds > 0 ? (EnemiesFolded / (float)folds).ToString("F1") : "-")}");
            sb.AppendLine($"  Kills: fold {fold} ({100 * fold / total}%), bullets {bullet} ({100 * bullet / total}%), wake {wake} ({100 * wake / total}%)");
            sb.Append($"  Hits taken: {HitsTaken}. Enemies spawned: {EnemiesSpawned}");
            return sb.ToString();
        }
    }
}

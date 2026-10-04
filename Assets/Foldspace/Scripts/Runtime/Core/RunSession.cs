using Foldspace.Config;
using UnityEngine;

namespace Foldspace
{
    /// <summary>
    /// Score, XP and fold-chain state for one run. Plain C# with times passed in, so it's unit-testable without a scene.
    /// </summary>
    public sealed class RunSession
    {
        const float ShardComboWindow = 0.35f;

        float lastFoldTime = float.NegativeInfinity;
        float lastShardTime = float.NegativeInfinity;

        public int RunId { get; private set; }
        public int Score { get; private set; }
        public int Level { get; private set; } = 1;
        public int Xp { get; private set; }
        public int XpToNext { get; private set; } = 1;
        public int Chain { get; private set; }
        public int BestChain { get; private set; }
        public int Folds { get; private set; }
        public int ShardCombo { get; private set; }
        public float RunTime { get; private set; }
        public bool NewBest { get; private set; }
        public float ChainWindow { get; private set; } = 1f;
        public float ChainExpiresAt { get; private set; }

        public float XpFraction => XpToNext > 0 ? (float)Xp / XpToNext : 0f;

        /// <summary>0 to 1: how much of the chain window is left at <paramref name="now"/>.</summary>
        public float ChainRemaining(float now) => Chain > 0 ? Mathf.Clamp01((ChainExpiresAt - now) / ChainWindow) : 0f;

        public void Begin(GameConfig config)
        {
            RunId++;
            Score = 0;
            Level = 1;
            Xp = 0;
            XpToNext = config.progression.XpForLevel(1);
            Chain = 0;
            BestChain = 0;
            Folds = 0;
            ShardCombo = 0;
            RunTime = 0f;
            NewBest = false;
            ChainWindow = Mathf.Max(0.01f, config.fold.chainWindow);
            ChainExpiresAt = 0f;
            lastFoldTime = float.NegativeInfinity;
            lastShardTime = float.NegativeInfinity;
        }

        public void Tick(float deltaTime, float now)
        {
            RunTime += deltaTime;
            if (Chain > 0 && now > ChainExpiresAt) Chain = 0;
        }

        /// <summary>Counts a fold that caught something and returns the new chain length.</summary>
        public int RegisterFold(float now)
        {
            Chain = now - lastFoldTime <= ChainWindow ? Chain + 1 : 1;
            lastFoldTime = now;
            ChainExpiresAt = now + ChainWindow;
            BestChain = Mathf.Max(BestChain, Chain);
            Folds++;
            return Chain;
        }

        /// <summary>Fold points scale with both the kills and the current chain.</summary>
        public int ScoreFold(int killed, int pointsPerKill)
        {
            int points = pointsPerKill * killed * Mathf.Max(1, Chain);
            Score += points;
            return points;
        }

        public void AddScore(int points) => Score += Mathf.Max(0, points);

        public void MarkNewBest() => NewBest = true;

        /// <summary>Picks up one shard (1 XP, 1 point). Returns how many shards in a row were grabbed quickly.</summary>
        public int CollectShard(float now)
        {
            ShardCombo = now - lastShardTime < ShardComboWindow ? ShardCombo + 1 : 0;
            lastShardTime = now;
            Score++;
            Xp++;
            return ShardCombo;
        }

        /// <summary>Spends XP on any levels it now covers. Returns how many levels were gained.</summary>
        public int ApplyLevelUps(ProgressionSettings progression)
        {
            int gained = 0;
            while (XpToNext > 0 && Xp >= XpToNext)
            {
                Xp -= XpToNext;
                Level++;
                XpToNext = progression.XpForLevel(Level);
                gained++;
            }
            return gained;
        }
    }
}

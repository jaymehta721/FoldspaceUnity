using System;
using UnityEngine;

namespace Foldspace.Config
{
    /// <summary>
    /// Every gameplay number in one asset. Treat it as read-only at runtime: to experiment in a test or a mode,
    /// play with a copy made by <see cref="UnityEngine.Object.Instantiate(UnityEngine.Object)"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Foldspace/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        public ArenaSettings arena = new ArenaSettings();
        public ShipSettings ship = new ShipSettings();
        public DashSettings dash = new DashSettings();
        public WeaponSettings weapon = new WeaponSettings();
        public WakeSettings wake = new WakeSettings();
        public FoldSettings fold = new FoldSettings();
        public ProgressionSettings progression = new ProgressionSettings();
        public OnboardingSettings onboarding = new OnboardingSettings();
        [Tooltip("Enemy pacing for a run. Swap profiles for difficulty modes.")]
        public DirectorProfile director;
    }

    [Serializable]
    public class ArenaSettings
    {
        public float radius = 4.4f;
        [Tooltip("Fraction of the screen's short side the lens (with its rim) fills.")]
        [Range(0.5f, 1f)] public float screenFill = 0.94f;
    }

    [Serializable]
    public class ShipSettings
    {
        public float speed = 3f;
        [Tooltip("Degrees per second.")]
        public float turnRate = 220f;
        public float radius = 0.2f;
        public int hullPips = 3;
        public float hitInvulnerability = 1.2f;
        [Tooltip("Invulnerable for this long after spawning.")]
        public float spawnGrace = 1f;
        public Vector2 spawnPosition = new Vector2(0f, -1.5f);
    }

    [Serializable]
    public class DashSettings
    {
        public float duration = 0.25f;
        public float speedMultiplier = 2.2f;
        public float cooldown = 4f;
    }

    [Serializable]
    public class WeaponSettings
    {
        public bool autoFire = true;
        public float fireInterval = 0.28f;
        public float bulletSpeed = 9f;
        public float bulletLifetime = 0.9f;
        public float autoAimCone = 30f;
        public float autoAimRange = 5f;
    }

    [Serializable]
    public class WakeSettings
    {
        [Tooltip("Seconds of path the wake remembers.")]
        public float duration = 2f;
        public float sampleSpacing = 0.06f;
        public float width = 0.14f;
        [Tooltip("Enemies take 1 damage when they touch the wake.")]
        public bool contactDamage = true;
        public float contactCooldown = 0.3f;
    }

    [Serializable]
    public class FoldSettings
    {
        public int damage = 2;
        public int pointsPerKill = 25;
        [Tooltip("Loops smaller than this (world units squared) are ignored.")]
        public float minArea = 0.3f;
        [Tooltip("Coming this close to an older part of the wake also closes the loop.")]
        public float loopCloseDistance = 0.14f;
        [Tooltip("Wake points younger than this can't close a loop by proximity.")]
        public float minLoopAge = 0.45f;
        [Tooltip("Folds within this many seconds of each other build a chain.")]
        public float chainWindow = 1.5f;
    }

    [Serializable]
    public class ProgressionSettings
    {
        public float magnetRadius = 1.5f;
        public float collectRadius = 0.4f;
        public float shardLifetime = 20f;
        [Tooltip("Folded enemies drop shardValue x chain, capped here.")]
        public int maxShardsPerFold = 12;
        public int xpFirstLevel = 10;
        public int xpPerLevel = 6;

        public int XpForLevel(int level) => xpFirstLevel + xpPerLevel * Mathf.Max(0, level - 1);
    }

    [Serializable]
    public class OnboardingSettings
    {
        public float controlsTipDuration = 5f;
        [Tooltip("Show the fold tip if the player hasn't closed a loop after this many seconds.")]
        public float foldHintDelay = 20f;
    }
}

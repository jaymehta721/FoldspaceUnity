using UnityEngine;

namespace Foldspace.Config
{
    /// <summary>How the director paces a run: telegraphed packs of swarmers, plus the occasional elite with escorts.</summary>
    [CreateAssetMenu(menuName = "Foldspace/Director Profile", fileName = "DirectorProfile")]
    public class DirectorProfile : ScriptableObject
    {
        public bool spawnEnemies = true;
        public int maxEnemies = 45;
        [Tooltip("Warning shown at a spawn point before the enemies appear.")]
        public float telegraphTime = 0.9f;

        [Header("Packs")]
        public EnemyDefinition packEnemy;
        public float firstPackDelay = 1.5f;
        public float packIntervalStart = 6f;
        public float packIntervalEnd = 3f;
        [Tooltip("Seconds until packs reach their fastest pace.")]
        public float rampDuration = 180f;
        public int packSizeMin = 5;
        public int packSizeMax = 7;
        [Tooltip("Extra enemies per pack for every minute survived.")]
        public int packGrowthPerMinute = 1;
        public float packSpread = 0.6f;

        [Header("Elites")]
        public EnemyDefinition eliteEnemy;
        public float firstEliteTime = 25f;
        public float eliteInterval = 20f;
        [Tooltip("Elites allowed at once: 1, plus one more per minute, up to this cap.")]
        public int maxElites = 4;
        public EnemyDefinition escortEnemy;
        public int escortCount = 3;
        public float escortSpread = 0.9f;

        public float PackInterval(float elapsed) => Mathf.Lerp(packIntervalStart, packIntervalEnd, Mathf.Clamp01(elapsed / rampDuration));
        public int PackSize(float elapsed) => Random.Range(packSizeMin, packSizeMax + 1) + packGrowthPerMinute * Mathf.FloorToInt(elapsed / 60f);
        public int EliteCap(float elapsed) => Mathf.Min(maxElites, 1 + Mathf.FloorToInt(elapsed / 60f));
    }
}

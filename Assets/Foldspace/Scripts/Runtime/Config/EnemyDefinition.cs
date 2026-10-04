using Foldspace.Gameplay;
using UnityEngine;

namespace Foldspace.Config
{
    /// <summary>Stats and rules for one enemy type. The prefab supplies the behaviour and visuals.</summary>
    [CreateAssetMenu(menuName = "Foldspace/Enemy Definition", fileName = "Enemy")]
    public class EnemyDefinition : ScriptableObject
    {
        public string displayName = "Enemy";
        public Enemy prefab;

        [Header("Stats")]
        public int hp = 1;
        public float radius = 0.2f;
        public float speed = 1.7f;
        public float acceleration = 3.5f;
        [Tooltip("Pushes away from other enemies closer than this.")]
        public float separationRadius = 0.5f;
        public float separationWeight = 1.5f;

        [Header("Rules")]
        [Tooltip("Bullets deflect off it. Only folds and the wake hurt it.")]
        public bool bulletProof;
        [Tooltip("The ship's wake doesn't hurt it.")]
        public bool wakeImmune;
        [Tooltip("Pops when it rams the ship.")]
        public bool diesOnContact = true;
        [Tooltip("Other enemies within this radius are bullet-proof too. 0 = no shield.")]
        public float shieldRadius;

        [Header("Rewards")]
        public int shardValue = 1;
        public int scoreValue = 10;

        [Header("Arrival")]
        [Tooltip("Banner shown when the director telegraphs this enemy. Leave empty for none.")]
        public string announceTitle;
        public string announceSubtitle;
    }
}

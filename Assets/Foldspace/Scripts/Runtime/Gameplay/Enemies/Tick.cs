using UnityEngine;

namespace Foldspace.Gameplay
{
    /// <summary>Swarmer that charges straight at the ship and pops on contact.</summary>
    public class Tick : Enemy
    {
        float phase;

        protected override void OnSpawned() => phase = Random.value * 10f;

        protected override void Think(float dt) => Seek(dt);

        protected override float Wobble() => 1f + 0.06f * Mathf.Sin(Time.time * 7f + phase);
    }
}

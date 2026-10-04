using Foldspace.Rendering;
using UnityEngine;

namespace Foldspace.Gameplay
{
    /// <summary>XP pickup. Drifts to a stop, then homes in once the ship gets close or a fold claims it.</summary>
    public class Shard : MonoBehaviour
    {
        const float FadeTime = 3f;
        const float HomingTopSpeed = 12f;
        const float HomingAcceleration = 30f;

        [SerializeField] SpriteRenderer sprite;
        [SerializeField] SpriteRenderer glow;

        GameContext context;
        Color coreColor;
        Color glowColor;
        float baseScale;
        Vector2 velocity;
        float homingSpeed;
        float bornAt;
        float spin;
        float phase;
        bool attracted;

        public Vector2 Position => transform.position;

        void Awake()
        {
            coreColor = sprite.color;
            glowColor = glow.color.WithAlpha(1f);
            baseScale = sprite.transform.localScale.x;
        }

        public void Spawn(GameContext ctx, Vector2 position, Vector2 scatter, bool attract)
        {
            context = ctx;
            transform.position = position;
            velocity = scatter;
            attracted = attract;
            homingSpeed = 0f;
            bornAt = Time.time;
            spin = Random.Range(-180f, 180f);
            phase = Random.value * 10f;
            Animate(0f);
        }

        public void Attract() => attracted = true;

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || context == null) return;
            Vector2 p = transform.position;

            var player = context.Player;
            var progression = context.Config.progression;
            if (context.IsPlaying && player != null && player.Alive)
            {
                Vector2 toPlayer = player.Position - p;
                float distance = toPlayer.magnitude;
                if (distance <= progression.collectRadius)
                {
                    context.Rules.CollectShard(this);
                    return;
                }
                if (distance <= progression.magnetRadius) attracted = true;
                if (attracted)
                {
                    homingSpeed = Mathf.MoveTowards(homingSpeed, HomingTopSpeed, HomingAcceleration * dt);
                    velocity = toPlayer / Mathf.Max(distance, 0.001f) * Mathf.Max(homingSpeed, velocity.magnitude);
                }
            }
            if (!attracted) velocity *= Mathf.Exp(-4f * dt);

            transform.position = p + velocity * dt;
            sprite.transform.Rotate(0f, 0f, spin * dt);

            float age = Time.time - bornAt;
            if (age > progression.shardLifetime)
            {
                context.Spawner.Despawn(this);
                return;
            }
            Animate(age);
        }

        void Animate(float age)
        {
            float lifetime = context.Config.progression.shardLifetime;
            bool fading = age > lifetime - FadeTime;
            float alpha = fading ? (lifetime - age) / FadeTime : 1f;
            if (fading && Mathf.Repeat(age * 6f, 1f) < 0.3f) alpha *= 0.3f;
            float pop = Mathf.Min(1f, age * 6f);
            float pulse = 1f + 0.15f * Mathf.Sin(Time.time * 8f + phase);
            sprite.transform.localScale = Vector3.one * (baseScale * pulse * pop);
            sprite.color = coreColor.WithAlpha(alpha);
            glow.color = glowColor.WithAlpha((attracted ? 0.65f : 0.4f) * alpha);
            glow.transform.localScale = Vector3.one * (attracted ? 0.75f : 0.6f) * pop;
        }
    }
}

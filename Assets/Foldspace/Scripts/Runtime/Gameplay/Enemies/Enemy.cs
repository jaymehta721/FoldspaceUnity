using System.Collections;
using Foldspace.Config;
using Foldspace.Rendering;
using Foldspace.Utilities;
using UnityEngine;

namespace Foldspace.Gameplay
{
    /// <summary>
    /// Shared enemy lifecycle: pop-in, steering, wake and ship contact, damage flash and the fold-away death.
    /// Stats come from an <see cref="EnemyDefinition"/>; instances are pooled, so <see cref="Spawn"/> resets everything.
    /// </summary>
    public abstract class Enemy : MonoBehaviour
    {
        /// <summary>Enemies pop in over this long and can't touch the ship until they finish.</summary>
        const float SpawnTime = 0.35f;
        const float FoldAwayTime = 0.2f;
        const float HitFlashTime = 0.07f;

        [SerializeField] protected SpriteRenderer body;
        [SerializeField] protected SpriteRenderer glow;
        [SerializeField] bool faceVelocity = true;

        protected GameContext Context { get; private set; }
        Color bodyColor;
        float bodyScale = 1f;
        float spawnedAt;
        float flashUntil;
        float nextWakeHit;
        float punch;
        Vector2 velocity;

        public EnemyDefinition Definition { get; private set; }
        public int Hp { get; private set; }
        public bool Dying { get; private set; }
        public bool Spawning => Time.time - spawnedAt < SpawnTime;
        public float Radius => Definition.radius;
        public Vector2 Position => transform.position;
        public Vector2 Velocity => velocity;
        internal Enemy SourcePrefab { get; set; }

        protected virtual void Awake()
        {
            bodyColor = body.color;
            bodyScale = body.transform.localScale.x;
        }

        public void Spawn(GameContext ctx, EnemyDefinition definition, Vector2 position)
        {
            Context = ctx;
            Definition = definition;
            Hp = definition.hp;
            Dying = false;
            velocity = Vector2.zero;
            spawnedAt = Time.time;
            flashUntil = 0f;
            nextWakeHit = 0f;
            punch = 0f;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            transform.localScale = Vector3.one * 0.01f;
            body.color = bodyColor;
            body.transform.localScale = Vector3.one * bodyScale;
            if (glow != null) glow.enabled = true;
            OnSpawned();
        }

        protected virtual void OnSpawned() { }
        protected abstract void Think(float dt);

        /// <summary>Per-frame scale wobble for the body, multiplied into its base scale.</summary>
        protected virtual float Wobble() => 1f;

        /// <summary>A bullet bounced off this enemy's shield at <paramref name="point"/>.</summary>
        public virtual void OnDeflected(Vector2 point) { }

        public void Push(Vector2 impulse) => velocity += impulse;

        /// <summary>Steers toward the ship (or the arena center) while keeping clear of other enemies.</summary>
        protected void Seek(float dt)
        {
            var player = Context.Player;
            Vector2 target = player != null && player.Alive ? player.Position : Vector2.zero;
            var d = Definition;
            Vector2 desired = (target - Position).normalized * d.speed + Context.Registry.Separation(this, d.separationRadius) * d.separationWeight;
            velocity = Vector2.MoveTowards(velocity, desired, d.acceleration * dt);
        }

        void Update()
        {
            if (Dying || Context == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Think(dt);
            transform.position = Context.ClampToArena(Position + velocity * dt, Radius);
            if (faceVelocity && velocity.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Euler(0f, 0f, Geometry.HeadingOf(velocity));

            float age = Time.time - spawnedAt;
            transform.localScale = Vector3.one * (age < SpawnTime ? Mathf.Max(0.01f, Ease.OutBack(age / SpawnTime)) : 1f);
            punch = Mathf.MoveTowards(punch, 0f, dt * 6f);
            body.transform.localScale = Vector3.one * (bodyScale * (1f + 0.35f * punch) * Wobble());
            body.color = Time.time < flashUntil ? Color.white : bodyColor;

            var player = Context.Player;
            if (player == null || !player.Alive || !Context.IsPlaying) return;

            var wake = Context.Config.wake;
            if (wake.contactDamage && !Definition.wakeImmune && Time.time >= nextWakeHit && player.Wake.Touches(Position, Radius))
            {
                nextWakeHit = Time.time + wake.contactCooldown;
                if (TakeDamage(1, DamageSource.Wake)) return;
            }

            if (Spawning) return;
            float reach = Radius + Context.Config.ship.radius;
            if (!player.Invulnerable && (player.Position - Position).sqrMagnitude <= reach * reach)
                OnTouchPlayer(player);
        }

        protected virtual void OnTouchPlayer(PlayerShip player)
        {
            player.TakeHit(Position);
            if (Definition.diesOnContact && !Dying) TakeDamage(Hp, DamageSource.Contact);
        }

        /// <summary>Returns true if this hit killed the enemy. Bullets do nothing to bullet-proof enemies.</summary>
        public bool TakeDamage(int amount, DamageSource source, Vector2 foldCenter = default)
        {
            if (Dying) return false;
            if (source == DamageSource.Bullet && Definition.bulletProof) return false;
            Hp -= amount;
            flashUntil = Time.time + HitFlashTime;
            punch = 1f;
            if (Hp > 0) return false;

            Dying = true;
            Context.Registry.Remove(this);
            Context.Events.RaiseEnemyKilled(this, source);
            if (source == DamageSource.Fold && isActiveAndEnabled) StartCoroutine(FoldAway(foldCenter));
            else Context.Spawner.Despawn(this);
            return true;
        }

        /// <summary>Spirals into the fold's center on unscaled time, so it plays through the hitstop.</summary>
        IEnumerator FoldAway(Vector2 center)
        {
            Vector2 start = Position;
            float startScale = transform.localScale.x;
            float spin = Random.value < 0.5f ? 1f : -1f;
            if (glow != null) glow.enabled = false;
            float t = 0f;
            while (t < FoldAwayTime)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / FoldAwayTime);
                Vector2 offset = Geometry.Rotate(start - center, spin * 200f * k) * (1f - Ease.InCubic(k));
                transform.position = center + offset;
                transform.localScale = Vector3.one * (startScale * (1f - 0.85f * k));
                transform.Rotate(0f, 0f, spin * 900f * Time.unscaledDeltaTime);
                body.color = Color.Lerp(Color.white, Palette.YouHot, k);
                yield return null;
            }
            Context.Vfx.Sparks(center, Palette.HaloLight, 3, 4f, 0.2f, true);
            Context.Spawner.Despawn(this);
        }
    }
}

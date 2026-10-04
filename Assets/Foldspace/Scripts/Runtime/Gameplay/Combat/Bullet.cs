using Foldspace.Utilities;
using UnityEngine;

namespace Foldspace.Gameplay
{
    /// <summary>Auto-fire shot. Hurts the first enemy it touches, or bounces off a shield.</summary>
    public class Bullet : MonoBehaviour
    {
        const float HitRadius = 0.06f;

        GameContext context;
        Vector2 velocity;
        float expiresAt;

        public void Spawn(GameContext ctx, Vector2 position, Vector2 direction)
        {
            context = ctx;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, Geometry.HeadingOf(direction)));
            velocity = direction.normalized * ctx.Config.weapon.bulletSpeed;
            expiresAt = Time.time + ctx.Config.weapon.bulletLifetime;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || context == null) return;
            Vector2 p = (Vector2)transform.position + velocity * dt;
            transform.position = p;

            float limit = context.Config.arena.radius;
            if (Time.time >= expiresAt || p.sqrMagnitude > limit * limit)
            {
                context.Events.RaiseBulletExpired(p);
                context.Spawner.Despawn(this);
                return;
            }

            var hit = context.Registry.FindEnemyAt(p, HitRadius);
            if (hit == null) return;
            var shield = hit.Definition.bulletProof ? hit : context.Registry.FindShieldFor(hit);
            if (shield != null)
            {
                shield.OnDeflected(p);
                context.Events.RaiseBulletDeflected(p);
            }
            else
            {
                hit.TakeDamage(1, DamageSource.Bullet);
                context.Events.RaiseBulletHit(p);
            }
            context.Spawner.Despawn(this);
        }
    }
}

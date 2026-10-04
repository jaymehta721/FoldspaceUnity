using Foldspace.Controls;
using Foldspace.Utilities;
using UnityEngine;

namespace Foldspace.Gameplay
{
    /// <summary>
    /// Always flies forward and fires on its own; the player only steers and dashes.
    /// Cosmetics live in <see cref="PlayerShipVisuals"/>, the trail and loop detection in <see cref="Wake"/>.
    /// </summary>
    public class PlayerShip : MonoBehaviour
    {
        const float MuzzleOffset = 0.34f;

        [SerializeField] Wake wake;
        [SerializeField] PlayerShipVisuals visuals;

        GameContext context;
        float heading;
        float invulnerableUntil;
        float dashUntil;
        float dashReadyAt;
        float fireTimer;

        /// <summary>When set, replaces input with a constant turn (1 = full left, -1 = full right). Used by tests and demos.</summary>
        public float? AutopilotTurn { get; set; }

        public Wake Wake => wake;
        public int Hull { get; private set; }
        public bool Alive => Hull > 0;
        public Vector2 Position => transform.position;
        public float Heading => heading;
        public Vector2 Forward => Geometry.Direction(heading);
        public bool Dashing => Time.time < dashUntil;
        public bool Invulnerable => Dashing || Time.time < invulnerableUntil;
        public float HurtUntil { get; private set; }
        public float DashReadyIn => Mathf.Max(0f, dashReadyAt - Time.time);

        public void Spawn(GameContext ctx, Vector2 position)
        {
            context = ctx;
            var ship = ctx.Config.ship;
            Hull = ship.hullPips;
            heading = 0f;
            invulnerableUntil = Time.time + ship.spawnGrace;
            HurtUntil = 0f;
            dashUntil = 0f;
            dashReadyAt = 0f;
            fireTimer = 0f;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            wake.Init(ctx);
            wake.Record(position);
            visuals.Init(ctx, this);
        }

        void Update()
        {
            if (!Alive || context == null || !context.IsPlaying) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            var ship = context.Config.ship;

            float before = heading;
            var steer = AutopilotTurn.HasValue ? SteerCommand.Turning(AutopilotTurn.Value, false) : context.Input.ReadSteer(Position, heading);
            if (steer.HasTarget) heading = Mathf.MoveTowardsAngle(heading, steer.TargetHeading, ship.turnRate * dt);
            else heading += steer.Turn * ship.turnRate * dt;
            heading = Mathf.Repeat(heading, 360f);
            float turn = Mathf.Clamp(Mathf.DeltaAngle(before, heading) / (ship.turnRate * dt), -1f, 1f);
            if (steer.Dash) TryDash();

            var dash = context.Config.dash;
            float speed = ship.speed * (Dashing ? dash.speedMultiplier : 1f);
            Vector2 position = Position + Forward * speed * dt;
            float limit = context.Config.arena.radius - ship.radius;
            if (position.sqrMagnitude > limit * limit)
            {
                Vector2 outward = position.normalized;
                position = outward * limit;
                if (Vector2.Dot(Forward, outward) > 0f)
                {
                    heading = Geometry.HeadingOf(Vector2.Reflect(Forward, -outward));
                    context.Events.RaiseRimBounced(position);
                }
            }

            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, heading));
            wake.Record(position);
            AutoFire(dt);
            visuals.Tick(dt, turn);
        }

        public void TakeHit(Vector2 from)
        {
            if (!Alive || Invulnerable || !context.IsPlaying) return;
            Hull--;
            invulnerableUntil = Time.time + context.Config.ship.hitInvulnerability;
            HurtUntil = invulnerableUntil;
            context.Events.RaisePlayerHit(Position);
            if (Hull > 0) return;
            context.Events.RaisePlayerDied(Position);
            gameObject.SetActive(false);
        }

        void TryDash()
        {
            if (Time.time < dashReadyAt) return;
            var dash = context.Config.dash;
            dashUntil = Time.time + dash.duration;
            dashReadyAt = Time.time + dash.cooldown;
            wake.Flash();
            context.Events.RaisePlayerDashed(Position, Forward);
        }

        void AutoFire(float dt)
        {
            var weapon = context.Config.weapon;
            if (!weapon.autoFire) return;
            fireTimer -= dt;
            if (fireTimer > 0f) return;
            fireTimer = weapon.fireInterval;

            Vector2 direction = Forward;
            var target = context.Registry.FindAutoAimTarget(Position, Forward, weapon.autoAimCone, weapon.autoAimRange);
            if (target != null) direction = (target.Position - Position).normalized;
            Vector2 muzzle = Position + Forward * MuzzleOffset;
            context.Spawner.SpawnBullet(muzzle, direction);
            context.Events.RaiseBulletFired(muzzle);
        }
    }
}

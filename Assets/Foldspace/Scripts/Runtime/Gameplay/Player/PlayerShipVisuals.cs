using Foldspace.Rendering;
using Foldspace.Utilities;
using UnityEngine;

namespace Foldspace.Gameplay
{
    /// <summary>Warp-in, banking, engine flame, hurt blink, dash afterimages and engine embers for the ship.</summary>
    public class PlayerShipVisuals : MonoBehaviour
    {
        const float WarpInTime = 0.4f;
        const float BankAmount = 0.22f;
        const float TailOffset = 0.32f;

        [SerializeField] SpriteRenderer body;
        [SerializeField] SpriteRenderer glow;
        [SerializeField] SpriteRenderer engineGlow;
        [Tooltip("Pivot at the engine; the flame sprite hangs below it, so scaling Y stretches the flame backward.")]
        [SerializeField] Transform thrust;

        [Header("Thrust")]
        [SerializeField] Vector2 thrustCruise = new Vector2(0.42f, 0.62f);
        [SerializeField] float thrustDashLength = 1.5f;

        GameContext context;
        PlayerShip ship;
        Color bodyColor;
        float bodyScale;
        float spawnedAt;
        float bank;
        float ghostTimer;
        float emberTimer;

        void Awake()
        {
            bodyColor = body.color;
            bodyScale = body.transform.localScale.x;
        }

        public void Init(GameContext ctx, PlayerShip owner)
        {
            context = ctx;
            ship = owner;
            spawnedAt = Time.time;
            bank = 0f;
            ghostTimer = 0f;
            emberTimer = 0f;
            Tick(0f, 0f);
        }

        public void Tick(float dt, float turn)
        {
            float now = Time.time;
            float appear = Ease.OutBack((now - spawnedAt) / WarpInTime);
            bank = Mathf.Lerp(bank, turn, 1f - Mathf.Exp(-10f * dt));
            body.transform.localScale = new Vector3(bodyScale * appear * (1f - BankAmount * Mathf.Abs(bank)), bodyScale * appear, 1f);

            bool dashing = ship.Dashing;
            float flicker = 0.85f + 0.15f * Mathf.Sin(now * 47f) * Mathf.Sin(now * 23f);
            thrust.localScale = new Vector3(thrustCruise.x * appear, (dashing ? thrustDashLength : thrustCruise.y) * flicker * appear, 1f);
            engineGlow.color = (dashing ? Palette.YouHot : Palette.Heat).WithAlpha((dashing ? 0.9f : 0.55f) * flicker);
            glow.color = Palette.You.WithAlpha(dashing ? 0.5f : 0.28f);
            glow.transform.localScale = Vector3.one * (dashing ? 2.1f : 1.7f) * appear;

            bool blink = now < ship.HurtUntil && Mathf.Repeat(now * 12f, 1f) < 0.5f;
            body.enabled = !blink;
            body.color = dashing ? Color.Lerp(bodyColor, Color.white, 0.6f) : bodyColor;
            if (dt <= 0f) return;

            context.Arena.Grid.Impulse(ship.Position, 0.8f, (dashing ? 14f : 6f) * dt);

            if (dashing)
            {
                ghostTimer -= dt;
                if (ghostTimer <= 0f)
                {
                    ghostTimer = 0.035f;
                    context.Vfx.Ghost(body.sprite, body.transform.position, transform.rotation, bodyScale, Palette.You.WithAlpha(0.45f), 0.22f);
                }
            }

            emberTimer -= dt;
            if (emberTimer <= 0f)
            {
                emberTimer = dashing ? 0.015f : 0.04f;
                Vector2 forward = ship.Forward;
                Vector2 tail = ship.Position - forward * TailOffset;
                Vector2 spray = -forward * Random.Range(0.8f, 1.6f) + Random.insideUnitCircle * 0.35f;
                var color = (Random.value < 0.5f ? Palette.Heat : Palette.You).WithAlpha(0.8f);
                context.Vfx.Ember(tail, spray, color, Random.Range(0.08f, 0.14f), Random.Range(0.2f, 0.35f));
            }
        }
    }
}

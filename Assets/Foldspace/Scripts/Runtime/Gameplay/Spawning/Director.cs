using System.Collections;
using Foldspace.Config;
using Foldspace.Rendering;
using UnityEngine;

namespace Foldspace.Gameplay
{
    /// <summary>Paces a run from its <see cref="DirectorProfile"/>: telegraphed packs from the far side of the lens, plus escorted elites.</summary>
    public class Director : MonoBehaviour
    {
        const float SpawnInset = 0.5f;
        const float SpawnArc = 1.6f;
        const float SpawnClearance = 0.4f;

        GameContext context;
        float elapsed;
        float nextPack;
        float nextElite;

        DirectorProfile Profile => context.Config.director;

        public void Init(GameContext ctx) => context = ctx;

        public void BeginRun()
        {
            StopAllCoroutines();
            elapsed = 0f;
            nextPack = Profile.firstPackDelay;
            nextElite = Profile.firstEliteTime;
        }

        public void EndRun() => StopAllCoroutines();

        void Update()
        {
            if (context == null || !context.IsPlaying) return;
            var profile = Profile;
            if (profile == null || !profile.spawnEnemies) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            elapsed += dt;

            if (elapsed >= nextPack)
            {
                nextPack = elapsed + profile.PackInterval(elapsed);
                StartCoroutine(SpawnPack(profile.packEnemy, profile.PackSize(elapsed), null, 0));
            }

            if (elapsed >= nextElite)
            {
                nextElite = elapsed + profile.eliteInterval;
                if (profile.eliteEnemy != null && context.Registry.CountAlive(profile.eliteEnemy) < profile.EliteCap(elapsed))
                    StartCoroutine(SpawnPack(profile.eliteEnemy, 1, profile.escortEnemy, profile.escortCount));
            }
        }

        IEnumerator SpawnPack(EnemyDefinition enemy, int count, EnemyDefinition escort, int escorts)
        {
            if (enemy == null) yield break;
            var profile = Profile;
            Vector2 at = PickSpawnPoint();
            context.Events.RaiseSpawnTelegraphed(at, profile.telegraphTime);
            if (!string.IsNullOrEmpty(enemy.announceTitle))
                context.Events.RaiseAnnounced(enemy.announceTitle, enemy.announceSubtitle, Palette.HaloLight);
            yield return new WaitForSeconds(profile.telegraphTime);
            if (!context.IsPlaying) yield break;

            for (int i = 0; i < count && context.Registry.Enemies.Count < profile.maxEnemies; i++)
                context.Spawner.SpawnEnemy(enemy, context.ClampToArena(at + Random.insideUnitCircle * profile.packSpread, SpawnClearance));

            if (escort == null) yield break;
            for (int i = 0; i < escorts && context.Registry.Enemies.Count < profile.maxEnemies; i++)
                context.Spawner.SpawnEnemy(escort, context.ClampToArena(at + Random.insideUnitCircle * profile.escortSpread, SpawnClearance));
        }

        Vector2 PickSpawnPoint()
        {
            var player = context.Player;
            Vector2 p = player != null ? player.Position : Vector2.zero;
            float away = p.sqrMagnitude > 0.01f ? Mathf.Atan2(-p.y, -p.x) : Random.Range(0f, Mathf.PI * 2f);
            float angle = away + Random.Range(-SpawnArc, SpawnArc);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (context.Config.arena.radius - SpawnInset);
        }
    }
}

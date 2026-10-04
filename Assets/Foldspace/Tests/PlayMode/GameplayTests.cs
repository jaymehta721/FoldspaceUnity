using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Foldspace.Config;
using Foldspace.Environment;
using Foldspace.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Foldspace.Tests
{
    /// <summary>Runs the shipped Game scene with tuned copies of its config, so the real prefabs and wiring are under test.</summary>
    public class GameplayTests
    {
        const string SceneName = "Game";

        GameManager game;
        GameContext context;
        EnemyDefinition tick;
        EnemyDefinition warden;
        readonly List<Object> clones = new List<Object>();

        PlayerShip Player => context.Player;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            Assert.IsNotNull(game, "the Game scene should contain a GameManager");
            Assert.IsTrue(game.enabled, "GameManager disabled itself: a reference is missing");
            context = game.Context;
            tick = context.Config.director.packEnemy;
            warden = context.Config.director.eliteEnemy;
        }

        [TearDown]
        public void TearDown()
        {
            if (game != null)
            {
                game.ShowTitle();
                game.Configure(null);
            }
            foreach (var clone in clones)
                if (clone != null) Object.Destroy(clone);
            clones.Clear();
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator Title_screen_runs_its_demo_over_the_static_environment()
        {
            Assert.IsNotNull(Object.FindAnyObjectByType<Backdrop>());
            Assert.IsNotNull(Object.FindAnyObjectByType<Arena>());
            yield return new WaitForSecondsRealtime(3.4f);
            yield return Capture("foldspace-title.png");
            Assert.AreEqual(GameState.Title, game.State);
            Assert.IsNull(Player);
        }

        [UnityTest]
        public IEnumerator Circling_a_pack_folds_everything_inside_and_nothing_outside()
        {
            var config = Quiet();
            game.StartRun();
            Vector2 center = LeftTurnCenter(config);
            Spawn(tick, center + new Vector2(0.2f, 0f));
            Spawn(tick, center + new Vector2(-0.2f, 0.1f));
            Spawn(tick, center + new Vector2(0f, -0.2f));
            var outside = Spawn(tick, center + new Vector2(2.5f, 0f));
            Player.AutopilotTurn = 1f;

            yield return WaitUntil(() => context.Session.Folds > 0, 4f);
            yield return Capture("foldspace-fold.png");
            Assert.AreEqual(1, context.Session.Folds, "one loop should close around the pack");
            Assert.AreEqual(1, context.Registry.Enemies.Count, "only the Tick outside the loop should survive");
            Assert.IsTrue(Contains(context.Registry.Enemies, outside));
            Assert.AreEqual(3, context.Metrics.EnemiesFolded);
            Assert.Greater(context.Session.Score, 0);
        }

        [UnityTest]
        public IEnumerator Warden_deflects_bullets()
        {
            Quiet(autoFire: true);
            game.StartRun();
            var shielded = Spawn(warden, Player.Position + Vector2.up * 2.6f);
            Player.AutopilotTurn = 0f;

            yield return new WaitForSecondsRealtime(0.5f);
            Assert.GreaterOrEqual(context.Metrics.Deflections, 1, "bullets should have reached the Warden");
            Assert.AreEqual(warden.hp, shielded.Hp);
            Assert.IsFalse(shielded.Dying);
        }

        [UnityTest]
        public IEnumerator Warden_dies_to_a_fold()
        {
            var config = Quiet();
            game.StartRun();
            var shielded = Spawn(warden, LeftTurnCenter(config));
            Player.AutopilotTurn = 1f;

            yield return WaitUntil(() => context.Session.Folds > 0, 4f);
            Assert.IsTrue(shielded.Dying || !shielded.gameObject.activeSelf);
            Assert.AreEqual(0, context.Registry.Enemies.Count);
        }

        [UnityTest]
        public IEnumerator Bullets_are_recycled_through_the_pool()
        {
            Quiet(autoFire: true);
            int fired = 0;
            context.Events.BulletFired += _ => fired++;
            game.StartRun();
            Player.AutopilotTurn = 1f;

            yield return new WaitForSecondsRealtime(3f);
            var spawner = Object.FindAnyObjectByType<EntitySpawner>();
            int instances = Object.FindObjectsByType<Bullet>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            Assert.GreaterOrEqual(fired, 8);
            Assert.Greater(spawner.PooledBullets, 0, "expired bullets should return to the pool");
            Assert.Less(instances, fired, "bullets should be reused, not instantiated per shot");
            Assert.AreEqual(instances, spawner.LiveBullets + spawner.PooledBullets);
        }

        [UnityTest]
        public IEnumerator Losing_the_last_hull_ends_the_run_and_restart_works()
        {
            Quiet().ship.hullPips = 1;
            game.StartRun();
            Player.AutopilotTurn = 0f;
            Spawn(tick, Player.Position + Vector2.up * 4.1f);

            yield return WaitUntil(() => game.State == GameState.GameOver, 4f);
            Assert.AreEqual(GameState.GameOver, game.State);
            yield return new WaitForSecondsRealtime(1.6f);
            yield return Capture("foldspace-gameover.png");

            game.StartRun();
            Assert.AreEqual(GameState.Playing, game.State);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(Player.Alive);
        }

        [UnityTest]
        public IEnumerator Fourteen_seconds_of_play_run_without_errors()
        {
            Tuned().ship.hullPips = 99;
            game.StartRun();
            float start = Time.realtimeSinceStartup;
            bool captured = false;
            while (Time.realtimeSinceStartup - start < 14f)
            {
                float t = Time.realtimeSinceStartup - start;
                Player.AutopilotTurn = Mathf.Repeat(t, 3f) < 1.8f ? 1f : 0.15f;
                if (!captured && t > 11.5f)
                {
                    captured = true;
                    yield return Capture("foldspace-gameplay.png");
                }
                yield return null;
            }
            Assert.Greater(context.Metrics.EnemiesSpawned, 0);
            Assert.Greater(context.Metrics.LoopsClosed, 0);
            Assert.IsTrue(Player.Alive);
            Debug.Log(context.Metrics.Summary(context.Session));
        }

        // ---- Helpers ----

        /// <summary>A copy of the shipped config (and its director) applied to the next run.</summary>
        GameConfig Tuned()
        {
            var config = Clone(game.Config);
            config.director = Clone(config.director);
            game.Configure(config);
            return config;
        }

        /// <summary>No director spawns, no auto-fire and frozen enemies, so a test controls everything on screen.</summary>
        GameConfig Quiet(bool autoFire = false)
        {
            var config = Tuned();
            config.director.spawnEnemies = false;
            config.weapon.autoFire = autoFire;
            tick = Frozen(tick);
            warden = Frozen(warden);
            return config;
        }

        EnemyDefinition Frozen(EnemyDefinition definition)
        {
            var copy = Clone(definition);
            copy.speed = 0f;
            copy.acceleration = 0f;
            return copy;
        }

        T Clone<T>(T asset) where T : Object
        {
            var copy = Object.Instantiate(asset);
            clones.Add(copy);
            return copy;
        }

        Enemy Spawn(EnemyDefinition definition, Vector2 position)
        {
            var enemy = context.Spawner.SpawnEnemy(definition, position);
            Assert.IsNotNull(enemy, $"spawning {definition.name}");
            return enemy;
        }

        static bool Contains(IReadOnlyList<Enemy> enemies, Enemy enemy)
        {
            foreach (var e in enemies)
                if (e == enemy) return true;
            return false;
        }

        /// <summary>The ship starts heading up, so a full left turn circles a point to its left.</summary>
        Vector2 LeftTurnCenter(GameConfig config)
        {
            float radius = config.ship.speed / (config.ship.turnRate * Mathf.Deg2Rad);
            return Player.Position + new Vector2(-radius, 0f);
        }

        static IEnumerator WaitUntil(Func<bool> condition, float timeout)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        }

        /// <summary>Renders the world camera (with post-processing) and then the UI camera into a 1600x900 PNG in Logs/.</summary>
        IEnumerator Capture(string fileName)
        {
            const int width = 1600, height = 900;
            var world = game.WorldCamera;
            var ui = game.UiCamera;
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            world.targetTexture = target;
            ui.targetTexture = target;
            yield return null;
            yield return null;

            world.Render();
            ui.Render();
            var previousActive = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = previousActive;
            world.targetTexture = null;
            ui.targetTexture = null;
            RenderTexture.ReleaseTemporary(target);

            string folder = Path.Combine(Application.dataPath, "..", "Logs");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, fileName), texture.EncodeToPNG());
            Object.Destroy(texture);
        }
    }
}

using System.Collections.Generic;
using Foldspace.Config;
using UnityEngine;
using UnityEngine.Pool;

namespace Foldspace.Gameplay
{
    /// <summary>
    /// Creates and recycles run entities from their prefabs. Bullets, shards and enemies are pooled;
    /// the player is instantiated fresh each run. Everything is parented under <see cref="worldRoot"/>.
    /// </summary>
    public class EntitySpawner : MonoBehaviour
    {
        [SerializeField] Transform worldRoot;
        [SerializeField] PlayerShip playerPrefab;
        [SerializeField] Bullet bulletPrefab;
        [SerializeField] Shard shardPrefab;

        [Header("Pools")]
        [SerializeField] int bulletCapacity = 32;
        [SerializeField] int shardCapacity = 96;
        [SerializeField] int enemyCapacity = 48;

        GameContext context;
        ObjectPool<Bullet> bullets;
        ObjectPool<Shard> shards;
        readonly Dictionary<Enemy, ObjectPool<Enemy>> enemyPools = new Dictionary<Enemy, ObjectPool<Enemy>>();
        readonly HashSet<Bullet> liveBullets = new HashSet<Bullet>();
        readonly HashSet<Shard> liveShards = new HashSet<Shard>();
        readonly HashSet<Enemy> liveEnemies = new HashSet<Enemy>();
        readonly List<Component> despawnBuffer = new List<Component>();
        PlayerShip player;

        public int LiveBullets => liveBullets.Count;
        public int PooledBullets => bullets != null ? bullets.CountInactive : 0;

        public void Init(GameContext ctx)
        {
            context = ctx;
            if (worldRoot == null) worldRoot = transform;
            bullets = CreatePool(bulletPrefab, bulletCapacity);
            shards = CreatePool(shardPrefab, shardCapacity);
        }

        public PlayerShip SpawnPlayer(Vector2 position)
        {
            if (player != null) Destroy(player.gameObject);
            player = Instantiate(playerPrefab, worldRoot);
            player.name = playerPrefab.name;
            player.Spawn(context, position);
            context.Registry.Player = player;
            return player;
        }

        public Enemy SpawnEnemy(EnemyDefinition definition, Vector2 position)
        {
            if (definition == null || definition.prefab == null)
            {
                Debug.LogError($"[Foldspace] Enemy definition '{(definition != null ? definition.name : "null")}' has no prefab.", this);
                return null;
            }
            if (!enemyPools.TryGetValue(definition.prefab, out var pool))
            {
                pool = CreatePool(definition.prefab, enemyCapacity);
                enemyPools.Add(definition.prefab, pool);
            }
            var enemy = pool.Get();
            enemy.SourcePrefab = definition.prefab;
            liveEnemies.Add(enemy);
            context.Registry.Add(enemy);
            enemy.Spawn(context, definition, position);
            context.Events.RaiseEnemySpawned(enemy);
            return enemy;
        }

        public Bullet SpawnBullet(Vector2 position, Vector2 direction)
        {
            var bullet = bullets.Get();
            liveBullets.Add(bullet);
            bullet.Spawn(context, position, direction);
            return bullet;
        }

        public void SpawnShards(Vector2 position, int count, bool attracted)
        {
            for (int i = 0; i < count; i++)
            {
                var shard = shards.Get();
                liveShards.Add(shard);
                context.Registry.Add(shard);
                shard.Spawn(context, position, Random.insideUnitCircle * Random.Range(1f, 2.5f), attracted);
            }
        }

        public void Despawn(Bullet bullet)
        {
            if (liveBullets.Remove(bullet)) bullets.Release(bullet);
        }

        public void Despawn(Shard shard)
        {
            if (!liveShards.Remove(shard)) return;
            context.Registry.Remove(shard);
            shards.Release(shard);
        }

        public void Despawn(Enemy enemy)
        {
            if (!liveEnemies.Remove(enemy)) return;
            context.Registry.Remove(enemy);
            enemyPools[enemy.SourcePrefab].Release(enemy);
        }

        /// <summary>Returns every live entity to its pool and removes the player.</summary>
        public void DespawnAll()
        {
            despawnBuffer.Clear();
            despawnBuffer.AddRange(liveBullets);
            despawnBuffer.AddRange(liveShards);
            despawnBuffer.AddRange(liveEnemies);
            foreach (var c in despawnBuffer)
            {
                switch (c)
                {
                    case Bullet b: Despawn(b); break;
                    case Shard s: Despawn(s); break;
                    case Enemy e: Despawn(e); break;
                }
            }
            despawnBuffer.Clear();

            if (player != null)
            {
                player.gameObject.SetActive(false);
                Destroy(player.gameObject);
            }
            player = null;
            context.Registry.Clear();
        }

        ObjectPool<T> CreatePool<T>(T prefab, int capacity) where T : Component
        {
            return new ObjectPool<T>(
                () =>
                {
                    var item = Instantiate(prefab, worldRoot);
                    item.name = prefab.name;
                    item.gameObject.SetActive(false);
                    return item;
                },
                item => item.gameObject.SetActive(true),
                item => item.gameObject.SetActive(false),
                item => { if (item != null) Destroy(item.gameObject); },
                collectionCheck: false,
                defaultCapacity: capacity,
                maxSize: capacity * 4);
        }
    }
}

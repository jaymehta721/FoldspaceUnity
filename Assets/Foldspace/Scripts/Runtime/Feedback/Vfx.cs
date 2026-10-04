using System;
using System.Collections.Generic;
using Foldspace.Rendering;
using Foldspace.Utilities;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

namespace Foldspace.Feedback
{
    /// <summary>
    /// Pooled one-shot effects: sprite particles (dots, sparks, glows, rings, afterimages) plus the
    /// <see cref="FoldFlash"/> and <see cref="SpawnTelegraph"/> prefabs.
    /// </summary>
    public class Vfx : MonoBehaviour
    {
        enum Kind
        {
            Dot,
            Spark,
            Ring,
            Glow,
            Ghost,
        }

        class Particle
        {
            public Transform transform;
            public SpriteRenderer sprite;
            public Kind kind;
            public Vector2 velocity;
            public Color color;
            public float age;
            public float life;
            public float size;
            public float endSize;
            public float drag;
            public float spin;
            public float length;
            public bool unscaled;
        }

        [SerializeField] Sprite glowSprite;
        [SerializeField] Sprite ringSprite;
        [SerializeField] Material additive;
        [Tooltip("Additive and pushed past 1.0 so it always catches the bloom.")]
        [SerializeField] Material additiveHot;
        [SerializeField] FoldFlash foldFlashPrefab;
        [SerializeField] SpawnTelegraph telegraphPrefab;
        [SerializeField] int prewarmParticles = 128;

        /// <summary>A fold effect finished collapsing: center, clamped size, and whether it caught anything.</summary>
        public event Action<Vector2, float, bool> FoldPopped;

        readonly List<Particle> particles = new List<Particle>();
        readonly Stack<Particle> pool = new Stack<Particle>();
        readonly List<FoldFlash> activeFolds = new List<FoldFlash>();
        readonly List<SpawnTelegraph> activeTelegraphs = new List<SpawnTelegraph>();
        ObjectPool<FoldFlash> folds;
        ObjectPool<SpawnTelegraph> telegraphs;

        public void Init()
        {
            if (folds != null) return;
            folds = CreatePool(foldFlashPrefab);
            telegraphs = CreatePool(telegraphPrefab);
            for (int i = 0; i < prewarmParticles; i++)
            {
                var p = NewParticle();
                p.transform.gameObject.SetActive(false);
                pool.Push(p);
            }
        }

        public void Clear()
        {
            foreach (var p in particles) Recycle(p);
            particles.Clear();
            foreach (var f in activeFolds) folds.Release(f);
            activeFolds.Clear();
            foreach (var t in activeTelegraphs) telegraphs.Release(t);
            activeTelegraphs.Clear();
        }

        // ---- Particles ----

        /// <summary>Round glowing dots flung outward.</summary>
        public void Burst(Vector2 at, Color color, int count, float speed, float scale = 0.3f)
        {
            for (int i = 0; i < count; i++)
            {
                var p = Spawn(Kind.Dot, glowSprite, additive, at, color, Random.Range(0.35f, 0.7f), scale * Random.Range(0.7f, 1.2f));
                p.velocity = Random.insideUnitCircle.normalized * (speed * Random.Range(0.4f, 1f));
                p.spin = Random.Range(-360f, 360f);
            }
        }

        /// <summary>Streaks that stretch along their velocity and burn out.</summary>
        public void Sparks(Vector2 at, Color color, int count, float speed, float length = 0.28f, bool unscaled = false)
        {
            for (int i = 0; i < count; i++)
            {
                var p = Spawn(Kind.Spark, glowSprite, additiveHot, at, color, Random.Range(0.22f, 0.45f), Random.Range(0.07f, 0.11f));
                p.velocity = Random.insideUnitCircle.normalized * (speed * Random.Range(0.45f, 1f));
                p.length = length * Random.Range(0.6f, 1.3f);
                p.drag = 4.5f;
                p.unscaled = unscaled;
            }
        }

        /// <summary>Single tiny ember with a given velocity, for engine trails.</summary>
        public void Ember(Vector2 at, Vector2 velocity, Color color, float size, float life)
        {
            var p = Spawn(Kind.Dot, glowSprite, additive, at, color, life, size, SortOrder.WakeGlow);
            p.velocity = velocity;
            p.drag = 2f;
        }

        public void Glow(Vector2 at, Color color, float size, float life, bool unscaled = false)
        {
            var p = Spawn(Kind.Glow, glowSprite, additive, at, color, life, size);
            p.endSize = size * 1.35f;
            p.unscaled = unscaled;
        }

        /// <summary>Expanding ring. Unscaled by default so it plays through hitstop.</summary>
        public void Ring(Vector2 at, Color color, float fromRadius, float toRadius, float life, bool unscaled = true)
        {
            var p = Spawn(Kind.Ring, ringSprite, additive, at, color, life, fromRadius / MeshFactory.RingSpriteRadius);
            p.endSize = toRadius / MeshFactory.RingSpriteRadius;
            p.unscaled = unscaled;
        }

        /// <summary>Fading afterimage of a sprite.</summary>
        public void Ghost(Sprite sprite, Vector3 position, Quaternion rotation, float scale, Color color, float life)
        {
            var p = Spawn(Kind.Ghost, sprite, additive, position, color, life, scale, SortOrder.Player - 1);
            p.transform.rotation = rotation;
            p.endSize = scale * 0.8f;
        }

        // ---- Prefab effects ----

        public void PlayFold(IReadOnlyList<Vector2> loop, bool caught)
        {
            var flash = folds.Get();
            flash.Play(loop, caught);
            activeFolds.Add(flash);
        }

        public void PlayTelegraph(Vector2 at, float duration)
        {
            var telegraph = telegraphs.Get();
            telegraph.Play(at, duration);
            activeTelegraphs.Add(telegraph);
        }

        void Update()
        {
            UpdateParticles(Time.deltaTime, Time.unscaledDeltaTime);

            float now = Time.unscaledTime;
            for (int i = activeFolds.Count - 1; i >= 0; i--)
            {
                var flash = activeFolds[i];
                bool alive = flash.Tick(now, out bool popped);
                if (popped) Pop(flash);
                if (alive) continue;
                activeFolds.RemoveAt(i);
                folds.Release(flash);
            }

            for (int i = activeTelegraphs.Count - 1; i >= 0; i--)
            {
                if (activeTelegraphs[i].Tick()) continue;
                telegraphs.Release(activeTelegraphs[i]);
                activeTelegraphs.RemoveAt(i);
            }
        }

        void Pop(FoldFlash flash)
        {
            Vector2 center = flash.Center;
            float size = Mathf.Clamp(flash.Radius, 0.4f, 2.5f);
            if (flash.Caught)
            {
                Ring(center, Palette.You, 0.15f, 1.2f + size, 0.45f);
                Ring(center, Color.white, 0.1f, 0.6f + size * 0.5f, 0.25f);
                Sparks(center, Palette.YouHot, 18, 7.5f, 0.38f, true);
                Glow(center, Palette.YouHot, 1.6f + size, 0.3f, true);
            }
            else
            {
                Ring(center, Palette.Ore, 0.1f, 0.4f + size * 0.5f, 0.3f);
            }
            FoldPopped?.Invoke(center, size, flash.Caught);
        }

        void UpdateParticles(float dt, float udt)
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];
                float step = p.unscaled ? udt : dt;
                p.age += step;
                if (p.age >= p.life)
                {
                    Recycle(p);
                    particles.RemoveAt(i);
                    continue;
                }
                float k = p.age / p.life;
                p.velocity *= Mathf.Exp(-p.drag * step);
                p.transform.position += (Vector3)(p.velocity * step);

                switch (p.kind)
                {
                    case Kind.Dot:
                        p.transform.Rotate(0f, 0f, p.spin * step);
                        p.transform.localScale = Vector3.one * (p.size * (1f - k));
                        p.sprite.color = p.color.WithAlpha(p.color.a * (1f - k));
                        break;
                    case Kind.Spark:
                        float speed = p.velocity.magnitude;
                        if (speed > 0.01f) p.transform.rotation = Quaternion.Euler(0f, 0f, Geometry.HeadingOf(p.velocity));
                        float width = p.size * (1f - 0.5f * k);
                        p.transform.localScale = new Vector3(width, Mathf.Max(width, p.length * Mathf.Clamp01(speed / 3f)), 1f);
                        p.sprite.color = p.color.WithAlpha(p.color.a * (1f - k * k));
                        break;
                    case Kind.Ring:
                    case Kind.Glow:
                        p.transform.localScale = Vector3.one * Mathf.Lerp(p.size, p.endSize, Ease.OutCubic(k));
                        p.sprite.color = p.color.WithAlpha(p.color.a * (1f - k) * (1f - k));
                        break;
                    case Kind.Ghost:
                        p.transform.localScale = Vector3.one * Mathf.Lerp(p.size, p.endSize, k);
                        p.sprite.color = p.color.WithAlpha(p.color.a * (1f - k));
                        break;
                }
            }
        }

        Particle Spawn(Kind kind, Sprite sprite, Material material, Vector2 at, Color color, float life, float size, int order = SortOrder.Fx)
        {
            var p = pool.Count > 0 ? pool.Pop() : NewParticle();
            p.transform.gameObject.SetActive(true);
            p.transform.SetPositionAndRotation(at, Quaternion.identity);
            p.transform.localScale = Vector3.one * size;
            p.sprite.sprite = sprite;
            p.sprite.sharedMaterial = material;
            p.sprite.sortingOrder = order;
            p.sprite.color = color;
            p.kind = kind;
            p.color = color;
            p.velocity = Vector2.zero;
            p.age = 0f;
            p.life = Mathf.Max(0.01f, life);
            p.size = size;
            p.endSize = 0f;
            p.drag = 3f;
            p.spin = 0f;
            p.length = 0f;
            p.unscaled = false;
            particles.Add(p);
            return p;
        }

        Particle NewParticle()
        {
            var go = new GameObject("Particle");
            go.transform.SetParent(transform, false);
            return new Particle { transform = go.transform, sprite = go.AddComponent<SpriteRenderer>() };
        }

        void Recycle(Particle p)
        {
            p.transform.gameObject.SetActive(false);
            pool.Push(p);
        }

        ObjectPool<T> CreatePool<T>(T prefab) where T : Component
        {
            return new ObjectPool<T>(
                () =>
                {
                    var item = Instantiate(prefab, transform);
                    item.name = prefab.name;
                    item.gameObject.SetActive(false);
                    return item;
                },
                item => item.gameObject.SetActive(true),
                item => item.gameObject.SetActive(false),
                item => { if (item != null) Destroy(item.gameObject); },
                collectionCheck: false, defaultCapacity: 4, maxSize: 32);
        }
    }
}

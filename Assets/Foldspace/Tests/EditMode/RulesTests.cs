using System.Collections.Generic;
using Foldspace.Config;
using Foldspace.Utilities;
using NUnit.Framework;
using UnityEngine;

namespace Foldspace.Tests
{
    public class GeometryTests
    {
        static readonly List<Vector2> Square = new List<Vector2> { new Vector2(0, 0), new Vector2(2, 0), new Vector2(2, 2), new Vector2(0, 2) };

        [Test]
        public void Area_and_containment()
        {
            Assert.AreEqual(4f, Mathf.Abs(Geometry.SignedArea(Square)), 1e-4f);
            Assert.IsTrue(Geometry.PointInPolygon(new Vector2(1, 1), Square));
            Assert.IsFalse(Geometry.PointInPolygon(new Vector2(3, 1), Square));
        }

        [Test]
        public void Segment_intersection()
        {
            Assert.IsTrue(Geometry.SegmentIntersection(new Vector2(0, 0), new Vector2(2, 2), new Vector2(0, 2), new Vector2(2, 0), out var hit));
            Assert.AreEqual(1f, hit.x, 1e-4f);
            Assert.AreEqual(1f, hit.y, 1e-4f);
            Assert.IsFalse(Geometry.SegmentIntersection(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1), out _));
        }

        [Test]
        public void Heading_and_direction_round_trip()
        {
            foreach (float heading in new[] { 0f, 45f, 90f, -135f })
            {
                var direction = Geometry.Direction(heading);
                Assert.AreEqual(heading, Mathf.DeltaAngle(0f, Geometry.HeadingOf(direction)), 1e-3f);
            }
            Assert.AreEqual(Vector2.up.x, Geometry.Direction(0f).x, 1e-5f, "heading 0 faces up");
            Assert.AreEqual(Vector2.up.y, Geometry.Direction(0f).y, 1e-5f, "heading 0 faces up");
        }

        [Test]
        public void Clock_format()
        {
            Assert.AreEqual("0:00", Format.Clock(-3f));
            Assert.AreEqual("1:05", Format.Clock(65.9f));
            Assert.AreEqual("12:00", Format.Clock(720f));
        }
    }

    public class RunSessionTests
    {
        GameConfig config;
        RunSession session;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<GameConfig>();
            config.fold.chainWindow = 1.5f;
            config.progression.xpFirstLevel = 10;
            config.progression.xpPerLevel = 6;
            session = new RunSession();
            session.Begin(config);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(config);

        [Test]
        public void Folds_inside_the_window_build_a_chain()
        {
            Assert.AreEqual(1, session.RegisterFold(0f));
            Assert.AreEqual(2, session.RegisterFold(1f));
            Assert.AreEqual(1, session.RegisterFold(3f), "a gap longer than the window restarts the chain");
            Assert.AreEqual(2, session.BestChain);
            Assert.AreEqual(3, session.Folds);
        }

        [Test]
        public void Chain_expires_on_tick()
        {
            session.RegisterFold(0f);
            Assert.AreEqual(0.5f, session.ChainRemaining(0.75f), 1e-4f);
            session.Tick(0.1f, 1.4f);
            Assert.AreEqual(1, session.Chain);
            session.Tick(0.1f, 1.6f);
            Assert.AreEqual(0, session.Chain);
            Assert.AreEqual(0f, session.ChainRemaining(1.6f));
        }

        [Test]
        public void Fold_score_scales_with_kills_and_chain()
        {
            session.RegisterFold(0f);
            session.RegisterFold(1f);
            Assert.AreEqual(150, session.ScoreFold(3, 25));
            Assert.AreEqual(150, session.Score);
        }

        [Test]
        public void Xp_spills_over_multiple_levels()
        {
            for (int i = 0; i < 27; i++) session.CollectShard(i);
            Assert.AreEqual(2, session.ApplyLevelUps(config.progression));
            Assert.AreEqual(3, session.Level);
            Assert.AreEqual(1, session.Xp, "27 - 10 - 16");
            Assert.AreEqual(22, session.XpToNext);
            Assert.AreEqual(27, session.Score, "each shard is also a point");
        }

        [Test]
        public void Quick_pickups_build_a_combo()
        {
            Assert.AreEqual(0, session.CollectShard(0f));
            Assert.AreEqual(1, session.CollectShard(0.2f));
            Assert.AreEqual(2, session.CollectShard(0.4f));
            Assert.AreEqual(0, session.CollectShard(2f));
        }

        [Test]
        public void Begin_resets_the_run()
        {
            session.RegisterFold(0f);
            session.ScoreFold(2, 25);
            session.MarkNewBest();
            int firstRun = session.RunId;
            session.Begin(config);
            Assert.AreEqual(firstRun + 1, session.RunId);
            Assert.AreEqual(0, session.Score);
            Assert.AreEqual(0, session.Folds);
            Assert.AreEqual(1, session.Level);
            Assert.IsFalse(session.NewBest);
        }
    }
}

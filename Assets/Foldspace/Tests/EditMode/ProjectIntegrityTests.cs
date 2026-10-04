using System.Collections.Generic;
using System.Linq;
using Foldspace.Config;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Foldspace.Tests
{
    /// <summary>Catches broken wiring in the generated data assets, prefabs and scene before anything runs.</summary>
    public class ProjectIntegrityTests
    {
        const string Root = "Assets/Foldspace";
        const string ScenePath = Root + "/Scenes/Game.unity";

        [Test]
        public void Config_links_its_director_and_enemies()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(Root + "/Data/GameConfig.asset");
            Assert.IsNotNull(config, "GameConfig asset");
            Assert.IsNotNull(config.director, "GameConfig.director");
            foreach (var enemy in new[] { config.director.packEnemy, config.director.escortEnemy, config.director.eliteEnemy })
            {
                Assert.IsNotNull(enemy, "director enemy definition");
                Assert.IsNotNull(enemy.prefab, $"{enemy.name}.prefab");
            }
        }

        [Test]
        public void Prefabs_have_no_missing_references()
        {
            var problems = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Root + "/Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
                    Collect(transform.gameObject, path, problems);
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void Game_scene_is_in_the_build_and_fully_wired()
        {
            Assert.IsTrue(EditorBuildSettings.scenes.Any(s => s.path == ScenePath && s.enabled), "Game scene in build settings");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var problems = new List<string>();
                foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    Collect(transform.gameObject, ScenePath, problems);
                Assert.IsEmpty(problems, string.Join("\n", problems));

                var roots = scene.GetRootGameObjects().Select(g => g.name).ToArray();
                CollectionAssert.IsSubsetOf(new[] { "[Systems]", "[Cameras]", "[Environment]", "[UI]", "[World]" }, roots);
                Assert.AreEqual(1, scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameManager>(true)).Count());
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>Reports missing scripts and unassigned object references on Foldspace components.</summary>
        static void Collect(GameObject go, string asset, List<string> problems)
        {
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0)
                problems.Add($"{asset}: '{go.name}' has a missing script");

            foreach (var component in go.GetComponents<MonoBehaviour>())
            {
                if (component == null || component.GetType().Namespace?.StartsWith("Foldspace") != true) continue;
                var property = new SerializedObject(component).GetIterator();
                while (property.NextVisible(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference || property.name == "m_Script") continue;
                    if (property.objectReferenceValue == null)
                        problems.Add($"{asset}: {component.GetType().Name} on '{go.name}' has nothing in '{property.propertyPath}'");
                }
            }
        }
    }
}

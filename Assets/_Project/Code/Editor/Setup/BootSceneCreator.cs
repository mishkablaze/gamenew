using System.Collections.Generic;
using System.IO;
using FishNet.Managing;
using FishNet.Managing.Object;
using FishNet.Managing.Timing;
using FishNet.Managing.Transporting;
using FishNet.Transporting.Tugboat;
using Raidline.App.Boot;
using Raidline.Infrastructure.Logging;
using Raidline.Infrastructure.Ticking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Raidline.Editor.Setup
{
    /// <summary>
    /// Creates the Boot scene deterministically (CONVENTIONS §3: assets are made by editor scripts).
    /// Menu: Raidline/Setup/Create Boot Scene. Safe to re-run: an existing scene is rebuilt.
    /// </summary>
    public static class BootSceneCreator
    {
        public const string BootScenePath = "Assets/_Project/Scenes/Boot.unity";
        private const ushort TickRate = 60;

        [MenuItem("Raidline/Setup/Create Boot Scene", priority = 0)]
        public static void CreateBootScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject("AppRoot");

            // Fish-Net components are added explicitly so their settings are visible and versioned.
            var networkManager = root.AddComponent<NetworkManager>();
            var timeManager = root.AddComponent<TimeManager>();
            var transportManager = root.AddComponent<TransportManager>();
            var tugboat = root.AddComponent<Tugboat>();
            transportManager.Transport = tugboat;

            DefaultPrefabObjects prefabs = FindDefaultPrefabObjects();
            if (prefabs != null)
            {
                networkManager.SpawnablePrefabs = prefabs;
            }
            else
            {
                Log.Warn(LogCategory.Boot, "DefaultPrefabObjects asset not found; Fish-Net will try to assign it automatically.");
            }

            ConfigureTimeManager(timeManager);

            root.AddComponent<UpdateRunner>();
            var appRoot = root.AddComponent<AppRoot>();
            var appRootSo = new SerializedObject(appRoot);
            appRootSo.FindProperty("_networkManager").objectReferenceValue = networkManager;
            appRootSo.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(BootScenePath));
            EditorSceneManager.SaveScene(scene, BootScenePath);
            PutBootSceneFirstInBuild();
            AssetDatabase.SaveAssets();

            Log.Info(LogCategory.Boot, "Boot scene created at " + BootScenePath + " and set as build scene #0.");
        }

        private static void ConfigureTimeManager(TimeManager timeManager)
        {
            var so = new SerializedObject(timeManager);
            SerializedProperty tickRate = so.FindProperty("_tickRate");
            SerializedProperty physicsMode = so.FindProperty("_physicsMode");
            if (tickRate != null)
            {
                tickRate.intValue = TickRate;
            }

            if (physicsMode != null)
            {
                physicsMode.intValue = (int)PhysicsMode.TimeManager;
            }

            if (tickRate == null || physicsMode == null)
            {
                Log.Warn(LogCategory.Boot, "TimeManager fields were renamed in this Fish-Net version; AppRoot sets them at runtime anyway.");
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static DefaultPrefabObjects FindDefaultPrefabObjects()
        {
            string[] guids = AssetDatabase.FindAssets("t:DefaultPrefabObjects");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<DefaultPrefabObjects>(path);
                if (asset != null)
                {
                    return asset;
                }
            }

            return null;
        }

        private static void PutBootSceneFirstInBuild()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == BootScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(BootScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}

using YangVil.Data;
using YangVil.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace YangVil.EditorTools
{
    public static class WorldSceneBuilder
    {
        public const string VillagePath = "Assets/Scenes/Village.unity";
        public const string BattlePath = "Assets/Scenes/Battle.unity";
        const string ConfigPath = "Assets/Resources/GameConfig.asset";

        [MenuItem("YangVil/Build World Scenes")]
        public static void Build()
        {
            BuildVillage();
            BuildBattle();
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            config.villageScene = AddressablesSetup.Scene(VillagePath);
            config.battleScene = AddressablesSetup.Scene(BattlePath);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("WorldSceneBuilder: Village·Battle 씬 생성");
        }

        static Camera WorldCamera(float size, Color background, Vector3 position)
        {
            var go = new GameObject("WorldCamera");
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = size;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.depth = 0;
            go.transform.position = position;
            go.AddComponent<Physics2DRaycaster>();
            return cam;
        }

        static void BuildVillage()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = WorldCamera(8f, new Color(0.45f, 0.65f, 0.38f), new Vector3(0, 2, -10));
            var world = new GameObject("VillageWorld").AddComponent<VillageWorld>();
            world.cam = cam;
            world.font = UiPrefabBuilder.Font;
            EditorSceneManager.SaveScene(scene, VillagePath);
        }

        static void BuildBattle()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = WorldCamera(6f, new Color(0.62f, 0.82f, 0.95f), new Vector3(0, 0, -10));
            var world = new GameObject("BattleWorld").AddComponent<BattleWorld>();
            world.cam = cam;
            world.font = UiPrefabBuilder.Font;
            EditorSceneManager.SaveScene(scene, BattlePath);
        }
    }
}

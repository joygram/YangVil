using System;
using System.Collections;
using CatVillage.Data;
using CatVillage.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace CatVillage.Core
{
    public class SceneFlow : MonoBehaviour
    {
        public static SceneFlow Instance { get; private set; }

        public StageData PendingStage { get; private set; }

        ScreenManager _screens;
        GameConfig _config;
        AsyncOperationHandle<SceneInstance> _current;
        bool _busy;

        public void Init(GameConfig config, ScreenManager screens)
        {
            Instance = this;
            _config = config;
            _screens = screens;
        }

        public void GoVillage() => Go(_config.villageScene, () => _screens.Show<VillageHud>());

        public void GoBattle(StageData stage)
        {
            PendingStage = stage;
            Go(_config.battleScene, () => _screens.Show<BattleHud>());
        }

        void Go(AssetReference scene, Action onLoaded)
        {
            if (_busy) return;
            StartCoroutine(Switch(scene, onLoaded));
        }

        IEnumerator Switch(AssetReference scene, Action onLoaded)
        {
            _busy = true;
            _screens.ShowLoading(true);
            if (_current.IsValid())
            {
                yield return Addressables.UnloadSceneAsync(_current);
                _current = default;
            }
            yield return Resources.UnloadUnusedAssets();
            GC.Collect();

            _current = Addressables.LoadSceneAsync(scene, LoadSceneMode.Additive);
            yield return _current;
            _busy = false;
            _screens.ShowLoading(false);
            if (_current.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"SceneFlow: {scene.RuntimeKey} 로드 실패");
                yield break;
            }
            SceneManager.SetActiveScene(_current.Result.Scene);
            onLoaded();
        }
    }
}

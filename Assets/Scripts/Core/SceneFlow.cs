using System;
using System.Collections;
using YangVil.Data;
using YangVil.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace YangVil.Core
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

        public void GoVillage() => GoVillage(false);

        public void GoVillage(bool openStages) => Go(_config.villageScene, () =>
        {
            if (openStages) _screens.Show<StageSelectScreen>();
            else _screens.Show<VillageHud>();
        });

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
            while (!_current.IsDone) yield return null;
            _busy = false;
            _screens.ShowLoading(false);
            if (_current.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"SceneFlow: {scene.RuntimeKey} 로드 실패 {_current.OperationException}");
                yield break;
            }
            SceneManager.SetActiveScene(_current.Result.Scene);
            Debug.Log($"SceneFlow: {_current.Result.Scene.name} 로드 완료");
            onLoaded();
        }
    }
}

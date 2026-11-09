using System;
using System.Collections;
using YangVil.Core;
using UnityEngine;

namespace YangVil.UI
{
    public class ScreenManager : MonoBehaviour
    {
        public GameObject loadingIndicator;

        public Game Game { get; private set; }

        RectTransform _layer;
        GameScreen _current;
        AssetScope _scope;
        int _switches;
        bool _busy;

        public void Init(Game game, RectTransform layer)
        {
            Game = game;
            _layer = layer;
            Application.lowMemory += OnLowMemory;
        }

        void OnDestroy()
        {
            Application.lowMemory -= OnLowMemory;
            _scope?.Dispose();
        }

        public void ShowLoading(bool on)
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(on);
        }

        Action _pending;

        public void Show<T>(Action<T> setup = null) where T : GameScreen
        {
            if (_busy)
            {
                _pending = () => Show(setup);
                return;
            }
            StartCoroutine(Switch(setup));
        }

        void RunPending()
        {
            var next = _pending;
            _pending = null;
            next?.Invoke();
        }

        IEnumerator Switch<T>(Action<T> setup) where T : GameScreen
        {
            _busy = true;
            if (loadingIndicator != null) loadingIndicator.SetActive(true);

            _current = null;
            _scope?.Dispose();
            yield return Unload();

            var reference = Game.Config.Screen(typeof(T).Name);
            if (reference == null) throw new InvalidOperationException($"{typeof(T).Name} 가 GameConfig.screens 에 없습니다.");

            _scope = new AssetScope();
            GameObject instance = null;
            yield return _scope.Instantiate(reference, _layer, go => instance = go);

            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            _busy = false;
            if (instance == null)
            {
                Debug.LogError($"ScreenManager: {typeof(T).Name} 생성 실패");
                RunPending();
                yield break;
            }

            if (_pending != null)
            {
                RunPending();
                yield break;
            }

            var screen = instance.GetComponent<T>();
            setup?.Invoke(screen);
            screen.Init(this, _scope);
            _current = screen;
        }

        IEnumerator Unload()
        {
            var policy = Game.Config.memory;
            _switches++;
            if (!policy.unloadUnusedOnScreenChange || _switches % Mathf.Max(1, policy.unloadEveryNScreens) != 0) yield break;
            yield return Resources.UnloadUnusedAssets();
            if (policy.collectGarbageOnUnload) GC.Collect();
        }

        void OnLowMemory()
        {
            if (!Game.Config.memory.releaseOnLowMemory) return;
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }
    }
}

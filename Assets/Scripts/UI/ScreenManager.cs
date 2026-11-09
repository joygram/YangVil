using System;
using System.Collections;
using CatVillage.Core;
using UnityEngine;

namespace CatVillage.UI
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

        public void Show<T>(Action<T> setup = null) where T : GameScreen
        {
            if (_busy) return;
            StartCoroutine(Switch(setup));
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
            if (instance == null) yield break;

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

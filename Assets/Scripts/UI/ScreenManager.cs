using System;
using CatVillage.Core;
using UnityEngine;

namespace CatVillage.UI
{
    public class ScreenManager : MonoBehaviour
    {
        public Game Game { get; private set; }

        RectTransform _layer;
        GameScreen _current;

        public void Init(Game game, RectTransform layer)
        {
            Game = game;
            _layer = layer;
        }

        public T Show<T>(Action<T> setup = null) where T : GameScreen
        {
            var prefab = Game.Config.screenPrefabs.Find(p => p != null && p.GetComponent<T>() != null);
            if (prefab == null) throw new InvalidOperationException($"{typeof(T).Name} 프리팹이 GameConfig.screenPrefabs 에 없습니다.");
            if (_current != null) Destroy(_current.gameObject);
            var screen = Instantiate(prefab, _layer, false).GetComponent<T>();
            setup?.Invoke(screen);
            screen.Init(this);
            _current = screen;
            return screen;
        }
    }
}

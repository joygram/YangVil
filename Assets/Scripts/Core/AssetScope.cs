using System;
using System.Collections;
using System.Collections.Generic;
using CatVillage.Data;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CatVillage.Core
{
    public class AssetScope : IDisposable
    {
        readonly Dictionary<object, AsyncOperationHandle> _handles = new Dictionary<object, AsyncOperationHandle>();
        readonly Dictionary<(Texture2D, Rect), Sprite> _sprites = new Dictionary<(Texture2D, Rect), Sprite>();
        readonly List<GameObject> _instances = new List<GameObject>();

        public bool Disposed { get; private set; }

        public AsyncOperationHandle<T> Load<T>(AssetReference reference) where T : UnityEngine.Object
        {
            var key = reference.RuntimeKey;
            if (_handles.TryGetValue(key, out var existing)) return existing.Convert<T>();
            var handle = Addressables.LoadAssetAsync<T>(reference);
            _handles[key] = handle;
            return handle;
        }

        public IEnumerator Instantiate(AssetReferenceGameObject reference, Transform parent, Action<GameObject> done)
        {
            var handle = Addressables.InstantiateAsync(reference, parent, false);
            yield return handle;
            if (Disposed)
            {
                Addressables.ReleaseInstance(handle);
                yield break;
            }
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"AssetScope: {reference.RuntimeKey} 생성 실패");
                yield break;
            }
            _instances.Add(handle.Result);
            done(handle.Result);
        }

        public IEnumerator LoadFrames(SpriteSet set, Action<Sprite[], Sprite[]> done)
        {
            if (set.UsesSheet)
            {
                var sheet = Load<Texture2D>(set.sheet);
                yield return sheet;
                if (Disposed || sheet.Status != AsyncOperationStatus.Succeeded) yield break;
                done(FromSheet(set, sheet.Result, set.idleRow, set.idleFrames),
                    FromSheet(set, sheet.Result, set.attackRow, set.attackFrames));
                yield break;
            }

            var idle = new List<AsyncOperationHandle<Texture2D>>();
            var attack = new List<AsyncOperationHandle<Texture2D>>();
            foreach (var r in set.idle) idle.Add(Load<Texture2D>(r));
            foreach (var r in set.attack) attack.Add(Load<Texture2D>(r));
            foreach (var h in idle) yield return h;
            foreach (var h in attack) yield return h;
            if (Disposed) yield break;
            done(FromFrames(idle), FromFrames(attack));
        }

        Sprite[] FromFrames(List<AsyncOperationHandle<Texture2D>> handles)
        {
            var result = new List<Sprite>();
            foreach (var h in handles)
                if (h.Status == AsyncOperationStatus.Succeeded)
                    result.Add(Sprite(h.Result, new Rect(0, 0, h.Result.width, h.Result.height)));
            return result.ToArray();
        }

        Sprite[] FromSheet(SpriteSet set, Texture2D t, int row, int count)
        {
            var cell = set.cellSize;
            var rows = t.height / cell;
            var n = Mathf.Min(count, set.framesPerRow);
            var result = new Sprite[n];
            for (var i = 0; i < n; i++)
                result[i] = Sprite(t, new Rect(i * cell, (rows - 1 - row) * cell, cell, cell));
            return result;
        }

        Sprite Sprite(Texture2D t, Rect rect)
        {
            if (_sprites.TryGetValue((t, rect), out var s)) return s;
            s = UnityEngine.Sprite.Create(t, rect, new Vector2(0.5f, 0f), rect.width);
            _sprites[(t, rect)] = s;
            return s;
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            foreach (var s in _sprites.Values) UnityEngine.Object.Destroy(s);
            _sprites.Clear();
            foreach (var go in _instances)
                if (go != null) Addressables.ReleaseInstance(go);
            _instances.Clear();
            foreach (var h in _handles.Values)
                if (h.IsValid()) Addressables.Release(h);
            _handles.Clear();
        }
    }
}

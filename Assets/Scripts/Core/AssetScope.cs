using System;
using System.Collections;
using System.Collections.Generic;
using YangVil.Data;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace YangVil.Core
{
    public class AssetScope : IDisposable
    {
        readonly Dictionary<object, AsyncOperationHandle> _handles = new Dictionary<object, AsyncOperationHandle>();
        readonly Dictionary<(Texture2D, Rect), Sprite> _sprites = new Dictionary<(Texture2D, Rect), Sprite>();
        readonly List<GameObject> _instances = new List<GameObject>();
        readonly List<Sprite> _looseSprites = new List<Sprite>();

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

        public IEnumerator LoadFrames(SpriteSet set, Action<FrameSet> done)
        {
            var frames = new FrameSet { Fps = set.fps };
            if (set.UsesSheet)
            {
                var sheet = Load<Texture2D>(set.sheet);
                yield return sheet;
                if (Disposed || sheet.Status != AsyncOperationStatus.Succeeded) yield break;
                frames.Idle = FromSheet(set, sheet.Result, set.idleRow, set.idleFrames);
                frames.Walk = FromSheet(set, sheet.Result, set.walkRow, set.walkFrames);
                frames.Attack = FromSheet(set, sheet.Result, set.attackRow, set.attackFrames);
                frames.Hurt = FromSheet(set, sheet.Result, set.hurtRow, set.hurtFrames);
                done(frames);
                yield break;
            }

            var lists = new[] { set.idle, set.walk, set.attack, set.hurt };
            var handles = new List<AsyncOperationHandle<Texture2D>>[lists.Length];
            for (var i = 0; i < lists.Length; i++)
            {
                handles[i] = new List<AsyncOperationHandle<Texture2D>>();
                if (lists[i] == null) continue;
                foreach (var r in lists[i])
                    if (r != null && r.RuntimeKeyIsValid()) handles[i].Add(Load<Texture2D>(r));
            }
            foreach (var list in handles)
                foreach (var h in list) yield return h;
            if (Disposed) yield break;
            frames.Idle = FromFrames(handles[0]);
            frames.Walk = FromFrames(handles[1]);
            frames.Attack = FromFrames(handles[2]);
            frames.Hurt = FromFrames(handles[3]);
            done(frames);
        }

        readonly Dictionary<string, Sprite> _painted = new Dictionary<string, Sprite>();
        readonly List<Texture2D> _paintedTextures = new List<Texture2D>();

        public IEnumerator LoadCatFrames(SpriteSet set, CatGenes genes, Action<FrameSet> done)
        {
            if (genes == null)
            {
                yield return LoadFrames(set, done);
                yield break;
            }
            FrameSet baseFrames = null;
            yield return LoadFrames(set, f => baseFrames = f);
            if (Disposed || baseFrames == null) yield break;
            done(new FrameSet
            {
                Fps = baseFrames.Fps,
                Idle = PaintAll(baseFrames.Idle, genes),
                Walk = PaintAll(baseFrames.Walk, genes),
                Attack = PaintAll(baseFrames.Attack, genes),
                Hurt = PaintAll(baseFrames.Hurt, genes)
            });
        }

        Sprite[] PaintAll(Sprite[] frames, CatGenes genes)
        {
            var result = new Sprite[frames.Length];
            for (var i = 0; i < frames.Length; i++)
            {
                var key = $"{frames[i].texture.name}:{frames[i].rect}#{genes.Key}";
                if (!_painted.TryGetValue(key, out var s))
                {
                    var t = frames[i].texture.isReadable ? CoatPainter.Paint(frames[i], genes) : null;
                    if (t == null)
                    {
                        result[i] = frames[i];
                        continue;
                    }
                    _paintedTextures.Add(t);
                    s = UnityEngine.Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0f), t.width);
                    _painted[key] = s;
                }
                result[i] = s;
            }
            return result;
        }

        public IEnumerator LoadSprite(AssetReferenceTexture2D reference, Vector2 pivot, float pixelsPerUnit, Action<Sprite> done)
        {
            if (reference == null || !reference.RuntimeKeyIsValid()) yield break;
            var h = Load<Texture2D>(reference);
            yield return h;
            if (Disposed || h.Status != AsyncOperationStatus.Succeeded) yield break;
            var t = h.Result;
            var s = UnityEngine.Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivot, pixelsPerUnit);
            _looseSprites.Add(s);
            done(s);
        }

        public IEnumerator LoadSlicedSprite(AssetReferenceTexture2D reference, Vector2 pivot, float pixelsPerUnit, int border, Action<Sprite> done)
        {
            if (reference == null || !reference.RuntimeKeyIsValid()) yield break;
            var h = Load<Texture2D>(reference);
            yield return h;
            if (Disposed || h.Status != AsyncOperationStatus.Succeeded) yield break;
            var t = h.Result;
            var s = UnityEngine.Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivot, pixelsPerUnit, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            _looseSprites.Add(s);
            done(s);
        }

        public IEnumerator LoadClip(AssetReference reference, Action<AudioClip> done)
        {
            if (reference == null || !reference.RuntimeKeyIsValid()) yield break;
            var h = Load<AudioClip>(reference);
            yield return h;
            if (Disposed || h.Status != AsyncOperationStatus.Succeeded) yield break;
            done(h.Result);
        }

        public IEnumerator Spawn(AssetReferenceGameObject reference, Vector3 position, Transform parent, float lifetime)
        {
            if (reference == null || !reference.RuntimeKeyIsValid()) yield break;
            var handle = Addressables.InstantiateAsync(reference, position, Quaternion.identity, parent);
            yield return handle;
            if (handle.Status != AsyncOperationStatus.Succeeded) yield break;
            if (Disposed)
            {
                Addressables.ReleaseInstance(handle);
                yield break;
            }
            _instances.Add(handle.Result);
            yield return new WaitForSeconds(lifetime);
            if (Disposed || handle.Result == null) yield break;
            _instances.Remove(handle.Result);
            Addressables.ReleaseInstance(handle.Result);
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
            if (row < 0 || row >= rows || count <= 0) return new Sprite[0];
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
            foreach (var s in _looseSprites) UnityEngine.Object.Destroy(s);
            _looseSprites.Clear();
            foreach (var s in _painted.Values) UnityEngine.Object.Destroy(s);
            _painted.Clear();
            foreach (var t in _paintedTextures) UnityEngine.Object.Destroy(t);
            _paintedTextures.Clear();
            foreach (var go in _instances)
                if (go != null) Addressables.ReleaseInstance(go);
            _instances.Clear();
            foreach (var h in _handles.Values)
                if (h.IsValid()) Addressables.Release(h);
            _handles.Clear();
        }
    }
}

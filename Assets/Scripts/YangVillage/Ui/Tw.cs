using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// Godot Tween·SceneTreeTimer 대응. 단계는 차례로, Parallel()/SetParallel() 은 같은 단계에 묶음.
// 시간 = Time.deltaTime (Godot Engine.TimeScale = Unity Time.timeScale).
namespace YangVillage.Ui
{
    public enum Trans { Linear, Sine, Quad, Quart, Back, Bounce }
    public enum Ease { In, Out, InOut }

    public sealed class Tw
    {
        sealed class Track
        {
            public float Duration;
            public Action Start;
            public Action<float> Apply;
            public Trans Trans;
            public Ease Ease;
            public bool Started;
        }

        readonly List<List<Track>> _steps = new List<List<Track>>();
        readonly UnityEngine.Object _owner;
        readonly TaskCompletionSource<bool> _done = new TaskCompletionSource<bool>();
        Trans _trans = Trans.Linear;
        Ease _ease = Ease.InOut;
        bool _joinNext, _parallel;
        int _loops = 1, _loop;
        int _step;
        float _time;
        bool _dead;
        Track _last;

        public Task Finished => _done.Task;
        public event Action OnFinished;
        public bool Running => !_dead;

        Tw(UnityEngine.Object owner) { _owner = owner; }

        public static Tw Create(UnityEngine.Object owner)
        {
            var tw = new Tw(owner);
            Runner.Instance.Add(tw);
            return tw;
        }

        public Tw SetTrans(Trans t) { if (_last != null) _last.Trans = t; else _trans = t; return this; }
        public Tw SetEase(Ease e) { if (_last != null) _last.Ease = e; else _ease = e; return this; }
        public Tw SetDefaultTrans(Trans t) { _trans = t; return this; }
        public Tw SetDefaultEase(Ease e) { _ease = e; return this; }
        public Tw SetLoops(int loops = 0) { _loops = loops; return this; }
        public Tw SetParallel() { _parallel = true; return this; }
        public Tw Parallel() { _joinNext = true; return this; }

        Tw Add(Track track)
        {
            track.Trans = _trans;
            track.Ease = _ease;
            if ((_joinNext || _parallel) && _steps.Count > 0) _steps[_steps.Count - 1].Add(track);
            else _steps.Add(new List<Track> { track });
            _joinNext = false;
            _last = track;
            return this;
        }

        public Tw Prop<T>(Func<T> get, Action<T> set, T to, float seconds, Func<T, T, float, T> lerp)
        {
            T from = default;
            return Add(new Track { Duration = seconds, Start = () => from = get(), Apply = k => set(lerp(from, to, k)) });
        }

        public Tw Float(Func<float> get, Action<float> set, float to, float seconds) => Prop(get, set, to, seconds, Mathf.LerpUnclamped);
        public Tw Vec(Func<Vector2> get, Action<Vector2> set, Vector2 to, float seconds) => Prop(get, set, to, seconds, Vector2.LerpUnclamped);
        public Tw Col(Func<Color> get, Action<Color> set, Color to, float seconds) => Prop(get, set, to, seconds, Color.LerpUnclamped);

        public Tw Method(Action<float> apply, float from, float to, float seconds) =>
            Add(new Track { Duration = seconds, Apply = k => apply(Mathf.LerpUnclamped(from, to, k)) });

        public Tw Interval(float seconds) => Add(new Track { Duration = seconds });

        public Tw Callback(Action action) => Add(new Track { Duration = 0, Start = action });

        public void Kill()
        {
            if (_dead) return;
            _dead = true;
            _done.TrySetResult(false);
        }

        // false = 끝남.
        internal bool Tick(float delta)
        {
            if (_dead) return false;
            if (_owner == null) { Kill(); return false; }
            if (_steps.Count == 0) { Complete(); return false; }
            _time += delta;
            while (true)
            {
                var step = _steps[_step];
                var longest = 0f;
                foreach (var t in step)
                {
                    if (!t.Started) { t.Started = true; t.Start?.Invoke(); if (_dead || _owner == null) return false; }
                    longest = Mathf.Max(longest, t.Duration);
                    var k = t.Duration <= 0 ? 1 : Mathf.Clamp01(_time / t.Duration);
                    t.Apply?.Invoke(Eval(t.Trans, t.Ease, k));
                }
                if (_time < longest) return true;
                _time -= longest;
                _step++;
                if (_step < _steps.Count) continue;
                _loop++;
                if (_loops != 0 && _loop >= _loops) { Complete(); return false; }
                _step = 0;
                foreach (var s in _steps) foreach (var t in s) t.Started = false;
                if (longest <= 0 && _steps.Count == 1) return true;
            }
        }

        void Complete()
        {
            _dead = true;
            OnFinished?.Invoke();
            _done.TrySetResult(true);
        }

        static float Eval(Trans trans, Ease ease, float k)
        {
            switch (ease)
            {
                case Ease.In: return In(trans, k);
                case Ease.Out: return 1 - In(trans, 1 - k);
                default: return k < 0.5f ? In(trans, k * 2) / 2 : 1 - In(trans, (1 - k) * 2) / 2;
            }
        }

        static float In(Trans trans, float k)
        {
            switch (trans)
            {
                case Trans.Sine: return 1 - Mathf.Cos(k * Mathf.PI / 2);
                case Trans.Quad: return k * k;
                case Trans.Quart: return k * k * k * k;
                case Trans.Back: return k * k * (2.70158f * k - 1.70158f);
                case Trans.Bounce: return 1 - BounceOut(1 - k);
                default: return k;
            }
        }

        static float BounceOut(float k)
        {
            if (k < 1 / 2.75f) return 7.5625f * k * k;
            if (k < 2 / 2.75f) { k -= 1.5f / 2.75f; return 7.5625f * k * k + 0.75f; }
            if (k < 2.5f / 2.75f) { k -= 2.25f / 2.75f; return 7.5625f * k * k + 0.9375f; }
            k -= 2.625f / 2.75f;
            return 7.5625f * k * k + 0.984375f;
        }

        // 타이머: realTime 이면 시간 배율 무시 (Godot CreateTimer ignoreTimeScale).
        public static Task Wait(float seconds, bool realTime = false) => Runner.Instance.Timer(seconds, realTime);

        sealed class Runner : MonoBehaviour
        {
            static Runner _instance;
            readonly List<Tw> _tweens = new List<Tw>();
            readonly List<(float left, bool real, TaskCompletionSource<bool> tcs)> _timers = new List<(float, bool, TaskCompletionSource<bool>)>();

            public static Runner Instance
            {
                get
                {
                    if (_instance != null) return _instance;
                    var go = new GameObject("TwRunner");
                    DontDestroyOnLoad(go);
                    return _instance = go.AddComponent<Runner>();
                }
            }

            public void Add(Tw tw) => _tweens.Add(tw);

            public Task Timer(float seconds, bool real)
            {
                var tcs = new TaskCompletionSource<bool>();
                _timers.Add((seconds, real, tcs));
                return tcs.Task;
            }

            void Update()
            {
                for (var i = 0; i < _tweens.Count; i++)
                    if (!_tweens[i].Tick(Time.deltaTime)) _tweens.RemoveAt(i--);
                for (var i = 0; i < _timers.Count; i++)
                {
                    var (left, real, tcs) = _timers[i];
                    left -= real ? Time.unscaledDeltaTime : Time.deltaTime;
                    if (left > 0) { _timers[i] = (left, real, tcs); continue; }
                    _timers.RemoveAt(i--);
                    tcs.TrySetResult(true);
                }
            }
        }
    }
}

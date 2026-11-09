using System;
using System.Collections;
using YangVil.Core;
using UnityEngine;

namespace YangVil.World
{
    public enum ActorPose { Idle, Walk, Attack, Hurt }

    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteActor : MonoBehaviour
    {
        public float height = 1.6f;

        SpriteRenderer _renderer;
        FrameSet _frames;
        Sprite[] _current;
        ActorPose _pose;
        float _time;
        Color _tint = Color.white;
        Action _onceDone;

        public SpriteRenderer Renderer => _renderer != null ? _renderer : _renderer = GetComponent<SpriteRenderer>();
        public bool Ready => _frames != null;
        public float VisibleTop
        {
            get
            {
                var sprite = Renderer.sprite;
                if (sprite == null || !sprite.texture.isReadable) return height;
                var pixels = sprite.texture.GetPixels32();
                var rect = sprite.rect;
                for (var y = (int)rect.yMax - 1; y >= (int)rect.yMin; y--)
                    for (var x = (int)rect.xMin; x < (int)rect.xMax; x++)
                        if (pixels[y * sprite.texture.width + x].a > 10)
                            return (y - rect.yMin + 1 - sprite.pivot.y) / sprite.pixelsPerUnit * transform.localScale.y;
                return height;
            }
        }

        public void SetFrames(FrameSet frames)
        {
            if (this == null) return;
            _frames = frames;
            Play(ActorPose.Idle);
        }

        public void SetTint(Color tint)
        {
            _tint = tint;
            Renderer.color = tint;
        }

        public void Face(bool right) => Renderer.flipX = !right;

        public void SetHeight(float worldHeight)
        {
            height = worldHeight;
            ApplyHeight();
        }

        void ApplyHeight()
        {
            var sprite = Renderer.sprite;
            transform.localScale = Vector3.one * (sprite == null ? height : height / Mathf.Max(0.001f, sprite.bounds.size.y));
        }

        public void Play(ActorPose pose, Action onceDone = null)
        {
            if (_frames == null) return;
            _pose = pose;
            _onceDone = onceDone;
            _time = 0;
            _current = pose switch
            {
                ActorPose.Walk => _frames.WalkOrIdle,
                ActorPose.Attack => _frames.AttackOrIdle,
                ActorPose.Hurt => _frames.HurtOrIdle,
                _ => _frames.Idle
            };
            if (_current.Length > 0) { Renderer.sprite = _current[0]; ApplyHeight(); }
        }

        void Update()
        {
            if (_current == null || _current.Length == 0) return;
            _time += Time.deltaTime;
            var frame = (int)(_time * _frames.Fps);
            var once = _pose == ActorPose.Attack || _pose == ActorPose.Hurt;
            if (once && frame >= _current.Length)
            {
                var done = _onceDone;
                Play(ActorPose.Idle);
                done?.Invoke();
                return;
            }
            Renderer.sprite = _current[frame % _current.Length];
            ApplyHeight();
        }

        public IEnumerator Flash(Color color, float seconds)
        {
            Renderer.color = color;
            yield return new WaitForSecondsRealtime(seconds);
            Renderer.color = _tint;
        }

        public void SetDead(bool dead) => Renderer.color = dead ? new Color(0.3f, 0.3f, 0.3f, 0.45f) : _tint;
    }
}

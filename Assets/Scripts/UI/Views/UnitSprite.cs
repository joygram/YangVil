using System.Collections;
using CatVillage.Core;
using CatVillage.Data;
using UnityEngine;
using UnityEngine.UI;

namespace CatVillage.UI
{
    public class UnitSprite : MonoBehaviour
    {
        public float fps = 6f;

        Image _image;
        Sprite[] _idle;
        Sprite[] _attack;
        Sprite[] _current;
        float _time;
        bool _flipped;

        public Image Image => _image != null ? _image : _image = GetComponent<Image>();

        public void Bind(SpriteSet set, Color tint)
        {
            _image = GetComponent<Image>();
            _image.color = tint;
            _idle = SpriteLibrary.Idle(set);
            _attack = SpriteLibrary.Attack(set);
            _current = _idle;
            if (_idle.Length > 0) _image.sprite = _idle[0];
        }

        public void Face(bool left)
        {
            _flipped = left;
            var s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (left ? -1 : 1);
            transform.localScale = s;
        }

        public void SetScale(float scale)
        {
            transform.localScale = new Vector3(_flipped ? -scale : scale, scale, 1);
        }

        void Update()
        {
            if (_current == null || _current.Length == 0) return;
            _time += Time.deltaTime;
            var frame = (int)(_time * fps);
            if (_current == _attack && frame >= _attack.Length)
            {
                _current = _idle;
                _time = 0;
                frame = 0;
            }
            _image.sprite = _current[frame % _current.Length];
        }

        public void PlayAttack()
        {
            _current = _attack;
            _time = 0;
        }

        public IEnumerator Lunge(Vector2 toward, float duration)
        {
            var rt = (RectTransform)transform;
            var start = rt.anchoredPosition;
            var target = Vector2.Lerp(start, toward, 0.35f);
            for (var t = 0f; t < 1f; t += Time.deltaTime / (duration / 2))
            {
                rt.anchoredPosition = Vector2.Lerp(start, target, t);
                yield return null;
            }
            for (var t = 0f; t < 1f; t += Time.deltaTime / (duration / 2))
            {
                rt.anchoredPosition = Vector2.Lerp(target, start, t);
                yield return null;
            }
            rt.anchoredPosition = start;
        }

        public IEnumerator Flash(Color color, float duration)
        {
            var original = _image.color;
            _image.color = color;
            yield return new WaitForSeconds(duration);
            _image.color = original;
        }
    }
}

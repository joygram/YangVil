using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Godot YangVillage CatWalker.cs 이식. 좌표 = 마당 기준 왼쪽 위 원점, 아래로 +y (Godot 와 동일).
namespace YangVillage.Ui
{
    // 마을 마당을 어슬렁거리는 고양이 한 마리.
    public class CatWalker : MonoBehaviour, IPointerClickHandler
    {
        const float Speed = 36;
        static readonly System.Random Rng = new System.Random();

        public Action Tapped;
        public Rect Bounds;
        public Vector2 Size;
        public Vector2 Position;
        Vector2 _target;
        double _rest, _hop;
        const float HopHeight = 5;
        const float HopRate = 10;
        const float Spacing = 0.42f;
        const int Tries = 24;

        SpriteView _view;
        bool _flip;

        public static CatWalker Create(Transform yard, string spriteId, float size, Rect bounds)
        {
            var root = Kit.Node("cat", yard);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0, 1);
            root.sizeDelta = new Vector2(size, size);
            var walker = root.gameObject.AddComponent<CatWalker>();
            walker.Size = new Vector2(size, size);
            walker.Bounds = bounds;
            walker._view = SpriteView.Of(spriteId, size, root);
            Kit.Fill(walker._view.Rect);
            walker._view.Image.raycastTarget = true;
            return walker;
        }

        public void Place()
        {
            Position = _target = FreePoint();
            _rest = Rng.NextDouble() * 2;
            Apply();
        }

        Vector2 RandomPoint() => new Vector2(
            Bounds.x + (float)Rng.NextDouble() * Math.Max(1, Bounds.width - Size.x),
            Bounds.y + (float)Rng.NextDouble() * Math.Max(1, Bounds.height - Size.y));

        // 다른 고양이가 서 있거나 가려는 자리를 피해 고른다.
        Vector2 FreePoint()
        {
            var point = RandomPoint();
            for (var i = 0; i < Tries && Crowded(point); i++) point = RandomPoint();
            return point;
        }

        float Room(CatWalker other) => (Size.x + other.Size.x) * Spacing;

        bool Crowded(Vector2 point)
        {
            foreach (Transform node in transform.parent)
            {
                var other = node.GetComponent<CatWalker>();
                if (other != null && other != this && Vector2.Distance(point + Size / 2, other._target + other.Size / 2) < Room(other)) return true;
            }
            return false;
        }

        // 지나가다 붙으면 서로 비켜선다.
        void StepAside(float delta)
        {
            foreach (Transform node in transform.parent)
            {
                var other = node.GetComponent<CatWalker>();
                if (other == null || other == this) continue;
                var away = Position + Size / 2 - other.Position - other.Size / 2;
                if (away.magnitude >= Room(other) || away == Vector2.zero) continue;
                Position += away.normalized * Speed * delta;
            }
            Position = Vector2.Max(Bounds.min, Vector2.Min(Position, Bounds.max - Size));
        }

        // 마당 정렬 기준 (Godot ZIndex).
        public float SortY => Position.y + Size.y;

        void Apply()
        {
            ((RectTransform)transform).anchoredPosition = new Vector2(Position.x, -Position.y);
            _view.transform.localScale = new Vector3(_flip ? -1 : 1, 1, 1);
        }

        void Update()
        {
            var delta = Time.deltaTime;
            StepAside(delta);
            Apply();
            if (_rest > 0)
            {
                _rest -= delta;
                if (_rest <= 0)
                {
                    _target = FreePoint();
                    _flip = _target.x < Position.x;
                    _hop = 0;
                }
                return;
            }
            _hop += delta;
            Position = Vector2.MoveTowards(Position, _target, Speed * delta);
            Apply();
            // Godot Rotation(시계 방향 +) → Unity z 회전(반시계 +): 부호 반전.
            _view.transform.localRotation = Quaternion.Euler(0, 0, -Mathf.Sin((float)_hop * HopRate) * HopHeight);
            if (Vector2.Distance(Position, _target) > 1) return;
            _rest = 1.5 + Rng.NextDouble() * 3;
            _view.transform.localRotation = Quaternion.identity;
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left) Tapped?.Invoke();
        }
    }
}

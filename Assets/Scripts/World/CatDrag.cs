using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CatVillage.World
{
    public class CatDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public Camera cam;
        public float liftScale = 1.25f;
        public float liftHeight = 0.35f;
        public Action Began;
        public Action<Vector2> Hovered;
        public Action<Vector2> Dropped;

        CatWanderer _wanderer;
        SpriteActor _actor;
        SpriteRenderer _shadow;
        Vector3 _home;
        Vector3 _scale;
        int _order;
        bool _dragging;

        public bool Dragging => _dragging;

        void Awake()
        {
            _wanderer = GetComponent<CatWanderer>();
            _actor = GetComponent<SpriteActor>();
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (!_dragging && !VillageCamera.Locked) GameCursor.Set(CursorKind.Hand);
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (!_dragging) GameCursor.Set(CursorKind.Default);
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (VillageCamera.Locked) return;
            _dragging = true;
            _home = transform.position;
            _scale = transform.localScale;
            if (_wanderer != null) _wanderer.enabled = false;
            transform.localScale = _scale * liftScale;
            if (_actor != null)
            {
                _order = _actor.Renderer.sortingOrder;
                _actor.Renderer.sortingOrder = 500;
                _actor.Play(ActorPose.Walk);
            }
            _shadow = WorldKit.Box(transform.parent, "PickShadow", Vector2.zero, new Vector2(0.9f, 0.25f), new Color(0, 0, 0, 0.35f), 499);
            GameCursor.Set(CursorKind.Grab);
            Began?.Invoke();
            Follow(e.position);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_dragging) return;
            Follow(e.position);
        }

        void Follow(Vector2 screen)
        {
            var p = (Vector2)cam.ScreenToWorldPoint(screen);
            transform.position = new Vector3(p.x, p.y - 0.2f + liftHeight, _home.z);
            if (_shadow != null) _shadow.transform.position = new Vector3(p.x, p.y - 0.2f, _home.z + 0.01f);
            Hovered?.Invoke(p);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!_dragging) return;
            _dragging = false;
            var drop = (Vector2)cam.ScreenToWorldPoint(e.position);
            transform.position = _home;
            transform.localScale = _scale;
            if (_shadow != null) Destroy(_shadow.gameObject);
            if (_actor != null)
            {
                _actor.Renderer.sortingOrder = _order;
                _actor.Play(ActorPose.Idle);
            }
            if (_wanderer != null) _wanderer.enabled = true;
            GameCursor.Set(CursorKind.Default);
            Dropped?.Invoke(drop);
        }

        void OnDisable()
        {
            if (_dragging) GameCursor.Set(CursorKind.Default);
        }
    }
}

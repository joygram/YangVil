using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CatVillage.World
{
    public class CatDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Camera cam;
        public Action Began;
        public Action<Vector2> Dropped;

        CatWanderer _wanderer;
        SpriteActor _actor;
        Vector3 _home;
        int _order;

        void Awake()
        {
            _wanderer = GetComponent<CatWanderer>();
            _actor = GetComponent<SpriteActor>();
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (VillageCamera.Locked) return;
            _home = transform.position;
            if (_wanderer != null) _wanderer.enabled = false;
            if (_actor != null)
            {
                _order = _actor.Renderer.sortingOrder;
                _actor.Renderer.sortingOrder = 500;
                _actor.Play(ActorPose.Walk);
            }
            Began?.Invoke();
        }

        public void OnDrag(PointerEventData e)
        {
            if (VillageCamera.Locked) return;
            var p = cam.ScreenToWorldPoint(e.position);
            transform.position = new Vector3(p.x, p.y - 0.3f, _home.z);
        }

        public void OnEndDrag(PointerEventData e)
        {
            var drop = (Vector2)cam.ScreenToWorldPoint(e.position);
            transform.position = _home;
            if (_actor != null)
            {
                _actor.Renderer.sortingOrder = _order;
                _actor.Play(ActorPose.Idle);
            }
            if (_wanderer != null) _wanderer.enabled = true;
            Dropped?.Invoke(drop);
        }
    }
}

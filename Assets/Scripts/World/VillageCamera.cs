using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CatVillage.World
{
    public class VillageCamera : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        public Camera cam;
        public Rect bounds = new Rect(-7, -12, 14, 24);
        public Vector2 zoomRange = new Vector2(5f, 11f);

        public static bool Locked;

        Vector3 _lastWorld;

        void Start() => Clamp();

        public void OnBeginDrag(PointerEventData e) => _lastWorld = cam.ScreenToWorldPoint(e.position);

        public void OnDrag(PointerEventData e)
        {
            if (Locked) return;
            var now = cam.ScreenToWorldPoint(e.position);
            cam.transform.position += _lastWorld - now;
            Clamp();
            _lastWorld = cam.ScreenToWorldPoint(e.position);
        }

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || Locked) return;
            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f) return;
            if (WorldKit.PointerOverUI(mouse.position.ReadValue())) return;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - Mathf.Sign(scroll) * 0.6f, zoomRange.x, zoomRange.y);
            Clamp();
        }

        void Clamp()
        {
            var h = cam.orthographicSize;
            var w = h * cam.aspect;
            var p = cam.transform.position;
            p.x = bounds.width / 2 > w ? Mathf.Clamp(p.x, bounds.xMin + w, bounds.xMax - w) : bounds.center.x;
            p.y = bounds.height / 2 > h ? Mathf.Clamp(p.y, bounds.yMin + h, bounds.yMax - h) : bounds.center.y;
            cam.transform.position = p;
        }
    }
}

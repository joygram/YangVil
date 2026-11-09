using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace YangVil.World
{
    public class VillageCamera : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        public Camera cam;
        public Rect bounds = new Rect(-7, -12, 14, 24);
        public Vector2 zoomRange = new Vector2(5f, 11f);
        public float wheelStep = 0.12f;

        public static bool Locked;

        Vector3 _lastWorld;
        float _pinchStart;
        float _sizeAtPinch;

        void OnEnable() => EnhancedTouchSupport.Enable();

        void Start() => Clamp();

        public void OnBeginDrag(PointerEventData e) => _lastWorld = cam.ScreenToWorldPoint(e.position);

        public void OnDrag(PointerEventData e)
        {
            if (Locked || Touch.activeTouches.Count > 1) return;
            var now = cam.ScreenToWorldPoint(e.position);
            cam.transform.position += _lastWorld - now;
            Clamp();
            _lastWorld = cam.ScreenToWorldPoint(e.position);
        }

        void Update()
        {
            if (Locked) return;
            Pinch();
            Wheel();
        }

        void Wheel()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f) return;
            var screen = mouse.position.ReadValue();
            if (WorldKit.PointerOverUI(screen)) return;
            ZoomAt(screen, cam.orthographicSize * (1f - Mathf.Sign(scroll) * wheelStep));
        }

        void Pinch()
        {
            var touches = Touch.activeTouches;
            if (touches.Count < 2)
            {
                _pinchStart = 0;
                return;
            }
            var a = touches[0].screenPosition;
            var b = touches[1].screenPosition;
            var distance = Vector2.Distance(a, b);
            if (_pinchStart <= 0)
            {
                if (WorldKit.PointerOverUI((a + b) / 2)) return;
                _pinchStart = distance;
                _sizeAtPinch = cam.orthographicSize;
                return;
            }
            ZoomAt((a + b) / 2, _sizeAtPinch * _pinchStart / Mathf.Max(1f, distance));
        }

        void ZoomAt(Vector2 screen, float size)
        {
            var before = cam.ScreenToWorldPoint(screen);
            cam.orthographicSize = Mathf.Clamp(size, zoomRange.x, zoomRange.y);
            var after = cam.ScreenToWorldPoint(screen);
            cam.transform.position += before - after;
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

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using YangVillage.Core;

// Godot YangVillage game/scripts/ui/Boot.cs 이식. 시작 씬(YangVillage.unity)의 유일한 컴포넌트.
// 테이블 점검 후 config.start_screen 으로 넘어감.
namespace YangVillage.Ui
{
    public class Boot : MonoBehaviour
    {
        // Godot viewport 540x960, stretch=canvas_items, aspect=expand.
        static readonly Vector2 Viewport = new Vector2(540, 960);

        public TMP_FontAsset font;

        void Awake()
        {
            Kit.Font = font;
            if (EventSystem.current == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Viewport;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            App.Root = (RectTransform)canvasGo.transform;
        }

        void Start()
        {
            var errors = TableCheck.Run(App.Tables);
            foreach (var error in errors) Debug.LogError(error);
            if (errors.Count > 0) return;
            App.Go(App.Db.Config("start_screen"));
        }
    }
}

using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace YangVil.EditorTools
{
    public static class VillageReviewCapture
    {
        public static bool Enabled => System.Environment.GetCommandLineArgs().Contains("-villageReview");
        public static void Save(Camera camera, string name, int width = 540, int height = 960, bool overview = false)
        {
            if (!Enabled) return;
            var canvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c => c.isRootCanvas);
            var oldMode = canvas.renderMode;
            var oldCamera = canvas.worldCamera;
            var oldPlane = canvas.planeDistance;
            var oldTarget = camera.targetTexture;
            var oldAspect = camera.aspect;
            var oldSize = camera.orthographicSize;
            var scaler = canvas.GetComponent<CanvasScaler>();
            var oldScaleMode = scaler.uiScaleMode;
            var oldScale = canvas.scaleFactor;
            var oldScalerScale = scaler.scaleFactor;
            var oldSorting = canvas.sortingOrder;
            var texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var active = RenderTexture.active;
            Texture2D image = null;
            try
            {
                texture.Create();
                camera.targetTexture = texture; camera.aspect = width / (float)height;
                var world = Object.FindAnyObjectByType<World.VillageWorld>();
                if (overview && world != null) camera.orthographicSize = world.layout.OverviewSize(camera.aspect);
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                canvas.sortingOrder = 2000;
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = width / 1080f;
                canvas.scaleFactor = scaler.scaleFactor;
                Canvas.ForceUpdateCanvases();
                foreach (var text in canvas.GetComponentsInChildren<TMPro.TMP_Text>()) text.ForceMeshUpdate();
                Canvas.ForceUpdateCanvases();
                var map = Object.FindAnyObjectByType<UI.StageSelectScreen>();
                if (map != null) map.FocusSelectedNow();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = texture });
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = texture });
                RenderTexture.active = texture;
                image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                Directory.CreateDirectory("Library/VillageReview");
                File.WriteAllBytes($"Library/VillageReview/{name}.png", image.EncodeToPNG());
                Debug.Log($"VillageReview: {name} {width}×{height}");
            }
            finally
            {
                RenderTexture.active = active; camera.targetTexture = oldTarget; camera.aspect = oldAspect; camera.orthographicSize = oldSize;
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldPlane;
                scaler.uiScaleMode = oldScaleMode; scaler.scaleFactor = oldScalerScale; canvas.scaleFactor = oldScale;
                canvas.sortingOrder = oldSorting;
                Object.DestroyImmediate(image); texture.Release(); Object.DestroyImmediate(texture);
                Canvas.ForceUpdateCanvases();
            }
        }
    }
}

using System;
using System.IO;
using YangVil.UI;
using YangVil.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace YangVil.EditorTools
{
    public static class VillageVisualBuilder
    {
        public const string ArtFolder = "Assets/Art/UI/Village";
        public const string LayoutPath = "Assets/Data/VillageLayout.asset";
        const string SceneryPath = "Assets/Prefabs/World/VillageScenery.prefab";
        const string Screens = "Assets/Prefabs/UI/Screens/";
        public static Sprite Sprite(VillageMotif motif) => AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{motif.ToString().ToLowerInvariant()}.png");

        public static VillageLayout EnsureLayout()
        {
            var layout = AssetDatabase.LoadAssetAtPath<VillageLayout>(LayoutPath);
            if (layout != null) return layout;
            layout = ScriptableObject.CreateInstance<VillageLayout>();
            AssetDatabase.CreateAsset(layout, LayoutPath);
            return layout;
        }

        [MenuItem("YangVil/Build Village Visuals")]
        public static void Build()
        {
            BakeArt();
            var layout = EnsureLayout();
            if (!File.Exists(SceneryPath))
            {
                Directory.CreateDirectory("Assets/Prefabs/World");
                AssetDatabase.Refresh();
                var root = new GameObject("VillageScenery");
                var world = root.AddComponent<VillageWorld>();
                world.layout = layout; world.font = UiPrefabBuilder.Font;
                world.BuildLandscape();
                UnityEngine.Object.DestroyImmediate(world);
                foreach (var r in root.GetComponentsInChildren<SpriteRenderer>())
                {
                    var motif = (VillageMotif)Enum.Parse(typeof(VillageMotif), r.sprite.name.Substring("village_".Length), true);
                    r.sprite = Sprite(motif);
                }
                PrefabUtility.SaveAsPrefabAsset(root, SceneryPath);
                UnityEngine.Object.DestroyImmediate(root);
            }
            else
            {
                var scenery = PrefabUtility.LoadPrefabContents(SceneryPath);
                try
                {
                    for (var i = scenery.transform.childCount - 1; i >= 0; i--)
                    {
                        var child = scenery.transform.GetChild(i);
                        if (child.name == "PathEdge" || child.name == "SandPath" || child.name == "PlazaRim" || child.name == "Plaza" || child.name == "YarnLandmark" || child.name == "UnbuiltMeadow" || child.name == "Label")
                            UnityEngine.Object.DestroyImmediate(child.gameObject);
                    }
                    PrefabUtility.SaveAsPrefabAsset(scenery, SceneryPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(scenery); }
            }
            layout.sceneryPrefab = AddressablesSetup.Prefab(AssetDatabase.LoadAssetAtPath<GameObject>(SceneryPath), AddressablesSetup.ArtGroup);
            EditorUtility.SetDirty(layout);
            Patch("VillageHud.prefab", StyleHud);
            Patch("StageSelectScreen.prefab", StyleMap);
            var scene = EditorSceneManager.OpenScene(WorldSceneBuilder.VillagePath);
            var village = UnityEngine.Object.FindAnyObjectByType<VillageWorld>();
            village.layout = layout;
            village.cam.backgroundColor = VillageIllustration.Grass;
            village.cam.orthographicSize = layout.cameraSize;
            village.cam.transform.position = new Vector3(layout.cameraStart.x, layout.cameraStart.y, -10);
            EditorSceneManager.SaveScene(scene);
            var boot = EditorSceneManager.OpenScene("Assets/Scenes/Intro.unity");
            UnityEngine.Object.FindAnyObjectByType<CanvasScaler>().matchWidthOrHeight = 0;
            EditorSceneManager.SaveScene(boot);
            UiPrefabBuilder.RegisterScreens();
            AssetDatabase.SaveAssets();
            Debug.Log("VillageVisualBuilder: 마을 배치·환경 프리팹·HUD·여정 지도 생성");
        }

        static void Patch(string name, Action<RectTransform> patch)
        {
            var path = Screens + name;
            var root = PrefabUtility.LoadPrefabContents(path);
            try { patch((RectTransform)root.transform); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        public static void BakeArt()
        {
            Directory.CreateDirectory(ArtFolder);
            foreach (VillageMotif motif in Enum.GetValues(typeof(VillageMotif)))
            {
                var path = $"{ArtFolder}/{motif.ToString().ToLowerInvariant()}.png";
                if (File.Exists(path)) continue;
                var texture = VillageIllustration.Draw(motif);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Point;
                importer.spriteBorder = motif == VillageMotif.Panel ? Vector4.one * 24 : Vector4.zero;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            AssetDatabase.Refresh();
        }

        static void Skin(Image image, Color color)
        {
            image.sprite = Sprite(VillageMotif.Panel); image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1; image.color = color;
        }

        public static void ButtonStyle(LabeledButton b, bool accent = false)
        {
            Skin(b.background, accent ? VillageIllustration.Sage : Color.white);
            b.normalSprite = Sprite(VillageMotif.Panel); b.highlightSprite = b.normalSprite;
            b.button.transition = Selectable.Transition.ColorTint;
            b.label.color = accent ? VillageIllustration.Cream : VillageIllustration.Ink;
            b.label.fontSize = 30;
            var colors = b.button.colors;
            colors.highlightedColor = new Color(1, 0.96f, 0.88f);
            colors.pressedColor = new Color(0.83f, 0.87f, 0.77f);
            colors.disabledColor = new Color(0.78f, 0.78f, 0.72f, 0.8f);
            b.button.colors = colors;
        }

        static void Icon(Transform parent, VillageMotif motif, Vector2 position, float size)
        {
            var existing = parent.Find("VillageIcon");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var image = UiFactory.Solid(parent, "VillageIcon", Color.white);
            image.sprite = Sprite(motif); image.raycastTarget = false;
            UiFactory.Place(image.rectTransform, Vector2.one * 0.5f, position, Vector2.one * size);
        }

        public static void StyleHud(RectTransform root)
        {
            BakeArt();
            var hud = root.GetComponent<VillageHud>();
            Skin(root.Find("TopBar").GetComponent<Image>(), Color.white);
            UiFactory.Place((RectTransform)root.Find("TopBar"), new Vector2(1, 1), new Vector2(-245, -70), new Vector2(430, 90));
            hud.wallet.fontSize = 28; hud.wallet.color = VillageIllustration.Ink;
            if (root.Find("VillageHeading") == null)
            {
                var heading = new UiFactory(UiPrefabBuilder.Font).Text(root, "냥빌\n<size=22>고양이들의 작은 하루</size>", 42, VillageIllustration.Ink, TextAlignmentOptions.MidlineLeft);
                heading.name = "VillageHeading";
                UiFactory.Place(heading.rectTransform, new Vector2(0, 1), new Vector2(235, -75), new Vector2(390, 120));
            }
            var guide = root.Find("Guide");
            var oldToggle = root.Find("GuideToggle");
            if (oldToggle != null) UnityEngine.Object.DestroyImmediate(oldToggle.gameObject);
            hud.guideToggle = new UiFactory(UiPrefabBuilder.Font).BlueButton(root, "GuideToggle", "안내", 25);
            ButtonStyle(hud.guideToggle);
            UiFactory.Place((RectTransform)hud.guideToggle.transform, new Vector2(0, 1), new Vector2(532, -70), new Vector2(110, 72));
            Skin(guide.GetComponent<Image>(), Color.white);
            UiFactory.Place((RectTransform)guide, new Vector2(0.5f, 1), new Vector2(0, -195), new Vector2(960, 96));
            hud.guideLabel.fontSize = 26; hud.guideLabel.color = VillageIllustration.Ink;
            ButtonStyle(hud.guideGo, true);
            UiFactory.Place((RectTransform)hud.guideGo.transform, new Vector2(1, 0.5f), new Vector2(-93, 0), new Vector2(150, 67));
            var menu = (RectTransform)root.Find("Menu");
            UiFactory.Place(menu, new Vector2(0.5f, 0), new Vector2(0, 102), new Vector2(1000, 162));
            var buttons = new[] { hud.hatchery, hud.shop, hud.breed, hud.dex, hud.adventure };
            var motifs = new[] { VillageMotif.Basket, VillageMotif.Shop, VillageMotif.Tower, VillageMotif.Book, VillageMotif.Gate };
            for (var i = 0; i < buttons.Length; i++)
            {
                ButtonStyle(buttons[i], i == 4);
                Icon(buttons[i].transform, motifs[i], new Vector2(0, 23), 88);
                UiFactory.Place(buttons[i].label.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 33), new Vector2(180, 52));
            }
            Skin(hud.panel.GetComponent<Image>(), Color.white);
            Skin(root.Find("MoveBanner").GetComponent<Image>(), Color.white);
            ButtonStyle(hud.panelClose); ButtonStyle(hud.moveCancel);
        }

        public static void StyleMap(RectTransform root)
        {
            BakeArt();
            var ui = new UiFactory(UiPrefabBuilder.Font);
            var screen = root.GetComponent<StageSelectScreen>();
            foreach (var name in new[] { "Stages", "JourneyViewport", "JourneyDetail", "JourneyHeader", "StrollCard" })
            {
                var old = root.Find(name);
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            }
            root.Find("Background").GetComponent<Image>().color = VillageIllustration.Cream;
            // Replace the old list frame while preserving the screen's bound controls.
            var viewport = UiFactory.Solid(root, "JourneyViewport", VillageIllustration.Grass);
            UiFactory.Stretch(viewport.rectTransform, 24, 400, 24, 310);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35; scroll.decelerationRate = 0.09f;
            scroll.viewport = viewport.rectTransform;
            var content = UiFactory.Node("JourneyTerrain", viewport.transform);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1); content.sizeDelta = new Vector2(0, 1900);
            content.anchoredPosition = Vector2.zero;
            content.gameObject.AddComponent<JourneyMapGraphic>().raycastTarget = false;
            screen.listContent = content; screen.mapScroll = scroll; scroll.content = content;
            viewport.transform.SetSiblingIndex(1);
            var header = UiFactory.Node("JourneyHeader", root);
            var title = ui.Text(header, "작은 발걸음, 넓은 세상", 45, VillageIllustration.Ink);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -74), new Vector2(950, 85));
            UiFactory.Stretch(header);
            screen.progress = ui.Text(header, "골목길 탐험", 25, VillageIllustration.Sage);
            UiFactory.Place(screen.progress.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(960, 48));
            screen.stroll = ui.BlueButton(root, "StrollCard", "산책 보내기", 26);
            ButtonStyle(screen.stroll);
            UiFactory.Place((RectTransform)screen.stroll.transform, new Vector2(0.5f, 1), new Vector2(0, -243), new Vector2(1000, 96));
            var detail = ui.Sliced(root, "JourneyDetail", Sprite(VillageMotif.Panel));
            detail.pixelsPerUnitMultiplier = 1;
            UiFactory.Place(detail.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 246), new Vector2(1000, 285));
            screen.detailTitle = ui.Text(detail.transform, "여정의 시작", 34, VillageIllustration.Ink, TextAlignmentOptions.MidlineLeft);
            UiFactory.Place(screen.detailTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -46), new Vector2(910, 62));
            screen.detailBody = ui.Text(detail.transform, "", 25, VillageIllustration.Ink, TextAlignmentOptions.TopLeft);
            UiFactory.Place(screen.detailBody.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -123), new Vector2(910, 95));
            screen.depart = ui.GreenButton(detail.transform, "Depart", "모험 출발  →", 30); ButtonStyle(screen.depart, true);
            UiFactory.Place((RectTransform)screen.depart.transform, new Vector2(0.5f, 0), new Vector2(0, 57), new Vector2(900, 80));
            ButtonStyle(screen.back);
            UiFactory.Place((RectTransform)screen.back.transform, new Vector2(0.5f, 0), new Vector2(0, 55), new Vector2(1000, 78));
            var warn = screen.partyWarning.GetComponent<TMP_Text>();
            warn.fontSize = 24; warn.color = VillageIllustration.Ink;
            UiFactory.Place(warn.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 420), new Vector2(900, 50));
            // The original list heading is replaced by the new header.
            foreach (Transform child in root)
                if (child.name == "Text") child.gameObject.SetActive(false);
        }
    }
}

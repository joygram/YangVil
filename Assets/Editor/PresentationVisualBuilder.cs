using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using YangVil.UI;
using YangVil.World;
using YangVil.Data;

namespace YangVil.EditorTools
{
    public static class PresentationVisualBuilder
    {
        const string ScreenFolder = "Assets/Prefabs/UI/Screens/";
        const string ComponentFolder = "Assets/Prefabs/UI/Components/";
        static UiFactory UI => new UiFactory(UiPrefabBuilder.Font);

        [MenuItem("YangVil/Build Intro And Battle Visuals")]
        public static void Build()
        {
            VillageVisualBuilder.BakeArt();
            EnemySilhouetteArt.Bake();
            Patch("IntroScreen.prefab", StyleIntro);
            Patch("BattleHud.prefab", StyleBattle);
            var scene = EditorSceneManager.OpenScene(WorldSceneBuilder.BattlePath);
            var world = UnityEngine.Object.FindAnyObjectByType<BattleWorld>();
            var defaults = new GameObject("PresentationDefaults").AddComponent<BattleWorld>();
            world.unitHeight = defaults.unitHeight;
            world.allySlots = defaults.allySlots;
            world.enemySlots = defaults.enemySlots;
            UnityEngine.Object.DestroyImmediate(defaults.gameObject);
            world.cam.orthographicSize = BattleWorld.ReferenceCameraSize;
            world.cam.backgroundColor = VillageIllustration.Hex(0xe2e9d5);
            EditorSceneManager.SaveScene(scene);
            UiPrefabBuilder.RegisterScreens();
            AssetDatabase.SaveAssets();
            Debug.Log("PresentationVisualBuilder: 인트로·전투 HUD·전투 배치 생성");
        }

        static void Patch(string name, Action<RectTransform> patch)
        {
            var path = ScreenFolder + name;
            var root = PrefabUtility.LoadPrefabContents(path);
            try { patch((RectTransform)root.transform); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static Image Art(Transform parent, string name, VillageMotif motif, Vector2 anchor, Vector2 size, Color? color = null)
        {
            var image = UiFactory.Solid(parent, name, color ?? Color.white);
            image.sprite = VillageVisualBuilder.Sprite(motif); image.raycastTarget = false;
            UiFactory.Place(image.rectTransform, anchor, Vector2.zero, size);
            return image;
        }

        public static void StyleIntro(RectTransform root)
        {
            VillageVisualBuilder.BakeArt();
            var screen = root.GetComponent<IntroScreen>();
            var ui = UI;
            var old = root.Find("IntroScenery");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach (var name in new[] { "FamilyTeen", "FamilyBaby", "IntroKicker", "IntroInvitation" })
            {
                var child = root.Find(name);
                if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
            root.Find("Background").GetComponent<Image>().color = VillageIllustration.Cream;
            var scenery = UiFactory.Stretch(UiFactory.Node("IntroScenery", root));
            scenery.SetSiblingIndex(1);
            Art(scenery, "Sun", VillageMotif.Disc, new Vector2(0.78f, 0.79f), Vector2.one * 260, VillageIllustration.Hex(0xf9e2ac));
            Art(scenery, "Cloud", VillageMotif.Disc, new Vector2(0.14f, 0.7f), new Vector2(320, 90), new Color(1, 1, 1, 0.7f));
            Art(scenery, "Cloud", VillageMotif.Disc, new Vector2(0.88f, 0.66f), new Vector2(300, 80), new Color(1, 1, 1, 0.7f));
            Art(scenery, "FarHill", VillageMotif.Disc, new Vector2(0.15f, 0.43f), new Vector2(1800, 850), VillageIllustration.Hex(0xc8d8ae));
            Art(scenery, "NearHill", VillageMotif.Disc, new Vector2(0.85f, 0.36f), new Vector2(1900, 1000), VillageIllustration.Hex(0xaac394));
            Art(scenery, "Meadow", VillageMotif.Disc, new Vector2(0.5f, 0.26f), new Vector2(1800, 1050), VillageIllustration.Grass);
            Art(scenery, "Path", VillageMotif.Disc, new Vector2(0.5f, 0.21f), new Vector2(750, 1050), VillageIllustration.Hex(0xf0dfba));
            Art(scenery, "Cottage", VillageMotif.House, new Vector2(0.25f, 0.55f), Vector2.one * 300);
            Art(scenery, "Bookshop", VillageMotif.Book, new Vector2(0.77f, 0.55f), Vector2.one * 260);
            Art(scenery, "Basket", VillageMotif.Basket, new Vector2(0.51f, 0.53f), Vector2.one * 210);
            for (var i = 0; i < 4; i++)
            {
                var side = i % 2 == 0 ? 0.035f : 0.965f;
                Art(scenery, "Forest", VillageMotif.Tree, new Vector2(side, 0.5f - i / 2 * 0.17f), Vector2.one * (350 + i * 18));
                Art(scenery, "Flowers", VillageMotif.Flowers, new Vector2(side + (i % 2 == 0 ? 0.09f : -0.09f), 0.41f - i / 2 * 0.16f), Vector2.one * 130);
            }
            Art(scenery, "Yarn", VillageMotif.Yarn, new Vector2(0.7f, 0.34f), Vector2.one * 95);
            var title = root.GetComponentsInChildren<TMP_Text>().First(t => t.text == "냥빌");
            title.fontSize = 176; title.color = VillageIllustration.Ink;
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(900, 220));
            var subtitle = root.GetComponentsInChildren<TMP_Text>().First(t => t.text.StartsWith("털실") || t.name == "IntroSubtitle");
            subtitle.name = "IntroSubtitle"; subtitle.text = "작은 발자국이 모여, 우리 마을이 돼요";
            subtitle.fontSize = 32; subtitle.color = VillageIllustration.Ink;
            UiFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 0.73f), Vector2.zero, new Vector2(900, 90));
            var kicker = ui.Text(root, "고양이들의 작은 마을", 28, VillageIllustration.Sage);
            kicker.name = "IntroKicker";
            UiFactory.Place(kicker.rectTransform, new Vector2(0.5f, 0.91f), Vector2.zero, new Vector2(800, 55));
            screen.teenMascot = UiFactory.Unit(root, 280); screen.teenMascot.name = "FamilyTeen";
            screen.babyMascot = UiFactory.Unit(root, 280); screen.babyMascot.name = "FamilyBaby";
            Family(screen.mascot, -215, CatData.AdultLevel);
            Family(screen.teenMascot, 35, CatData.TeenLevel);
            Family(screen.babyMascot, 235, 1);
            screen.mascot.transform.SetAsLastSibling();
            var invitation = ui.Text(root, "모험도, 낮잠도 함께해요", 28, VillageIllustration.Ink);
            invitation.name = "IntroInvitation";
            UiFactory.Place(invitation.rectTransform, new Vector2(0.5f, 0.27f), Vector2.zero, new Vector2(900, 70));
            VillageVisualBuilder.ButtonStyle(screen.start, true); screen.start.Text = "마을로 들어가기  →"; screen.start.label.fontSize = 42;
            UiFactory.Place((RectTransform)screen.start.transform, new Vector2(0.5f, 0.17f), Vector2.zero, new Vector2(820, 145));
            VillageVisualBuilder.ButtonStyle(screen.reset); screen.reset.Text = "진행 초기화"; screen.reset.label.fontSize = 24;
            UiFactory.Place((RectTransform)screen.reset.transform, new Vector2(0.5f, 0.075f), Vector2.zero, new Vector2(320, 68));
            screen.versionLabel.color = VillageIllustration.Sage; screen.versionLabel.fontSize = 22;
        }

        static void Family(UnitSprite unit, float x, int level)
        {
            var size = 280 * CatData.VisualScaleAt(level);
            var rect = (RectTransform)unit.transform;
            UiFactory.Place(rect, new Vector2(0.5f, 0.36f), new Vector2(x, 0), Vector2.one * size);
            rect.pivot = new Vector2(0.5f, 0);
        }

        static LabeledButton Template(bool portrait)
        {
            var path = ComponentFolder + (portrait ? "BattlePortraitButton.prefab" : "BattleItemButton.prefab");
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                var contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var label = contents.GetComponent<LabeledButton>().label;
                    label.fontSizeMax = 24; label.fontSizeMin = 20;
                    var unit = contents.GetComponentInChildren<UnitSprite>();
                    if (unit != null) UiFactory.Place((RectTransform)unit.transform, new Vector2(0.5f, 1), new Vector2(0, -74), Vector2.one * 128);
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
                return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<LabeledButton>();
            }
            var button = UI.BlueButton(null, portrait ? "BattlePortraitButton" : "BattleItemButton", "", 24);
            VillageVisualBuilder.ButtonStyle(button);
            button.label.fontSize = 24; button.label.enableAutoSizing = true; button.label.fontSizeMin = 20; button.label.fontSizeMax = 24;
            if (portrait)
            {
                var unit = UiFactory.Unit(button.transform, 128);
                UiFactory.Place((RectTransform)unit.transform, new Vector2(0.5f, 1), new Vector2(0, -74), Vector2.one * 128);
                UiFactory.Place(button.label.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 42), new Vector2(285, 80));
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(button.gameObject, path);
            UnityEngine.Object.DestroyImmediate(button.gameObject);
            return prefab.GetComponent<LabeledButton>();
        }

        public static void StyleBattle(RectTransform root)
        {
            VillageVisualBuilder.BakeArt();
            var screen = root.GetComponent<BattleHud>();
            screen.portraitPrefab = Template(true); screen.itemButtonPrefab = Template(false);
            foreach (var name in new[] { "BattleTopPanel", "BattleDock", "BattleNarration" })
            {
                var old = root.Find(name);
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            }
            var top = Art(root, "BattleTopPanel", VillageMotif.Panel, new Vector2(0.5f, 1), new Vector2(1010, 110));
            top.type = Image.Type.Sliced; top.rectTransform.anchoredPosition = new Vector2(0, -70); top.transform.SetAsFirstSibling();
            var dock = Art(root, "BattleDock", VillageMotif.Panel, new Vector2(0.5f, 0), new Vector2(1060, 400));
            dock.type = Image.Type.Sliced; dock.rectTransform.anchoredPosition = new Vector2(0, 205); dock.transform.SetAsFirstSibling();
            var narration = Art(root, "BattleNarration", VillageMotif.Panel, new Vector2(0.5f, 1), new Vector2(970, 75));
            narration.type = Image.Type.Sliced; narration.rectTransform.anchoredPosition = new Vector2(0, -265); narration.transform.SetAsFirstSibling();
            screen.title.color = VillageIllustration.Ink; screen.title.outlineWidth = 0; screen.title.fontSize = 34;
            UiFactory.Place(screen.title.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(950, 90));
            UiFactory.Place((RectTransform)root.Find("Controls"), new Vector2(0.5f, 1), new Vector2(0, -175), new Vector2(650, 78));
            foreach (var button in new[] { screen.speed, screen.autoSkill, screen.pause })
            { VillageVisualBuilder.ButtonStyle(button); button.label.fontSize = 24; }
            screen.status.color = VillageIllustration.Ink; screen.status.outlineWidth = 0; screen.status.fontSize = 26;
            UiFactory.Place(screen.status.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -265), new Vector2(920, 65));
            UiFactory.Place(screen.portraitRow, new Vector2(0.5f, 0), new Vector2(0, 278), new Vector2(1000, 225));
            UiFactory.Place(screen.itemRow, new Vector2(0.5f, 0), new Vector2(0, 100), new Vector2(1000, 100));
            var cut = screen.cutIn.GetComponent<Image>();
            cut.color = new Color(0.34f, 0.46f, 0.34f, 0.96f);
            screen.cutInLabel.color = VillageIllustration.Cream; screen.cutInLabel.fontSize = 54;
        }
    }
}

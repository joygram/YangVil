using System.IO;
using System.Linq;
using YangVil.Data;
using YangVil.UI;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace YangVil.EditorTools
{
    public static class UiPrefabBuilder
    {
        const string ComponentDir = "Assets/Prefabs/UI/Components";
        const string ScreenDir = "Assets/Prefabs/UI/Screens";
        const string FontSource = "Assets/Art/ThirdParty/Galmuri/Galmuri11.ttf";
        const string FontAssetPath = "Assets/Art/UI/Fonts/Galmuri11 SDF.asset";
        const string AtlasPath = "Assets/Art/UI/UI.spriteatlasv2";
        const string TmpEssentials = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";
        const string ConfigPath = "Assets/Resources/GameConfig.asset";

        static readonly Color Paper = new Color(0.98f, 0.94f, 0.86f);
        static readonly Color Ink = new Color(0.22f, 0.16f, 0.12f);

        [MenuItem("YangVil/Import TMP Essentials")]
        public static void ImportTmpEssentials()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
            {
                ExitIfBatch(0);
                return;
            }
            AssetDatabase.importPackageCompleted += _ =>
            {
                Debug.Log("UiPrefabBuilder: TMP Essential Resources 가져오기 완료");
                ExitIfBatch(0);
            };
            AssetDatabase.importPackageFailed += (_, error) =>
            {
                Debug.LogError($"UiPrefabBuilder: TMP 가져오기 실패 {error}");
                ExitIfBatch(1);
            };
            AssetDatabase.ImportPackage(TmpEssentials, false);
        }

        static void ExitIfBatch(int code)
        {
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        [MenuItem("YangVil/Build UI Prefabs")]
        public static void Build()
        {
            Directory.CreateDirectory(ComponentDir);
            Directory.CreateDirectory(ScreenDir);
            Directory.CreateDirectory(Path.GetDirectoryName(FontAssetPath));
            AssetDatabase.Refresh();

            var ui = new UiFactory(FontAsset());
            BuildAtlas();

            var catCard = Save(CatCard(ui), $"{ComponentDir}/CatCard.prefab").GetComponent<CatCard>();
            var rowCard = Save(RowCard(ui), $"{ComponentDir}/RowCard.prefab").GetComponent<RowCard>();
            var stageButton = Save(ListButton(ui, "StageButton", 130), $"{ComponentDir}/StageButton.prefab").GetComponent<LabeledButton>();
            var portrait = Save(ListButton(ui, "PortraitButton", 0), $"{ComponentDir}/PortraitButton.prefab").GetComponent<LabeledButton>();
            var itemButton = Save(ListButton(ui, "ItemButton", 0), $"{ComponentDir}/ItemButton.prefab").GetComponent<LabeledButton>();
            var shopSlot = Save(ShopSlot(ui), $"{ComponentDir}/ShopSlot.prefab").GetComponent<ShopSlot>();
            var tabButton = Save(ListButton(ui, "TabButton", 0), $"{ComponentDir}/TabButton.prefab").GetComponent<LabeledButton>();
            foreach (var legacy in new[] { $"{ComponentDir}/UnitSlot.prefab", $"{ComponentDir}/DamagePopup.prefab",
                         $"{ScreenDir}/VillageScreen.prefab", $"{ScreenDir}/BattleScreen.prefab" })
                AssetDatabase.DeleteAsset(legacy);

            Save(Intro(ui), $"{ScreenDir}/IntroScreen.prefab");
            Save(VillageHud(ui, catCard, rowCard, shopSlot, tabButton), $"{ScreenDir}/VillageHud.prefab");
            Save(StageSelect(ui, stageButton), $"{ScreenDir}/StageSelectScreen.prefab");
            Save(BattleHud(ui, portrait, itemButton), $"{ScreenDir}/BattleHud.prefab");
            Save(Result(ui), $"{ScreenDir}/ResultScreen.prefab");
            RegisterScreens();
        }

        public static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);

        [MenuItem("YangVil/Register Screen Prefabs")]
        public static void RegisterScreens()
        {
            AddressablesSetup.Run();
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            config.screens.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { ScreenDir }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                var screen = prefab.GetComponent<GameScreen>();
                if (screen == null) continue;
                config.screens.Add(new ScreenEntry { screenType = screen.GetType().Name, prefab = AddressablesSetup.Prefab(prefab) });
            }
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log($"UiPrefabBuilder: 화면 {config.screens.Count}개 등록");
        }

        static TMP_FontAsset FontAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing != null) return existing;
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontSource);
            var asset = TMP_FontAsset.CreateFontAsset(font, 44, 6, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            asset.name = "Galmuri11 SDF";
            AssetDatabase.CreateAsset(asset, FontAssetPath);
            asset.material.name = "Galmuri11 SDF Material";
            asset.atlasTextures[0].name = "Galmuri11 SDF Atlas";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        static void BuildAtlas()
        {
            if (File.Exists(AtlasPath)) return;
            var atlas = new SpriteAtlasAsset();
            atlas.Add(new Object[] { AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/Art/ThirdParty/KenneyPixelUI/9-Slice") });
            SpriteAtlasAsset.Save(atlas, AtlasPath);
            AssetDatabase.Refresh();
            var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(AtlasPath);
            importer.packingSettings = new SpriteAtlasPackingSettings
            {
                enableRotation = false, enableTightPacking = false, padding = 4
            };
            importer.textureSettings = new SpriteAtlasTextureSettings
            {
                filterMode = FilterMode.Point, generateMipMaps = false, sRGB = true
            };
            importer.SaveAndReimport();
        }

        static GameObject Save(RectTransform root, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
            Object.DestroyImmediate(root.gameObject);
            return prefab;
        }

        static RectTransform ScreenRoot(string name)
        {
            var rt = UiFactory.Stretch(UiFactory.Node(name, null));
            return rt;
        }

        static RectTransform CatCard(UiFactory ui)
        {
            var card = ui.Sliced(null, "CatCard", ui.Panel);
            card.rectTransform.sizeDelta = new Vector2(940, 300);
            UiFactory.Height(card, 300);
            var c = card.gameObject.AddComponent<CatCard>();
            c.portrait = UiFactory.Unit(card.transform, 170);
            UiFactory.Place((RectTransform)c.portrait.transform, new Vector2(0, 0.5f), new Vector2(115, -5), new Vector2(170, 170));
            c.info = ui.Text(card.transform, "치즈 [불꽃] Lv1 아기\nHP 120 공격 22 방어 10 속도 12\nEXP 0/50 · 모닥불 집\n기분 최고 · 배 100 · 청결 100", 24, Ink, TextAlignmentOptions.MidlineLeft);
            UiFactory.Stretch(c.info.rectTransform, 220, 10, 330, 10);
            c.feed = ui.BlueButton(card.transform, "Feed", "츄르", 24);
            UiFactory.Place((RectTransform)c.feed.transform, new Vector2(1, 0.5f), new Vector2(-235, 45), new Vector2(150, 80));
            c.wash = ui.BlueButton(card.transform, "Wash", "씻기기", 24);
            UiFactory.Place((RectTransform)c.wash.transform, new Vector2(1, 0.5f), new Vector2(-85, 45), new Vector2(150, 80));
            c.party = ui.BlueButton(card.transform, "Party", "파티", 24);
            UiFactory.Place((RectTransform)c.party.transform, new Vector2(1, 0.5f), new Vector2(-235, -45), new Vector2(150, 80));
            c.move = ui.GreenButton(card.transform, "Move", "이동", 24);
            UiFactory.Place((RectTransform)c.move.transform, new Vector2(1, 0.5f), new Vector2(-85, -45), new Vector2(150, 80));
            return card.rectTransform;
        }

        static RectTransform ShopSlot(UiFactory ui)
        {
            var slot = ui.Sliced(null, "ShopSlot", ui.Panel);
            slot.rectTransform.sizeDelta = new Vector2(290, 400);
            var s = slot.gameObject.AddComponent<ShopSlot>();
            var frame = ui.Sliced(slot.transform, "IconFrame", ui.Inlay);
            UiFactory.Place(frame.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -95), new Vector2(160, 160));
            s.icon = UiFactory.Solid(frame.transform, "Icon", Color.white);
            UiFactory.Stretch(s.icon.rectTransform, 14, 14, 14, 14);
            s.icon.raycastTarget = false;
            s.iconLetter = ui.Text(frame.transform, "?", 72, Color.white);
            UiFactory.Stretch(s.iconLetter.rectTransform);
            s.title = ui.Text(slot.transform, "이름", 28, Ink);
            UiFactory.Place(s.title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -200), new Vector2(270, 40));
            s.detail = ui.Text(slot.transform, "설명", 20, Color.gray);
            UiFactory.Place(s.detail.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -250), new Vector2(270, 56));
            s.owned = ui.Text(slot.transform, "", 20, Ink);
            UiFactory.Place(s.owned.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -290), new Vector2(270, 28));
            s.buy = ui.GreenButton(slot.transform, "Buy", "100 코인", 26);
            UiFactory.Place((RectTransform)s.buy.transform, new Vector2(0.5f, 0f), new Vector2(0, 50), new Vector2(250, 76));
            return slot.rectTransform;
        }

        static RectTransform RowCard(UiFactory ui)
        {
            var card = ui.Sliced(null, "RowCard", ui.Panel);
            card.rectTransform.sizeDelta = new Vector2(1000, 150);
            UiFactory.Height(card, 150);
            var r = card.gameObject.AddComponent<RowCard>();
            r.label = ui.Text(card.transform, "항목", 30, Ink, TextAlignmentOptions.MidlineLeft);
            UiFactory.Stretch(r.label.rectTransform, 30, 10, 250, 10);
            r.action = ui.GreenButton(card.transform, "Action", "구매", 30);
            UiFactory.Place((RectTransform)r.action.transform, new Vector2(1, 0.5f), new Vector2(-120, 0), new Vector2(200, 100));
            return card.rectTransform;
        }

        static RectTransform ListButton(UiFactory ui, string name, float height)
        {
            var b = ui.Button(null, name, name, ui.Blue, ui.BluePressed, ui.Red, 30);
            var rt = (RectTransform)b.transform;
            rt.sizeDelta = new Vector2(320, height > 0 ? height : 150);
            if (height > 0) UiFactory.Height(b, height);
            return rt;
        }

        static RectTransform Intro(UiFactory ui)
        {
            var root = ScreenRoot("IntroScreen");
            UiFactory.Background(root, Paper);
            var s = root.gameObject.AddComponent<IntroScreen>();
            var title = ui.Text(root, "냥빌", 140, Ink);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(900, 180));
            var sub = ui.Text(root, "털실 바구니에서 태어난 고양이들의 모험", 34, Color.gray);
            UiFactory.Place(sub.rectTransform, new Vector2(0.5f, 0.64f), Vector2.zero, new Vector2(900, 80));
            s.mascot = UiFactory.Unit(root, 320);
            UiFactory.Place((RectTransform)s.mascot.transform, new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(320, 320));
            s.start = ui.GreenButton(root, "Start", "시작하기", 48);
            UiFactory.Place((RectTransform)s.start.transform, new Vector2(0.5f, 0.24f), Vector2.zero, new Vector2(560, 140));
            s.reset = ui.Button(root, "Reset", "처음부터", ui.Red, ui.BluePressed, null, 28);
            UiFactory.Place((RectTransform)s.reset.transform, new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(320, 90));
            s.versionLabel = ui.Text(root, "v0.1.0", 26, Color.gray);
            UiFactory.Place(s.versionLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 50), new Vector2(900, 50));
            return root;
        }

        static RectTransform VillageHud(UiFactory ui, CatCard catCard, RowCard rowCard, ShopSlot shopSlot, LabeledButton tabButton)
        {
            var root = ScreenRoot("VillageHud");
            var s = root.gameObject.AddComponent<VillageHud>();
            s.catCardPrefab = catCard;
            s.rowCardPrefab = rowCard;
            s.shopSlotPrefab = shopSlot;
            s.tabButtonPrefab = tabButton;

            var top = ui.Sliced(root, "TopBar", ui.Panel);
            UiFactory.Place(top.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(1040, 110));
            s.wallet = ui.Text(top.transform, "생선 코인 0", 32, Ink, TextAlignmentOptions.MidlineLeft);
            UiFactory.Stretch(s.wallet.rectTransform, 30, 10, 30, 10);

            var menu = UiFactory.Node("Menu", root);
            UiFactory.Place(menu, new Vector2(0.5f, 0f), new Vector2(0, 90), new Vector2(1040, 130));
            UiFactory.Row(menu, 10);
            s.hatchery = ui.BlueButton(menu, "Hatchery", "부화소", 30);
            s.shop = ui.BlueButton(menu, "Shop", "상점", 30);
            s.breed = ui.BlueButton(menu, "Breed", "짝짓기", 30);
            s.dex = ui.BlueButton(menu, "Dex", "도감", 30);
            s.adventure = ui.GreenButton(menu, "Adventure", "모험", 34);

            var panel = ui.Sliced(root, "Panel", ui.Panel);
            UiFactory.Stretch(panel.rectTransform, 30, 240, 30, 150);
            s.panel = panel.gameObject;
            s.panelTitle = ui.Text(panel.transform, "제목", 44, Ink);
            UiFactory.Place(s.panelTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(-60, -60), new Vector2(800, 80));
            s.panelClose = ui.Button(panel.transform, "Close", "닫기", ui.Red, ui.BluePressed, null, 28);
            UiFactory.Place((RectTransform)s.panelClose.transform, new Vector2(1f, 1f), new Vector2(-100, -60), new Vector2(150, 80));
            s.panelContent = ui.ScrollList(panel.transform, "List");
            UiFactory.Stretch((RectTransform)s.panelContent.parent, 20, 20, 20, 120);

            var banner = ui.Sliced(root, "MoveBanner", ui.Panel, new Color(0.85f, 1f, 0.8f));
            UiFactory.Place(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -320), new Vector2(1040, 120));
            s.moveBanner = banner.gameObject;
            s.moveLabel = ui.Text(banner.transform, "옮길 집을 누르세요", 28, Ink, TextAlignmentOptions.MidlineLeft);
            UiFactory.Stretch(s.moveLabel.rectTransform, 30, 10, 200, 10);
            s.moveCancel = ui.Button(banner.transform, "Cancel", "취소", ui.Red, ui.BluePressed, null, 28);
            UiFactory.Place((RectTransform)s.moveCancel.transform, new Vector2(1f, 0.5f), new Vector2(-100, 0), new Vector2(160, 90));

            var toastBg = UiFactory.Solid(root, "Toast", new Color(0, 0, 0, 0.75f));
            UiFactory.Place(toastBg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860, 120));
            toastBg.raycastTarget = false;
            s.toast = toastBg.gameObject.AddComponent<Toast>();
            s.toast.label = ui.Text(toastBg.transform, "알림", 34, Color.white);
            UiFactory.Stretch(s.toast.label.rectTransform);

            var guide = ui.Sliced(root, "Guide", ui.Panel, new Color(1f, 0.95f, 0.75f));
            UiFactory.Place(guide.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -190), new Vector2(1040, 110));
            s.guideLabel = ui.Text(guide.transform, "할 일", 28, Ink, TextAlignmentOptions.MidlineLeft);
            UiFactory.Stretch(s.guideLabel.rectTransform, 26, 8, 190, 8);
            s.guideGo = ui.GreenButton(guide.transform, "GuideGo", "가기 ▶", 30);
            UiFactory.Place((RectTransform)s.guideGo.transform, new Vector2(1f, 0.5f), new Vector2(-95, 0), new Vector2(160, 84));
            guide.transform.SetSiblingIndex(1);
            VillageVisualBuilder.StyleHud(root);
            return root;
        }

        static RectTransform StageSelect(UiFactory ui, LabeledButton stageButton)
        {
            var root = ScreenRoot("StageSelectScreen");
            UiFactory.Background(root, Paper);
            var s = root.gameObject.AddComponent<StageSelectScreen>();
            s.stageButtonPrefab = stageButton;
            var title = ui.Text(root, "1지역 · 골목길", 60, Ink);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(1000, 120));
            s.listContent = ui.ScrollList(root, "Stages");
            UiFactory.Stretch((RectTransform)s.listContent.parent, 20, 300, 20, 200);
            var warn = ui.Text(root, "마을에서 파티를 먼저 편성하세요", 32, Color.red);
            UiFactory.Place(warn.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 250), new Vector2(1000, 60));
            s.partyWarning = warn.gameObject;
            s.back = ui.BlueButton(root, "Back", "마을로", 40);
            UiFactory.Place((RectTransform)s.back.transform, new Vector2(0.5f, 0f), new Vector2(0, 110), new Vector2(600, 140));
            VillageVisualBuilder.StyleMap(root);
            return root;
        }

        static RectTransform BattleHud(UiFactory ui, LabeledButton portrait, LabeledButton item)
        {
            var root = ScreenRoot("BattleHud");
            var s = root.gameObject.AddComponent<BattleHud>();
            s.portraitPrefab = portrait;
            s.itemButtonPrefab = item;

            s.title = ui.Text(root, "스테이지", 40, Color.white);
            s.title.outlineWidth = 0.2f;
            s.title.outlineColor = Color.black;
            UiFactory.Place(s.title.rectTransform, new Vector2(0.5f, 1f), new Vector2(-200, -70), new Vector2(600, 90));

            var cut = UiFactory.Solid(root, "CutIn", new Color(0.1f, 0.06f, 0.04f, 0.85f));
            UiFactory.Place(cut.rectTransform, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(1200, 220));
            cut.raycastTarget = false;
            s.cutIn = cut.gameObject;
            s.cutInLabel = ui.Text(cut.transform, "필살기!", 64, new Color(1f, 0.85f, 0.2f));
            UiFactory.Stretch(s.cutInLabel.rectTransform);

            var controls = UiFactory.Node("Controls", root);
            UiFactory.Place(controls, new Vector2(1f, 1f), new Vector2(-230, -70), new Vector2(420, 90));
            UiFactory.Row(controls, 10);
            s.speed = ui.BlueButton(controls, "Speed", "1x", 26);
            s.autoSkill = ui.BlueButton(controls, "AutoSkill", "자동필살", 22);
            s.pause = ui.BlueButton(controls, "Pause", "정지", 26);

            s.status = ui.Text(root, "", 34, Color.white);
            s.status.outlineWidth = 0.2f;
            s.status.outlineColor = Color.black;
            UiFactory.Place(s.status.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(1000, 80));

            s.portraitRow = UiFactory.Node("Portraits", root);
            UiFactory.Place(s.portraitRow, new Vector2(0.5f, 0f), new Vector2(0, 270), new Vector2(1040, 170));
            UiFactory.Row(s.portraitRow, 20);

            s.itemRow = UiFactory.Node("Items", root);
            UiFactory.Place(s.itemRow, new Vector2(0.5f, 0f), new Vector2(0, 100), new Vector2(1040, 120));
            UiFactory.Row(s.itemRow, 16);
            return root;
        }

        static RectTransform Result(UiFactory ui)
        {
            var root = ScreenRoot("ResultScreen");
            UiFactory.Background(root, Paper);
            var s = root.gameObject.AddComponent<ResultScreen>();
            var panel = ui.Sliced(root, "Panel", ui.Panel);
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(900, 860));
            s.title = ui.Text(panel.transform, "승리!", 96, Ink);
            UiFactory.Place(s.title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -120), new Vector2(800, 150));
            s.body = ui.Text(panel.transform, "보상", 44, Ink);
            UiFactory.Place(s.body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(800, 420));
            s.hint = ui.Text(panel.transform, "", 28, Color.gray);
            UiFactory.Place(s.hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 90), new Vector2(820, 120));
            s.next = ui.GreenButton(root, "Next", "다음 스테이지", 44);
            UiFactory.Place((RectTransform)s.next.transform, new Vector2(0.5f, 0f), new Vector2(0, 360), new Vector2(980, 150));
            s.retry = ui.BlueButton(root, "Retry", "다시 도전", 34);
            UiFactory.Place((RectTransform)s.retry.transform, new Vector2(0.5f, 0f), new Vector2(-330, 160), new Vector2(310, 130));
            s.stages = ui.BlueButton(root, "Stages", "모험 목록", 34);
            UiFactory.Place((RectTransform)s.stages.transform, new Vector2(0.5f, 0f), new Vector2(0, 160), new Vector2(310, 130));
            s.home = ui.BlueButton(root, "Home", "마을로", 34);
            UiFactory.Place((RectTransform)s.home.transform, new Vector2(0.5f, 0f), new Vector2(330, 160), new Vector2(310, 130));
            return root;
        }
    }
}

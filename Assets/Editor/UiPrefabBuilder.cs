using System.IO;
using System.Linq;
using CatVillage.Data;
using CatVillage.UI;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace CatVillage.EditorTools
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

        [MenuItem("CatVillage/Import TMP Essentials")]
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

        [MenuItem("CatVillage/Build UI Prefabs")]
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
            var slot = Save(UnitSlot(ui), $"{ComponentDir}/UnitSlot.prefab").GetComponent<UnitSlot>();
            var popup = Save(Popup(ui), $"{ComponentDir}/DamagePopup.prefab").GetComponent<DamagePopup>();

            var screens = new[]
            {
                Save(Intro(ui), $"{ScreenDir}/IntroScreen.prefab"),
                Save(Village(ui, catCard, rowCard), $"{ScreenDir}/VillageScreen.prefab"),
                Save(StageSelect(ui, stageButton), $"{ScreenDir}/StageSelectScreen.prefab"),
                Save(Battle(ui, slot, portrait, itemButton, popup), $"{ScreenDir}/BattleScreen.prefab"),
                Save(Result(ui), $"{ScreenDir}/ResultScreen.prefab")
            };

            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            config.screenPrefabs = screens.ToList();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log($"UiPrefabBuilder: 컴포넌트 7, 화면 {screens.Length}");
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
            card.rectTransform.sizeDelta = new Vector2(1000, 220);
            UiFactory.Height(card, 220);
            var c = card.gameObject.AddComponent<CatCard>();
            c.portrait = UiFactory.Unit(card.transform, 170);
            UiFactory.Place((RectTransform)c.portrait.transform, new Vector2(0, 0.5f), new Vector2(115, -5), new Vector2(170, 170));
            c.info = ui.Text(card.transform, "치즈 [불꽃] Lv1 아기\nHP 120  공격 22  방어 10  속도 12\nEXP 0/50", 28, Ink, TextAlignmentOptions.MidlineLeft);
            UiFactory.Stretch(c.info.rectTransform, 220, 10, 250, 10);
            c.feed = ui.BlueButton(card.transform, "Feed", "츄르 주기", 28);
            UiFactory.Place((RectTransform)c.feed.transform, new Vector2(1, 0.5f), new Vector2(-125, 50), new Vector2(220, 84));
            c.party = ui.BlueButton(card.transform, "Party", "파티 편성", 28);
            UiFactory.Place((RectTransform)c.party.transform, new Vector2(1, 0.5f), new Vector2(-125, -50), new Vector2(220, 84));
            return card.rectTransform;
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

        static RectTransform UnitSlot(UiFactory ui)
        {
            var slot = UiFactory.Node("UnitSlot", null);
            UiFactory.Place(slot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 400));
            var hit = slot.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            var s = slot.gameObject.AddComponent<UnitSlot>();
            s.button = slot.gameObject.AddComponent<Button>();
            s.button.transition = Selectable.Transition.None;
            s.sprite = UiFactory.Unit(slot, 200);
            UiFactory.Place((RectTransform)s.sprite.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(200, 200));
            s.nameLabel = ui.Text(slot, "이름", 26, Ink);
            UiFactory.Place(s.nameLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(300, 40));
            s.hp = ui.Bar(slot, "Hp", new Color(0.35f, 0.85f, 0.35f), new Vector2(0, 50), new Vector2(220, 22));
            s.gauge = ui.Bar(slot, "Gauge", new Color(1f, 0.85f, 0.2f), new Vector2(0, 20), new Vector2(220, 14));
            var marker = ui.Text(slot, "▼ 집중", 28, Color.red);
            UiFactory.Place(marker.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, 15), new Vector2(200, 40));
            s.focusMarker = marker.gameObject;
            s.popupAnchor = UiFactory.Node("PopupAnchor", slot);
            UiFactory.Place(s.popupAnchor, new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(10, 10));
            return slot;
        }

        static RectTransform Popup(UiFactory ui)
        {
            var root = UiFactory.Node("DamagePopup", null);
            UiFactory.Place(root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340, 70));
            var p = root.gameObject.AddComponent<DamagePopup>();
            p.label = ui.Text(root, "-99", 40, Color.white);
            p.label.outlineWidth = 0.25f;
            p.label.outlineColor = Color.black;
            UiFactory.Stretch(p.label.rectTransform);
            return root;
        }

        static RectTransform Intro(UiFactory ui)
        {
            var root = ScreenRoot("IntroScreen");
            UiFactory.Background(root, Paper);
            var s = root.gameObject.AddComponent<IntroScreen>();
            var title = ui.Text(root, "고양이 마을", 110, Ink);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(900, 180));
            var sub = ui.Text(root, "털실 바구니에서 태어난 고양이들의 모험", 34, Color.gray);
            UiFactory.Place(sub.rectTransform, new Vector2(0.5f, 0.64f), Vector2.zero, new Vector2(900, 80));
            s.mascot = UiFactory.Unit(root, 320);
            UiFactory.Place((RectTransform)s.mascot.transform, new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(320, 320));
            s.start = ui.GreenButton(root, "Start", "시작하기", 48);
            UiFactory.Place((RectTransform)s.start.transform, new Vector2(0.5f, 0.24f), Vector2.zero, new Vector2(560, 140));
            s.reset = ui.Button(root, "Reset", "처음부터", ui.Red, ui.BluePressed, null, 28);
            UiFactory.Place((RectTransform)s.reset.transform, new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(320, 90));
            return root;
        }

        static RectTransform Village(UiFactory ui, CatCard catCard, RowCard rowCard)
        {
            var root = ScreenRoot("VillageScreen");
            UiFactory.Background(root, Paper);
            var s = root.gameObject.AddComponent<VillageScreen>();
            s.catCardPrefab = catCard;
            s.rowCardPrefab = rowCard;

            var top = ui.Sliced(root, "TopBar", ui.Panel);
            UiFactory.Place(top.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(1040, 150));
            s.wallet = ui.Text(top.transform, "생선 코인 0", 36, Ink, TextAlignmentOptions.MidlineLeft);
            UiFactory.Stretch(s.wallet.rectTransform, 30, 10, 380, 10);
            s.collect = ui.GreenButton(top.transform, "Collect", "코인 받기", 28);
            UiFactory.Place((RectTransform)s.collect.transform, new Vector2(1f, 0.5f), new Vector2(-190, 0), new Vector2(340, 100));

            var tabs = UiFactory.Node("Tabs", root);
            UiFactory.Place(tabs, new Vector2(0.5f, 1f), new Vector2(0, -240), new Vector2(1040, 110));
            UiFactory.Row(tabs, 16);
            s.tabCats = ui.BlueButton(tabs, "TabCats", "고양이");
            s.tabBaskets = ui.BlueButton(tabs, "TabBaskets", "바구니");
            s.tabShop = ui.BlueButton(tabs, "TabShop", "상점");

            s.listContent = ui.ScrollList(root, "List");
            UiFactory.Stretch((RectTransform)s.listContent.parent, 20, 220, 20, 310);

            s.adventure = ui.GreenButton(root, "Adventure", "모험 떠나기", 48);
            UiFactory.Place((RectTransform)s.adventure.transform, new Vector2(0.5f, 0f), new Vector2(0, 110), new Vector2(1000, 160));

            var toastBg = UiFactory.Solid(root, "Toast", new Color(0, 0, 0, 0.75f));
            UiFactory.Place(toastBg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 120));
            toastBg.raycastTarget = false;
            s.toast = toastBg.gameObject.AddComponent<Toast>();
            s.toast.label = ui.Text(toastBg.transform, "알림", 36, Color.white);
            UiFactory.Stretch(s.toast.label.rectTransform);
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
            return root;
        }

        static RectTransform Battle(UiFactory ui, UnitSlot slot, LabeledButton portrait, LabeledButton item, DamagePopup popup)
        {
            var root = ScreenRoot("BattleScreen");
            UiFactory.Background(root, new Color(0.86f, 0.92f, 0.82f));
            var s = root.gameObject.AddComponent<BattleScreen>();
            s.slotPrefab = slot;
            s.portraitPrefab = portrait;
            s.itemButtonPrefab = item;
            s.popupPrefab = popup;

            s.title = ui.Text(root, "스테이지", 44, Ink);
            UiFactory.Place(s.title.rectTransform, new Vector2(0.5f, 1f), new Vector2(-200, -70), new Vector2(600, 90));

            var controls = UiFactory.Node("Controls", root);
            UiFactory.Place(controls, new Vector2(1f, 1f), new Vector2(-230, -70), new Vector2(420, 90));
            UiFactory.Row(controls, 10);
            s.speed = ui.BlueButton(controls, "Speed", "1x", 26);
            s.autoSkill = ui.BlueButton(controls, "AutoSkill", "자동필살", 22);
            s.pause = ui.BlueButton(controls, "Pause", "정지", 26);

            s.enemyRow = UiFactory.Node("Enemies", root);
            UiFactory.Place(s.enemyRow, new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(1040, 420));
            s.allyRow = UiFactory.Node("Allies", root);
            UiFactory.Place(s.allyRow, new Vector2(0.5f, 0.36f), Vector2.zero, new Vector2(1040, 420));

            s.status = ui.Text(root, "", 34, Ink);
            UiFactory.Place(s.status.rectTransform, new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(1000, 80));

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
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(900, 900));
            s.title = ui.Text(panel.transform, "승리!", 96, Ink);
            UiFactory.Place(s.title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -130), new Vector2(800, 150));
            s.body = ui.Text(panel.transform, "보상", 44, Ink);
            UiFactory.Place(s.body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(800, 500));
            s.retry = ui.BlueButton(root, "Retry", "다시 도전", 40);
            UiFactory.Place((RectTransform)s.retry.transform, new Vector2(0.5f, 0f), new Vector2(-260, 150), new Vector2(480, 140));
            s.home = ui.GreenButton(root, "Home", "마을로", 40);
            UiFactory.Place((RectTransform)s.home.transform, new Vector2(0.5f, 0f), new Vector2(260, 150), new Vector2(480, 140));
            return root;
        }
    }
}

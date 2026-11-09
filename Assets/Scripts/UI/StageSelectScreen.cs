using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YangVil.World;

namespace YangVil.UI
{
    public class StageSelectScreen : GameScreen
    {
        public RectTransform listContent;
        public LabeledButton stageButtonPrefab;
        public GameObject partyWarning;
        public LabeledButton back;
        public ScrollRect mapScroll;
        public TMP_Text detailTitle;
        public TMP_Text detailBody;
        public TMP_Text progress;
        public LabeledButton depart;
        public LabeledButton stroll;
        readonly List<Image> _rings = new List<Image>();
        readonly List<LabeledButton> _nodes = new List<LabeledButton>();
        static int _lastSelected = -1;
        int _selected;
        public int SelectedIndex => _selected;
        public int NodeCount => _nodes.Count;

        protected override void Build()
        {
            var hasParty = Game.Party.Any();
            partyWarning.SetActive(!hasParty);
            back.OnClick(() => Screens.Show<VillageHud>());
            BuildStroll();
            var count = Game.Config.stages.Count;
            listContent.sizeDelta = new Vector2(0, JourneyMapGraphic.MapHeight(count));
            var map = listContent.GetComponent<JourneyMapGraphic>();
            map.stageCount = count;
            map.unlockedCount = Enumerable.Range(0, count).Count(Game.IsUnlocked);
            map.SetVerticesDirty();
            var next = Enumerable.Range(0, count).Where(i => Game.IsUnlocked(i) && Game.Stars(Game.Config.stages[i]) == 0).DefaultIfEmpty(Mathf.Max(0, count - 1)).First();
            progress.text = $"골목길 탐험  ·  {Game.Config.stages.Count(s => Game.Stars(s) > 0)} / {count} 거점";
            BuildScenery();
            for (var i = 0; i < Game.Config.stages.Count; i++)
            {
                var stage = Game.Config.stages[i];
                var unlocked = Game.IsUnlocked(i);
                var point = JourneyMapGraphic.RoutePoint(i, count);
                var anchor = new Vector2(point.x + 0.5f, 0);
                var pos = new Vector2(0, point.y * listContent.sizeDelta.y);
                var ring = Art(listContent, "SelectedRoute", VillageMotif.Disc, pos, Vector2.one * (stage.HasBoss ? 174 : 152), anchor);
                ring.color = VillageIllustration.Cream;
                _rings.Add(ring);
                var b = Instantiate(stageButtonPrefab, listContent);
                var le = b.GetComponent<LayoutElement>();
                if (le != null) Destroy(le);
                Place((RectTransform)b.transform, anchor, pos, Vector2.one * (stage.HasBoss ? 142 : 124));
                b.background.sprite = VillageIllustration.Get(VillageMotif.Disc);
                b.background.type = Image.Type.Simple;
                b.button.transition = Selectable.Transition.ColorTint;
                b.background.color = unlocked ? (stage.HasBoss ? VillageIllustration.Peach : VillageIllustration.Sage) : VillageIllustration.Hex(0x9da98b);
                b.label.color = VillageIllustration.Cream;
                b.label.fontSize = 38;
                b.Text = unlocked ? $"{i + 1:00}" : "";
                if (!unlocked) Art(b.transform, "Locked", VillageMotif.Lock, Vector2.zero, Vector2.one * 65, Vector2.one * 0.5f);
                var index = i;
                b.OnClick(() => SelectStage(index));
                _nodes.Add(b);
                if (i == next)
                {
                    Art(listContent, "NextFootstep", VillageMotif.Paw, pos + Vector2.up * 102, Vector2.one * 55, anchor);
                }
                var name = Label(listContent, stage.displayName, 27, VillageIllustration.Ink);
                Place(name.rectTransform, anchor, pos + Vector2.down * 103, new Vector2(330, 65));
                name.textWrappingMode = TextWrappingModes.Normal;
                var stars = Label(listContent, unlocked ? Ui.Stars(Game.Stars(stage)) : "아직 닿지 않은 길", 26, unlocked ? VillageIllustration.Ink : VillageIllustration.Sage);
                Place(stars.rectTransform, anchor, pos + Vector2.down * 150, new Vector2(320, 40));
            }
            _selected = Mathf.Clamp(_lastSelected >= 0 ? _lastSelected : next, 0, Mathf.Max(0, count - 1));
            if (count > 0) SelectStage(_selected);
            else { detailTitle.text = "모험 준비 중"; detailBody.text = "마을에서 잠시 쉬어 가요."; depart.Interactable = false; }
            StartCoroutine(FocusSelected());
        }

        public void SelectStage(int index)
        {
            _selected = _lastSelected = index;
            var stage = Game.Config.stages[index];
            var unlocked = Game.IsUnlocked(index);
            for (var i = 0; i < _rings.Count; i++) _rings[i].enabled = i == index;
            detailTitle.text = stage.displayName + (stage.HasBoss ? " · 보스 거점" : "");
            detailBody.text = unlocked
                ? $"{stage.waves.Count}웨이브 · 적 Lv{stage.enemyLevel}   {Ui.Stars(Game.Stars(stage))}\n생선 코인 {stage.rewardCoins} · 츄르 {stage.rewardTreats}" + (stage.firstClearBasket != null && Game.Stars(stage) == 0 ? " · 첫 승리 바구니" : "")
                : "앞선 거점을 클리어하면 이 길이 열려요.";
            depart.Text = !unlocked ? "잠긴 길" : !Game.Party.Any() ? "파티를 편성해 주세요" : "모험 출발  →";
            depart.Interactable = unlocked && Game.Party.Any();
            depart.OnClick(() => { if (Game.IsUnlocked(index) && Game.Party.Any()) Core.SceneFlow.Instance.GoBattle(stage); });
        }

        System.Collections.IEnumerator FocusSelected()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            FocusSelectedNow();
        }

        public void FocusSelectedNow()
        {
            mapScroll.Rebuild(CanvasUpdate.PostLayout);
            var height = listContent.rect.height;
            var viewport = mapScroll.viewport.rect.height;
            var point = JourneyMapGraphic.RoutePoint(_selected, Game.Config.stages.Count);
            mapScroll.verticalNormalizedPosition = Mathf.Clamp01((point.y * height - viewport * 0.48f) / Mathf.Max(1, height - viewport));
        }

        void BuildScenery()
        {
            var h = listContent.sizeDelta.y;
            Art(listContent, "VillageDeparture", VillageMotif.House, new Vector2(0, 120), Vector2.one * 170, new Vector2(0.5f, 0));
            var start = Label(listContent, "냥빌에서 출발", 24, VillageIllustration.Ink);
            Place(start.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 38), new Vector2(350, 45));
            for (var i = 0; i < 30; i++)
            {
                var side = i % 2 == 0 ? 0.08f : 0.92f;
                var y = 180 + i / 2 * (h - 300) / 15;
                Art(listContent, "Forest", i % 4 == 0 ? VillageMotif.Flowers : VillageMotif.Tree,
                    new Vector2(Mathf.Sin(i * 2.1f) * 22, y), Vector2.one * (140 + i % 3 * 25), new Vector2(side, 0));
            }
            Art(listContent, "CreekPond", VillageMotif.Pond, new Vector2(0, h * 0.35f), new Vector2(220, 155), new Vector2(0.72f, 0));
            Art(listContent, "BossLandmark", VillageMotif.Gate, new Vector2(0, h - 110), Vector2.one * 230, new Vector2(0.5f, 0));
            var title = Label(listContent, "숲 너머의 발자국", 30, VillageIllustration.Ink);
            Place(title.rectTransform, new Vector2(0.5f, 0), new Vector2(0, h - 26), new Vector2(500, 50));
        }

        Image Art(Transform parent, string name, VillageMotif motif, Vector2 pos, Vector2 size, Vector2 anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = VillageIllustration.Get(motif); image.raycastTarget = false;
            Place(image.rectTransform, anchor, pos, size);
            return image;
        }
        TMP_Text Label(Transform parent, string text, float size, Color color)
        {
            var go = new GameObject("RouteLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<TextMeshProUGUI>();
            label.font = stageButtonPrefab.label.font; label.text = text; label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center; label.color = color; label.raycastTarget = false;
            return label;
        }
        static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        { rt.anchorMin = rt.anchorMax = anchor; rt.pivot = Vector2.one * 0.5f; rt.anchoredPosition = pos; rt.sizeDelta = size; }

        static string _lastStroll;

        void BuildStroll()
        {
            var b = stroll;
            var stage = Game.BestClearedStage();
            if (Game.OnExpedition)
            {
                b.Text = $"산책 중 {Ui.Clock(Game.ExpeditionSeconds)} · 전투 {Game.ExpeditionSeconds / Core.CatLife.ExpeditionRunSeconds}회 → 데려오기";
                b.OnClick(() =>
                {
                    var r = Game.ReturnExpedition();
                    _lastStroll = $"\"{r.Bark}\" 코인+{r.Coins} 츄르+{r.Treats} EXP+{r.Exp}";
                    Rebuild();
                });
                return;
            }
            b.Text = _lastStroll ?? (stage == null
                ? "산책: 스테이지 하나를 깨면 열려요"
                : $"파티 산책 보내기 ({stage.displayName} 반복, 최대 {Core.CatLife.ExpeditionCapSeconds / 3600}시간)");
            b.Interactable = stage != null && Game.Party.Any();
            b.OnClick(() =>
            {
                _lastStroll = null;
                Game.StartExpedition();
                Rebuild();
            });
        }

        void Rebuild()
        {
            foreach (Transform child in listContent) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            _rings.Clear(); _nodes.Clear();
            Build();
        }
    }
}

using System.Linq;
using UnityEngine;

namespace YangVil.UI
{
    public class StageSelectScreen : GameScreen
    {
        public RectTransform listContent;
        public LabeledButton stageButtonPrefab;
        public GameObject partyWarning;
        public LabeledButton back;

        protected override void Build()
        {
            var hasParty = Game.Party.Any();
            partyWarning.SetActive(!hasParty);
            back.OnClick(() => Screens.Show<VillageHud>());
            BuildStroll();

            for (var i = 0; i < Game.Config.stages.Count; i++)
            {
                var stage = Game.Config.stages[i];
                var unlocked = Game.IsUnlocked(i);
                var b = Instantiate(stageButtonPrefab, listContent);
                b.Text = unlocked ? $"{stage.displayName}  {Ui.Stars(Game.Stars(stage))}  ({stage.waves.Count}웨이브)" : $"{stage.displayName}  (잠김)";
                b.Highlight(stage.HasBoss);
                b.Interactable = unlocked && hasParty;
                var s = stage;
                b.OnClick(() => Core.SceneFlow.Instance.GoBattle(s));
            }
        }

        static string _lastStroll;

        void BuildStroll()
        {
            var b = Instantiate(stageButtonPrefab, listContent);
            var stage = Game.BestClearedStage();
            if (Game.OnExpedition)
            {
                b.Text = $"🐾 산책 중 {Ui.Clock(Game.ExpeditionSeconds)} · 전투 {Game.ExpeditionSeconds / Core.CatLife.ExpeditionRunSeconds}회 → 데려오기";
                b.OnClick(() =>
                {
                    var r = Game.ReturnExpedition();
                    _lastStroll = $"🐾 \"{r.Bark}\" 코인+{r.Coins} 츄르+{r.Treats} EXP+{r.Exp}";
                    Rebuild();
                });
                return;
            }
            b.Text = _lastStroll ?? (stage == null
                ? "🐾 산책: 스테이지 하나를 깨면 열려요"
                : $"🐾 파티 산책 보내기 ({stage.displayName} 반복, 최대 {Core.CatLife.ExpeditionCapSeconds / 3600}시간)");
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
            foreach (Transform child in listContent) Destroy(child.gameObject);
            Build();
        }
    }
}

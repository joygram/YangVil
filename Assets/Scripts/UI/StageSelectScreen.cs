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
    }
}

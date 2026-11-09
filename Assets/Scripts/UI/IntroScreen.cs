namespace YangVil.UI
{
    public class IntroScreen : GameScreen
    {
        public UnitSprite mascot;
        public UnitSprite teenMascot;
        public UnitSprite babyMascot;
        public LabeledButton start;
        public LabeledButton reset;
        public TMPro.TMP_Text versionLabel;

        protected override void Build()
        {
            versionLabel.text = Core.BuildInfo.Label;
            var starter = Game.Config.starterCat;
            var genes = Core.Genetics.FromSpecies(starter, new System.Random(7));
            BindUnit(mascot, starter.sprites, UnityEngine.Color.white, genes);
            BindUnit(teenMascot, starter.sprites, UnityEngine.Color.white, genes);
            BindUnit(babyMascot, starter.sprites, UnityEngine.Color.white, genes);
            start.OnClick(() => Core.SceneFlow.Instance.GoVillage());
            reset.OnClick(() => Game.ResetSave());
        }
    }
}

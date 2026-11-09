namespace YangVil.UI
{
    public class IntroScreen : GameScreen
    {
        public UnitSprite mascot;
        public LabeledButton start;
        public LabeledButton reset;
        public TMPro.TMP_Text versionLabel;

        protected override void Build()
        {
            versionLabel.text = Core.BuildInfo.Label;
            var starter = Game.Config.starterCat;
            BindUnit(mascot, starter.sprites, UnityEngine.Color.white, Core.Genetics.FromSpecies(starter, new System.Random(7)));
            start.OnClick(() => Core.SceneFlow.Instance.GoVillage());
            reset.OnClick(() => Game.ResetSave());
        }
    }
}

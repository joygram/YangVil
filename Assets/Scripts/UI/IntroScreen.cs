namespace CatVillage.UI
{
    public class IntroScreen : GameScreen
    {
        public UnitSprite mascot;
        public LabeledButton start;
        public LabeledButton reset;

        protected override void Build()
        {
            var starter = Game.Config.starterCat;
            BindUnit(mascot, starter.sprites, starter.tint);
            start.OnClick(() => Core.SceneFlow.Instance.GoVillage());
            reset.OnClick(() => Game.ResetSave());
        }
    }
}

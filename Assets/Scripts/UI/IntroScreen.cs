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
            mascot.Bind(starter.sprites, starter.tint);
            start.OnClick(() => Screens.Show<VillageScreen>());
            reset.OnClick(() => Game.ResetSave());
        }
    }
}

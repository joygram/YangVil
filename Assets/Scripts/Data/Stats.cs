using System;

namespace CatVillage.Data
{
    [Serializable]
    public struct Stats
    {
        public int hp;
        public int atk;
        public int def;
        public int spd;

        public Stats(int hp, int atk, int def, int spd)
        {
            this.hp = hp;
            this.atk = atk;
            this.def = def;
            this.spd = spd;
        }

        public Stats Scaled(float m) => new Stats(
            (int)(hp * m), (int)(atk * m), (int)(def * m), (int)(spd * m));

        public static Stats operator +(Stats a, Stats b) =>
            new Stats(a.hp + b.hp, a.atk + b.atk, a.def + b.def, a.spd + b.spd);

        public static Stats operator *(Stats a, int n) =>
            new Stats(a.hp * n, a.atk * n, a.def * n, a.spd * n);
    }
}

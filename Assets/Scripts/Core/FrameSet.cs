using UnityEngine;

namespace YangVil.Core
{
    public class FrameSet
    {
        public Sprite[] Idle = new Sprite[0];
        public Sprite[] Walk = new Sprite[0];
        public Sprite[] Attack = new Sprite[0];
        public Sprite[] Hurt = new Sprite[0];
        public float Fps = 8f;

        public Sprite[] WalkOrIdle => Walk.Length > 0 ? Walk : Idle;
        public Sprite[] AttackOrIdle => Attack.Length > 0 ? Attack : Idle;
        public Sprite[] HurtOrIdle => Hurt.Length > 0 ? Hurt : Idle;
    }
}

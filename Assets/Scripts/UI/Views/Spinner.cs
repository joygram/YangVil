using UnityEngine;

namespace CatVillage.UI
{
    public class Spinner : MonoBehaviour
    {
        public float degreesPerSecond = 270f;

        void Update() => transform.Rotate(0, 0, -degreesPerSecond * Time.unscaledDeltaTime);
    }
}

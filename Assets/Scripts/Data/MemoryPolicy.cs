using System;

namespace CatVillage.Data
{
    [Serializable]
    public class MemoryPolicy
    {
        public bool unloadUnusedOnScreenChange = true;
        public int unloadEveryNScreens = 1;
        public bool collectGarbageOnUnload = true;
        public bool releaseOnLowMemory = true;
    }
}

using System;

namespace YangVil.Core
{
    public enum CatMood { Happy, Normal, Hungry, Dirty, Weak }

    public static class CatCare
    {
        public const float FullnessPerHour = 12f;
        public const float CleanlinessPerHour = 8f;
        public const float LowThreshold = 30f;
        public const float HighThreshold = 70f;
        public const float FeedAmount = 40f;
        public const float LowProductionRate = 0.5f;

        public static void Decay(OwnedCat cat, long now)
        {
            if (cat.careUnix <= 0) cat.careUnix = now;
            var hours = Math.Max(0, now - cat.careUnix) / 3600f;
            if (hours <= 0) return;
            cat.fullness = Math.Max(0f, cat.fullness - hours * FullnessPerHour);
            cat.cleanliness = Math.Max(0f, cat.cleanliness - hours * CleanlinessPerHour);
            cat.careUnix = now;
        }

        public static CatMood Mood(OwnedCat cat)
        {
            var hungry = cat.fullness < LowThreshold;
            var dirty = cat.cleanliness < LowThreshold;
            if (hungry && dirty) return CatMood.Weak;
            if (hungry) return CatMood.Hungry;
            if (dirty) return CatMood.Dirty;
            if (cat.fullness >= HighThreshold && cat.cleanliness >= HighThreshold) return CatMood.Happy;
            return CatMood.Normal;
        }

        public static float ProductionRate(OwnedCat cat) => Mood(cat) switch
        {
            CatMood.Hungry or CatMood.Dirty or CatMood.Weak => LowProductionRate,
            _ => 1f
        };

        public static string Label(CatMood mood) => mood switch
        {
            CatMood.Happy => "기분 최고",
            CatMood.Hungry => "배고파요",
            CatMood.Dirty => "꼬질꼬질",
            CatMood.Weak => "비실비실",
            _ => "보통"
        };
    }
}

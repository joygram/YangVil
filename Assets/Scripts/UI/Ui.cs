using System;
using YangVil.Data;
using UnityEngine;

namespace YangVil.UI
{
    public static class Ui
    {
        public static GameConfig Config;

        public static readonly Color Ink = new Color(0.22f, 0.16f, 0.12f);
        public static readonly Color Paper = new Color(0.98f, 0.94f, 0.86f);
        public static readonly Color Good = new Color(1f, 0.85f, 0.2f);
        public static readonly Color Muted = new Color(0.55f, 0.55f, 0.55f);

        public static void Clear(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
        }

        public static string Stars(int n) => new string('★', n) + new string('☆', 3 - n);

        public static string Clock(long seconds)
        {
            if (seconds <= 0) return "부화 가능!";
            var t = TimeSpan.FromSeconds(seconds);
            return t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes:00}:{t.Seconds:00}";
        }
    }
}

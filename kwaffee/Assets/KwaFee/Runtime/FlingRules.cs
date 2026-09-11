using UnityEngine;

namespace KwaFee {
    public static class FlingRules {
        public const float MinSpeed = 5f;
        public const float MaxSpeed = 12f;
        public const float ChargeDuration = 1.1f;
        public static float SpeedForCharge(float seconds) => Mathf.Lerp(MinSpeed, MaxSpeed, Mathf.Clamp01(seconds / ChargeDuration));
        public static float Charge01(float seconds) => Mathf.Clamp01(seconds / ChargeDuration);
        public static int TipsForServe(float liquid) => Mathf.RoundToInt(Mathf.Lerp(4f, 12f, Mathf.Clamp01(liquid)));
    }
}

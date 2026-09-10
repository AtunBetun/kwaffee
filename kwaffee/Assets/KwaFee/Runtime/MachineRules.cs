using UnityEngine;

namespace KwaFee {
    public static class MachineRules {
        public const float BrokenThreshold = 0.05f;
        public const float ProductionInterval = 4f;
        public const float PassiveDegradePerSecond = 0.004f;
        // Each produced cup wears a machine; with 4s production this reaches
        // breakage (~0.05) after ~7 cups per machine under load.
        public const float UseDegradePerCup = 0.14f;
        public static bool IsBroken(float health) => health <= BrokenThreshold;
        public static float Degrade(float health, float dt) => Mathf.Max(0f, health - PassiveDegradePerSecond * dt);
        public static float ProductionDelay(float health) => Mathf.Lerp(ProductionInterval * 1.6f, ProductionInterval, Mathf.Clamp01(health));
    }
}
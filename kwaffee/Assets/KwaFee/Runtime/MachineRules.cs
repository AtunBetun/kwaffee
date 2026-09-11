using UnityEngine;

namespace KwaFee {
    public static class MachineRules {
        public const float BrokenThreshold = 0.05f;
        public const float ProductionInterval = 4f;
        public const float PassiveDegradePerSecond = 0.004f;
        // Each produced cup wears a machine; 0.02/cup with 45 cups in a 3-min shift
        // (0.9 wear) plus passive 0.72 = 1.62 — under full 4-machine load a
        // machine breaks late-shift; light traffic survives. Breakage is a
        // sustained-load consequence, not a first-minute cascade.
        public const float UseDegradePerCup = 0.02f;
        public static bool IsBroken(float health) => health <= BrokenThreshold;
        public static float Degrade(float health, float dt) => Mathf.Max(0f, health - PassiveDegradePerSecond * dt);
        public static float ProductionDelay(float health) => Mathf.Lerp(ProductionInterval * 1.6f, ProductionInterval, Mathf.Clamp01(health));
    }
}
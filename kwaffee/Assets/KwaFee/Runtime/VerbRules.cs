using UnityEngine;

namespace KwaFee {
    public enum VerbKind { Fling, Chug, Fix, Steal, Sabotage }
    public enum MachineKind { Espresso, Grinder, SteamWand, IceMachine }

    public static class VerbRules {
        public const float ChugDuration = 3.5f;
        // Overdose at 2.5s: a full cup holds 1.0 liquid, drained at 0.35/s, so
        // ~2.57s of chugging empties it. 2.5s keeps greedy single-cup chugging
        // a real overdose outcome; 3s was unreachable from one cup.
        public const float OverdoseSeconds = 2.5f;
        public const float MaxChugMultiplier = 1.65f;
        public const float RepairAmount = .5f;
        public const float SabotageAmount = .35f;
        public const float MinChugLiquid = 0.1f;
        public const float ChugDrinkRate = 0.35f;
        public static float ChugMultiplier(float seconds) => Mathf.Lerp(1f, MaxChugMultiplier, Mathf.Clamp01(seconds / ChugDuration));
        public static bool IsOverdose(float seconds) => seconds >= OverdoseSeconds;
        public static bool CanSteal(bool targetHolding, bool actorHolding, float distance) => targetHolding && !actorHolding && distance <= 1.6f;
        public static float Repair(float health) => Mathf.Clamp01(health + RepairAmount);
        public static float Sabotage(float health) => Mathf.Clamp01(health - SabotageAmount);
        public static bool CanChug(bool holdingCup, float liquid) => holdingCup && liquid >= MinChugLiquid;
        public static float ChugConsume(float liquid, float dt) => Mathf.Max(0f, liquid - dt * ChugDrinkRate);
    }
}
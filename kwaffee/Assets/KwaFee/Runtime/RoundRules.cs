namespace KwaFee {
    public static class RoundRules {
        public const float ShiftLength = 180f;
        public const float OrderSpawnInterval = 12f;
        public const int MaxActiveOrders = 3;
        public const int QuotaTarget = 8;
        public const float CustomerPatience = 40f;
        public const float ShiftEndGrace = 3f;

        public static bool ShouldSpawnOrder(float elapsed, float lastSpawnTime, int activeOrders)
            => activeOrders < MaxActiveOrders && (elapsed - lastSpawnTime) >= OrderSpawnInterval;

        public static bool OrderExpired(float patienceLeft) => patienceLeft <= 0f;

        public static bool QuotaMet(int served) => served >= QuotaTarget;

        public static bool IsWithinShift(float elapsed) => elapsed < ShiftLength + ShiftEndGrace;

        public static float PatienceAfter(float patienceLeft, float dt) => patienceLeft - dt;
    }
}

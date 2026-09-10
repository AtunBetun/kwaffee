namespace KwaFee {
    public sealed class TipLedger {
        private readonly int[] balances;

        public TipLedger(int playerCount) {
            balances = new int[playerCount];
        }

        public int PlayerCount => balances.Length;

        public void AddServe(int ownerId, int amount) {
            if (ownerId >= 0 && ownerId < balances.Length) {
                balances[ownerId] += amount;
            }
        }

        public int Balance(int playerId) {
            if (playerId < 0 || playerId >= balances.Length) {
                return 0;
            }
            return balances[playerId];
        }

        public int Total {
            get {
                int total = 0;
                foreach (int balance in balances) {
                    total += balance;
                }
                return total;
            }
        }
    }
}
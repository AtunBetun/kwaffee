using NUnit.Framework;

namespace KwaFee.Tests {
    public sealed class VerbAndMachineRulesTests {
        [Test]
        public void CanChugRequiresHoldingCupAndEnoughLiquid() {
            Assert.That(VerbRules.CanChug(false, 1f), Is.False);
            Assert.That(VerbRules.CanChug(true, 0.05f), Is.False);
            Assert.That(VerbRules.CanChug(true, 1f), Is.True);
        }

        [Test]
        public void ChugConsumeDrainsAtDrinkRate() {
            Assert.That(VerbRules.ChugConsume(1f, 1f), Is.EqualTo(0.65f).Within(0.001f));
            Assert.That(VerbRules.ChugConsume(0.9f, 2f), Is.EqualTo(0.9f - 0.7f).Within(0.001f));
        }

        [Test]
        public void ChugConsumeClampsAtZeroAndNeverNegative() {
            Assert.That(VerbRules.ChugConsume(0.2f, 1f), Is.EqualTo(0f));
            Assert.That(VerbRules.ChugConsume(0.1f, 5f), Is.EqualTo(0f));
            Assert.That(VerbRules.ChugConsume(0f, 10f), Is.EqualTo(0f));
        }

        [Test]
        public void IsBrokenOnlyAtOrBelowBrokenThreshold() {
            Assert.That(MachineRules.IsBroken(MachineRules.BrokenThreshold), Is.True);
            Assert.That(MachineRules.IsBroken(MachineRules.BrokenThreshold + 0.001f), Is.False);
            Assert.That(MachineRules.IsBroken(1f), Is.False);
        }

        [Test]
        public void DegradeDrainsHealthAndClampsAtZero() {
            Assert.That(MachineRules.Degrade(1f, 1f), Is.EqualTo(0.996f).Within(0.001f));
            Assert.That(MachineRules.Degrade(0.002f, 1f), Is.EqualTo(0f));
            Assert.That(MachineRules.Degrade(1f, 0f), Is.EqualTo(1f));
        }

        [Test]
        public void ProductionDelayScalesWithHealthAndIsBounded() {
            Assert.That(MachineRules.ProductionDelay(1f), Is.EqualTo(MachineRules.ProductionInterval));
            Assert.That(MachineRules.ProductionDelay(0f), Is.EqualTo(MachineRules.ProductionInterval * 1.6f).Within(0.001f));
            float mid = MachineRules.ProductionDelay(0.5f);
            Assert.That(mid, Is.GreaterThan(MachineRules.ProductionInterval).And.LessThan(MachineRules.ProductionInterval * 1.6f));
            Assert.That(MachineRules.ProductionDelay(2f), Is.EqualTo(MachineRules.ProductionInterval));
        }

        [Test]
        public void TipLedgerCreditsOwnerOnly() {
            TipLedger ledger = new TipLedger(3);
            ledger.AddServe(1, 5);
            Assert.That(ledger.Balance(1), Is.EqualTo(5));
            Assert.That(ledger.Balance(0), Is.EqualTo(0));
            Assert.That(ledger.Balance(2), Is.EqualTo(0));
        }

        [Test]
        public void TipLedgerIgnoresOutOfRangeOwners() {
            TipLedger ledger = new TipLedger(3);
            ledger.AddServe(-1, 5);
            ledger.AddServe(3, 7);
            Assert.That(ledger.Total, Is.EqualTo(0));
            Assert.That(ledger.PlayerCount, Is.EqualTo(3));
        }

        [Test]
        public void TipLedgerTotalsAndBalancesAreIndependent() {
            TipLedger ledger = new TipLedger(2);
            ledger.AddServe(0, 4);
            ledger.AddServe(1, 6);
            ledger.AddServe(0, 2);
            Assert.That(ledger.Balance(0), Is.EqualTo(6));
            Assert.That(ledger.Balance(1), Is.EqualTo(6));
            Assert.That(ledger.Total, Is.EqualTo(12));
            Assert.That(ledger.Balance(5), Is.EqualTo(0));
        }
    }
}
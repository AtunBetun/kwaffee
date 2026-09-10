using NUnit.Framework;

namespace KwaFee.Tests {
    public sealed class RoundRulesTests {
        [Test]
        public void ShouldSpawnOrderRequiresUnderCapAndElapsedInterval() {
            Assert.That(RoundRules.ShouldSpawnOrder(0f, 0f, 0), Is.False);                 // too soon
            Assert.That(RoundRules.ShouldSpawnOrder(11.9f, 0f, 0), Is.False);              // just under interval
            Assert.That(RoundRules.ShouldSpawnOrder(12f, 0f, 0), Is.True);                 // exactly interval
            Assert.That(RoundRules.ShouldSpawnOrder(50f, 30f, 0), Is.True);                // 20s elapsed
        }

        [Test]
        public void ShouldSpawnOrderFalseAtMaxActiveOrdersRegardlessOfTime() {
            Assert.That(RoundRules.ShouldSpawnOrder(12f, 0f, 3), Is.False);                // enough time, at cap
            Assert.That(RoundRules.ShouldSpawnOrder(100f, 0f, 3), Is.False);
        }

        [Test]
        public void OrderExpiredOnlyAtOrBelowZeroPatience() {
            Assert.That(RoundRules.OrderExpired(0f), Is.True);
            Assert.That(RoundRules.OrderExpired(-1f), Is.True);
            Assert.That(RoundRules.OrderExpired(0.001f), Is.False);
            Assert.That(RoundRules.OrderExpired(40f), Is.False);
        }

        [Test]
        public void QuotaMetOnlyAtOrAboveTarget() {
            Assert.That(RoundRules.QuotaMet(8), Is.True);
            Assert.That(RoundRules.QuotaMet(20), Is.True);
            Assert.That(RoundRules.QuotaMet(7), Is.False);
            Assert.That(RoundRules.QuotaMet(0), Is.False);
        }

        [Test]
        public void IsWithinShiftTrueBeforeClockPlusGrace() {
            Assert.That(RoundRules.IsWithinShift(0f), Is.True);
            Assert.That(RoundRules.IsWithinShift(179f), Is.True);
            Assert.That(RoundRules.IsWithinShift(182.9f), Is.True);                        // inside grace
            Assert.That(RoundRules.IsWithinShift(183f), Is.False);                         // exactly bound
            Assert.That(RoundRules.IsWithinShift(200f), Is.False);
        }

        [Test]
        public void PatienceAfterReducesByDtAndCanGoNegative() {
            Assert.That(RoundRules.PatienceAfter(40f, 10f), Is.EqualTo(30f).Within(0.001f));
            Assert.That(RoundRules.PatienceAfter(5f, 10f), Is.EqualTo(-5f).Within(0.001f)); // negative ok
            Assert.That(RoundRules.PatienceAfter(12f, 0f), Is.EqualTo(12f));                // zero dt unchanged
        }
    }
}

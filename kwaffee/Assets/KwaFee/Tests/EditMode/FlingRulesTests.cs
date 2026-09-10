using NUnit.Framework;

namespace KwaFee.Tests {
    public sealed class FlingRulesTests {
        [Test]
        public void ChargeClampsAtBothEnds() {
            Assert.That(FlingRules.Charge01(-1f), Is.EqualTo(0f));
            Assert.That(FlingRules.Charge01(FlingRules.ChargeDuration), Is.EqualTo(1f));
            Assert.That(FlingRules.Charge01(FlingRules.ChargeDuration * 3f), Is.EqualTo(1f));
        }

        [Test]
        public void SpeedIsMonotonicAndBounded() {
            float low = FlingRules.SpeedForCharge(0f);
            float mid = FlingRules.SpeedForCharge(FlingRules.ChargeDuration * .5f);
            float high = FlingRules.SpeedForCharge(FlingRules.ChargeDuration);
            Assert.That(low, Is.EqualTo(FlingRules.MinSpeed));
            Assert.That(high, Is.EqualTo(FlingRules.MaxSpeed));
            Assert.That(mid, Is.GreaterThan(low).And.LessThan(high));
        }

        [Test]
        public void TipsUseTheWholeLiquidRange() {
            Assert.That(FlingRules.TipsForServe(-1f), Is.EqualTo(4));
            Assert.That(FlingRules.TipsForServe(0.5f), Is.EqualTo(8));
            Assert.That(FlingRules.TipsForServe(2f), Is.EqualTo(12));
        }

        [Test]
        public void ChugBoostAndOverdoseHaveReadableLimits() {
            Assert.That(VerbRules.ChugMultiplier(0f), Is.EqualTo(1f));
            Assert.That(VerbRules.ChugMultiplier(VerbRules.ChugDuration), Is.GreaterThan(1f));
            Assert.That(VerbRules.ChugMultiplier(VerbRules.ChugDuration * 3f), Is.EqualTo(VerbRules.MaxChugMultiplier));
            Assert.That(VerbRules.IsOverdose(VerbRules.OverdoseSeconds), Is.True);
        }

        [Test]
        public void StealRequiresAnUnheldActorAndNearbyHeldTarget() {
            Assert.That(VerbRules.CanSteal(false, true, 1f), Is.False);
            Assert.That(VerbRules.CanSteal(true, false, 1f), Is.True);
            Assert.That(VerbRules.CanSteal(true, false, 3f), Is.False);
        }

        [Test]
        public void SabotageAndRepairStayWithinMachineBounds() {
            Assert.That(VerbRules.Repair(0.2f), Is.EqualTo(0.7f).Within(0.001f));
            Assert.That(VerbRules.Sabotage(0.2f), Is.EqualTo(0f).Within(0.001f));
            Assert.That(VerbRules.Sabotage(0.9f), Is.EqualTo(0.55f).Within(0.001f));
        }
    }
}

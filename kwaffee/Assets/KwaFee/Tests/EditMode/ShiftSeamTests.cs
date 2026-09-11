using UnityEngine;
using NUnit.Framework;

namespace KwaFee.Tests {
    /// Plain-data ShiftWorld: acts like the real scene (positions, velocities,
    /// cup states) without MonoBehaviours or prefabs, so the seam tests run
    /// headless and fast. The test drives the same loop the sim adapter owns:
    /// Command -> Step(dt) -> Integrate(dt) (physics).
    sealed class FakeWorld : ShiftWorld {
        public readonly Vector3[] actors = new Vector3[Shift.PlayerCount];
        public readonly Vector3[] actorVel = new Vector3[Shift.PlayerCount];
        public readonly Vector3[] actorAim = new Vector3[Shift.PlayerCount];
        public readonly Vector3[] cups = new Vector3[Shift.CupCount];
        public readonly Vector3[] cupVel = new Vector3[Shift.CupCount];
        public readonly float[] cupUp = new float[Shift.CupCount];
        public readonly float dt;

        public static readonly Vector3[] Spawns = {
            new Vector3(-3f, 0f, -3f), new Vector3(-1f, 0f, -3f), new Vector3(1f, 0f, -3f), new Vector3(3f, 0f, -3f)
        };

        public FakeWorld(float stepDt) {
            dt = stepDt;
            for (int i = 0; i < actors.Length; i++) actors[i] = Spawns[i];
            for (int i = 0; i < actorAim.Length; i++) actorAim[i] = Vector3.forward;
            for (int i = 0; i < cups.Length; i++) cups[i] = new Vector3(0f, -100f, 0f);
            for (int i = 0; i < cupUp.Length; i++) cupUp[i] = 1f;
        }

        // Test shorthand for placing scene objects (physics would do this; the
        // tests place deterministically instead of simulating before the rule).
        public void PlaceActor(int actor, Vector3 position) { actors[actor] = position; }
        public void PlaceCup(int cup, Vector3 position) { cups[cup] = position; }

        // Physics integration: actors drift with their commanded velocity; cups
        // drift with their throw velocity and gravity.
        public void Integrate() {
            for (int i = 0; i < actors.Length; i++) actors[i] += actorVel[i] * dt;
            for (int i = 0; i < cups.Length; i++) {
                cupVel[i].y -= 9.8f * dt;
                cups[i] += cupVel[i] * dt;
            }
        }

        public Vector3 PlayerPosition(int actor) { return actors[actor]; }
        public Vector3 PlayerVelocity(int actor) { return actorVel[actor]; }
        public Vector3 CupPosition(int cup) { return cups[cup]; }
        public float CupUpDot(int cup) { return cupUp[cup]; }

        public void DrivePlayer(int actor, Vector3 xzVelocity, Vector3 aim) {
            actorVel[actor] = new Vector3(xzVelocity.x, actorVel[actor].y, xzVelocity.z);
            actorAim[actor] = aim;
        }
        public void FreezePlayer(int actor) { actorVel[actor] = Vector3.zero; }
        public void HoldCup(int actor, int cup) { cupVel[cup] = Vector3.zero; }
        public void ThrowCup(int cup, Vector3 velocity, Vector3 spinVelocity) { cupVel[cup] = velocity; }
        public void DropCup(int cup, Vector3 velocity) { cupVel[cup] = velocity; }
        public void PutOnRack(int cup) { cups[cup] = Shift.RackPosition; }
        public void ResetCupToPool(int cup) { cups[cup] = new Vector3(0f, -100f, 0f); cupVel[cup] = Vector3.zero; cupUp[cup] = 1f; }
        public void MarkCupServed(int cup) { cupVel[cup] = Vector3.zero; }
        public void RespawnPlayer(int actor) { actors[actor] = Spawns[actor]; actorVel[actor] = Vector3.zero; }
        public void SpawnCustomer(int slot) { }
        public void HideCustomer(int slot) { }
    }

    public sealed class ShiftSeamTests {
        static readonly float Tick = 0.02f;
        FakeWorld world;
        Shift shift;

        void SetupShift() {
            world = new FakeWorld(Tick);
            shift = new Shift(world);
            shift.Begin(7, Shift.PlayerCount);
        }

        void Step(float seconds) {
            int steps = Mathf.Max(1, Mathf.RoundToInt(seconds / Tick));
            for (int i = 0; i < steps; i++) {
                shift.Step(Tick);
                world.Integrate();
            }
        }

        /// Fills the rack with one produced cup (and machine backlog) by
        /// running production, then walks actor 0 to the rack and grabs.
        int GrabAtRack() {
            Step(12.02f);
            world.PlaceActor(0, new Vector3(Shift.RackPosition.x, 0f, Shift.RackPosition.z));
            shift.Command(0, ShiftVerb.Grab, ShiftArgs.None());
            for (int i = 0; i < Shift.CupCount; i++)
                if (shift.Metrics().CupStates[i] == CupState.Held) return i;
            return -1;
        }

        [Test]
        public void FlingThatLandsCreditsCulpritAndSpills() {
            SetupShift();
            int cupId = GrabAtRack();
            Assert.That(cupId, Is.GreaterThanOrEqualTo(0));

            world.PlaceActor(1, new Vector3(0.5f, 0f, 0.5f));
            int chaos = shift.Metrics().ChaosEvents;
            shift.Command(0, ShiftVerb.Aim, ShiftArgs.Aim(world.actors[1]));
            shift.Command(0, ShiftVerb.FlingBegin, ShiftArgs.None());
            Step(0.3f);
            shift.Command(0, ShiftVerb.FlingRelease, ShiftArgs.None());
            Assert.That(shift.Metrics().CupStates[cupId], Is.EqualTo(CupState.Airborne));
            Step(0.2f); // burns the owner-grace window (0.15s) before a hit can land
            Assert.That(shift.Metrics().CupLiquid[cupId], Is.EqualTo(1f).Within(0.001f));

            shift.OnCupHit(cupId, world.actors[1], 5f);

            Assert.That(shift.Metrics().Hits, Is.EqualTo(1));
            Assert.That(shift.Metrics().PlayerCulprits[0], Is.EqualTo(1));
            Assert.That(shift.Metrics().CupLiquid[cupId], Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(shift.Metrics().ChaosEvents, Is.GreaterThan(chaos));
            Assert.That(shift.Metrics().Roast, Does.Contain("beaned a coworker"));
        }

        [Test]
        public void ChugDrainsTheHeldCupThroughTheSeam() {
            SetupShift();
            int cupId = GrabAtRack();
            Assert.That(cupId, Is.GreaterThanOrEqualTo(0));
            Assert.That(shift.Metrics().CupLiquid[cupId], Is.EqualTo(1f).Within(0.001f));

            shift.Command(0, ShiftVerb.ChugBegin, ShiftArgs.None());
            Step(1f); // 1 second at ChugDrinkRate 0.35
            Assert.That(shift.Metrics().CupLiquid[cupId], Is.EqualTo(0.65f).Within(0.001f));
            Assert.That(shift.Metrics().Chugs, Is.EqualTo(1));

            shift.Command(0, ShiftVerb.ChugEnd, ShiftArgs.None());
            Step(1f);
            Assert.That(shift.Metrics().CupLiquid[cupId], Is.EqualTo(0.65f).Within(0.001f), "stopped chug must not keep draining");
        }

        [Test]
        public void StealRespectsDistanceAndEmptyHands() {
            SetupShift();
            int cupId = GrabAtRack();
            Assert.That(cupId, Is.GreaterThanOrEqualTo(0));

            // Far: no steal.
            world.PlaceActor(1, new Vector3(-3f, 0f, -3f));
            shift.Command(1, ShiftVerb.Steal, ShiftArgs.None());
            Assert.That(shift.Metrics().Steals, Is.EqualTo(0));

            // Close: the cup transfers, owner travels, culprit counts.
            world.PlaceActor(1, new Vector3(Shift.RackPosition.x, 0f, Shift.RackPosition.z));
            shift.Command(1, ShiftVerb.Steal, ShiftArgs.None());
            Assert.That(shift.Metrics().Steals, Is.EqualTo(1));
            Assert.That(shift.Metrics().PlayerCulprits[1], Is.EqualTo(1));
            Assert.That(shift.Metrics().CupOwner[cupId], Is.EqualTo(1));
            int held = 0;
            for (int i = 0; i < Shift.CupCount; i++) if (shift.Metrics().CupStates[i] == CupState.Held) held++;
            Assert.That(held, Is.EqualTo(1), "exactly one barista holds the cup");

            // Thief's hands full: no second steal.
            int chaos = shift.Metrics().ChaosEvents;
            shift.Command(1, ShiftVerb.Steal, ShiftArgs.None());
            Assert.That(shift.Metrics().Steals, Is.EqualTo(1));
            // Empty-handed victim far from anyone holding: no steal either.
            shift.Command(0, ShiftVerb.Steal, ShiftArgs.None());
            Assert.That(shift.Metrics().Steals, Is.EqualTo(1));
        }

        [Test]
        public void MachineProductionDrainsBacklogToTheRack() {
            SetupShift();
            // 4 machines produce every 4s over 12s: machine 0 puts one cup on
            // the rack (t=4) then its later cups and everyone else's go into
            // backlog (rack stays full).
            Step(12.02f);
            Assert.That(shift.Metrics().MachineBacklog[0], Is.EqualTo(2));
            Assert.That(shift.Metrics().RackCups, Is.EqualTo(1));

            world.PlaceActor(0, new Vector3(Shift.RackPosition.x, 0f, Shift.RackPosition.z));
            shift.Command(0, ShiftVerb.Grab, ShiftArgs.None());
            Assert.That(shift.Metrics().RackCups, Is.EqualTo(0));
            Assert.That(shift.Metrics().MachineBacklog[0], Is.EqualTo(2), "rack cup drains first, not backlog");

            shift.Command(0, ShiftVerb.Grab, ShiftArgs.None());
            Assert.That(shift.Metrics().MachineBacklog[0], Is.EqualTo(1), "backlog cup drains to the rack");
            Assert.That(shift.Metrics().RackCups, Is.EqualTo(0));
        }

        [Test]
        public void QuotaMissBringsBigTony() {
            SetupShift();
            Step(183.5f); // shift 180s + grace 3s; zero serves
            ShiftMetrics m = shift.Metrics();
            Assert.That(m.ShiftOver, Is.True);
            Assert.That(m.QuotaMet, Is.False);
            Assert.That(m.FixturesLost, Is.EqualTo(1));
            Assert.That(m.BrokenMachines, Is.EqualTo(1));
            int broken = -1;
            for (int i = 0; i < 4; i++) if (MachineRules.IsBroken(m.MachineHealth[i])) broken = i;
            Assert.That(broken, Is.GreaterThanOrEqualTo(0));
            Assert.That(m.RoundSummary, Does.Contain("Big Tony"));
        }

        [Test]
        public void OrderExpiryWalksACustomer() {
            SetupShift();
            Step(52.5f); // orders spawn at 12/24/36 with 40s patience each
            ShiftMetrics m = shift.Metrics();
            Assert.That(m.ExpiredOrders, Is.EqualTo(1));
            Assert.That(m.Orders[0].Active, Is.False, "first order walked");
            Assert.That(m.Orders[1].Active, Is.True, "second customer still waiting");
            Assert.That(m.Roast, Does.Contain("walked"));
        }

        [Test]
        public void RestartShiftResetsAllRoundState() {
            SetupShift();
            int cupId = GrabAtRack();
            Assert.That(cupId, Is.GreaterThanOrEqualTo(0));
            shift.Command(0, ShiftVerb.FlingBegin, ShiftArgs.None());
            Step(0.3f);
            shift.Command(0, ShiftVerb.FlingRelease, ShiftArgs.None());
            Step(0.2f);
            shift.OnServe(cupId);
            Assert.That(shift.Metrics().Serves, Is.EqualTo(1));
            Assert.That(shift.Metrics().TotalTips, Is.GreaterThan(0));

            shift.RestartShift();
            ShiftMetrics m = shift.Metrics();
            Assert.That(m.Serves, Is.EqualTo(0));
            Assert.That(m.TotalTips, Is.EqualTo(0));
            Assert.That(m.ShiftElapsed, Is.EqualTo(0f));
            Assert.That(m.RackCups, Is.EqualTo(0));
            Assert.That(m.FixturesLost, Is.EqualTo(0));
            for (int i = 0; i < Shift.CupCount; i++) {
                Assert.That(m.CupStates[i], Is.EqualTo(CupState.Pool));
                Assert.That(m.CupLiquid[i], Is.EqualTo(0f));
                Assert.That(m.CupOwner[i], Is.EqualTo(-1));
            }
            for (int i = 0; i < 4; i++) Assert.That(m.MachineHealth[i], Is.EqualTo(1f));
        }
    }
}
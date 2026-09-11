using UnityEngine;

namespace KwaFee {
    /// Verbs an actor (human input, bot policy, or SimHarness) sends through
    /// the Shift seam. Fling is split into charge/release phases and Chug into
    /// begin/end so the Shift owns every charge and chug transition.
    public enum ShiftVerb { Move, Aim, Grab, FlingBegin, FlingRelease, ChugBegin, ChugEnd, Drop, Fix, Steal, Sabotage }

    public sealed class ShiftArgs {
        public Vector2 MoveDirection = Vector2.zero;
        public Vector3 AimPoint = Vector3.zero;

        public static ShiftArgs Move(Vector2 direction) { ShiftArgs a = new ShiftArgs(); a.MoveDirection = direction; return a; }
        public static ShiftArgs Aim(Vector3 point) { ShiftArgs a = new ShiftArgs(); a.AimPoint = point; return a; }
        public static ShiftArgs None() { return new ShiftArgs(); }
    }

    /// Kinematic/visual surface the Shift commands. CoreGame implements it over
    /// the real scene; tests implement a fake over plain data. No rule lives on
    /// this side — only transform/velocity/visual writes and position reads.
    public interface ShiftWorld {
        Vector3 PlayerPosition(int actor);
        Vector3 PlayerVelocity(int actor);
        Vector3 CupPosition(int cup);
        float CupUpDot(int cup);
        /// Applies the whole per-actor kinematic write the old Barista.Tick did:
        /// body velocity (y preserved), wake, facing rotation, held-cup follow.
        void DrivePlayer(int actor, Vector3 xzVelocity, Vector3 aim);
        /// Stun freeze: fully zero the body velocity (old Barista.Tick did a
        /// full-zero write when stunned, not the y-preserving drive).
        void FreezePlayer(int actor);
        void HoldCup(int actor, int cup);
        void ThrowCup(int cup, Vector3 velocity, Vector3 spinVelocity);
        void DropCup(int cup, Vector3 velocity);
        void PutOnRack(int cup);
        void ResetCupToPool(int cup);
        void MarkCupServed(int cup);
        void RespawnPlayer(int actor);
        void SpawnCustomer(int slot);
        void HideCustomer(int slot);
    }

    public sealed class ShiftOrderView {
        public int Slot;
        public bool Active;
        public string Demand = "";
        public float Patience;
    }

    /// Single exit for round state. The HUD and the /sim report read only this.
    /// A cached mutable snapshot refreshed by Begin/Command/Step — zero per-frame
    /// allocation.
    public sealed class ShiftMetrics {
        public int Shots, Serves, Misses, Catches, Hits, Chugs, Overdoses, Fixes, Steals, Sabotages, ChaosEvents, ExpiredOrders, FixturesLost;
        public float ShiftElapsed;
        public bool ShiftOver, QuotaMet;
        public string RoundSummary = "";
        public string Roast = "";
        public float RoastTime;
        public int TotalTips;
        public int ActiveOrders, BrokenMachines, RackCups;
        public readonly int[] PlayerTips = new int[Shift.PlayerCount];
        public readonly int[] PlayerCulprits = new int[Shift.PlayerCount];
        public readonly int[] PlayerChugs = new int[Shift.PlayerCount];
        public readonly float[] MachineHealth = new float[4];
        public readonly int[] MachineBacklog = new int[4];
        public readonly CupState[] CupStates = new CupState[Shift.CupCount];
        public readonly float[] CupLiquid = new float[Shift.CupCount];
        public readonly int[] CupOwner = new int[Shift.CupCount];
        public readonly ShiftOrderView[] Orders = makeOrders(Shift.MaxOrders);
        public readonly float[] Charge01 = new float[Shift.PlayerCount];
        public readonly float[] Chug01 = new float[Shift.PlayerCount];
        public readonly float[] Stun = new float[Shift.PlayerCount];

        static ShiftOrderView[] makeOrders(int count) {
            ShiftOrderView[] views = new ShiftOrderView[count];
            for (int i = 0; i < count; i++) views[i] = new ShiftOrderView();
            return views;
        }
    }

    /// One deep module behind one seam. All gameplay intent enters through
    /// Command; all round state exits through Metrics; Begin and Step own the
    /// round lifecycle. MonoBehaviours keep only kinematic/visual state and
    /// forward physics callbacks here. Deterministic: seeded System.Random,
    /// fixed dt, no world-clock or RNG reads — the sim adapter owns physics
    /// stepping and calls Step(dt) then Simulate(dt) in lockstep.
    public sealed class Shift {
        public const int PlayerCount = 4;
        public const int CupCount = 16;
        public const int MaxOrders = 3;

        public const float RackGrabDistanceSquared = 3.2f;
        public static readonly Vector3 RackPosition = new Vector3(-4f, 1.05f, -0.6f);
        public static readonly Vector3[] MachinePositions = {
            new Vector3(-2.5f, 0f, 1.8f), new Vector3(-.8f, 0f, 1.8f), new Vector3(.9f, 0f, 1.8f), new Vector3(2.6f, 0f, 1.8f)
        };
        static readonly string[] machineNames = { "Espresso machine", "Grinder", "Steam Wand", "Ice Machine" };
        static readonly string[] demands = {
            "TRIPLE SHOT. NO FOAM. HURRY.",
            "decaf. DECAF. I WILL KNOW.",
            "one milk, undah 4 degrees.",
            "whatevah's fast. I got a cousin watchin' my cah.",
            "extra hot. BURN me like a Red Sox loss.",
            "two sugars, no lid. LIVE dangerous."
        };
        public static readonly Vector3[] SlotPositions = { new Vector3(-4.5f, 0f, 3.2f), new Vector3(0f, 0f, 3.2f), new Vector3(4.5f, 0f, 3.2f) };

        sealed class ActorState {
            public Vector2 move = Vector2.zero;
            public Vector3 aim = Vector3.forward;
            public int heldCup = -1;
            public float charge, chugSeconds, stun;
            public bool charging, chugging, overdosed;
        }

        sealed class CupRecord {
            public CupState state = CupState.Pool;
            public int ownerId = -1;
            public float liquid, age, grace;
            public bool wasAirborne, hitPlayer;
        }

        sealed class ShiftOrder {
            public readonly int Slot;
            public string Demand = "";
            public float Patience;
            public bool Served, Expired, Spawned;
            public ShiftOrder(int slot) { Slot = slot; }
        }

        readonly ShiftWorld world;
        public float MoveSpeed = 5f;
        public bool Initialized => initialized;

        readonly ActorState[] actors = new ActorState[PlayerCount];
        readonly CupRecord[] cups = new CupRecord[CupCount];
        readonly ShiftOrder[] orders = new ShiftOrder[MaxOrders];
        readonly float[] machineHealth = new float[4];
        readonly float[] machineTimers = new float[4];
        readonly int[] machineBacklog = new int[4];
        TipLedger tips;
        System.Random random;
        bool initialized, shiftOver, quotaMet, tonyCalled;
        float shiftElapsed, lastOrderSpawn, roastTime;
        string roast = "Get a cup movin', ya statue.";
        string roundSummary = "";
        int Shots, Serves, Misses, Catches, Hits, Chugs, Overdoses, Fixes, Steals, Sabotages, ChaosEvents, ExpiredOrders, FixturesLost;
        readonly ShiftMetrics metrics = new ShiftMetrics();

        public Shift(ShiftWorld world) {
            this.world = world;
            for (int i = 0; i < PlayerCount; i++) actors[i] = new ActorState();
            for (int i = 0; i < CupCount; i++) cups[i] = new CupRecord();
            for (int i = 0; i < MaxOrders; i++) orders[i] = new ShiftOrder(i);
        }

        /// Spawn/bind the pools and reset round state. Callable once per fresh
        /// Shift; human play and SimHarness both bind through this.
        public void Begin(int seed, int actorCount) {
            random = new System.Random(seed);
            ResetRound();
        }

        /// Same reset, same RNG stream (R-restart keeps the seeded sequence so
        /// a replay never re-rolls identical shifts).
        public void RestartShift() {
            ResetRound();
        }

        void ResetRound() {
            initialized = true;
            shiftOver = false; quotaMet = false; tonyCalled = false;
            shiftElapsed = 0f; lastOrderSpawn = -RoundRules.OrderSpawnInterval;
            tips = new TipLedger(PlayerCount);
            for (int i = 0; i < 4; i++) { machineHealth[i] = 1f; machineTimers[i] = 0f; machineBacklog[i] = 0; }
            for (int i = 0; i < CupCount; i++) {
                CupRecord cup = cups[i];
                cup.state = CupState.Pool; cup.ownerId = -1; cup.liquid = 0f; cup.age = 0f; cup.grace = 0f;
                cup.wasAirborne = false; cup.hitPlayer = false;
                world.ResetCupToPool(i);
            }
            for (int i = 0; i < MaxOrders; i++) HideOrder(orders[i]);
            for (int i = 0; i < actors.Length; i++) {
                ActorState actor = actors[i];
                actor.move = Vector2.zero; actor.aim = Vector3.forward; actor.heldCup = -1;
                actor.charge = 0f; actor.chugSeconds = 0f; actor.stun = 0f;
                actor.charging = false; actor.chugging = false; actor.overdosed = false;
                world.RespawnPlayer(i);
            }
            Shots = 0; Serves = 0; Misses = 0; Catches = 0; Hits = 0; Chugs = 0; Overdoses = 0;
            Fixes = 0; Steals = 0; Sabotages = 0; ChaosEvents = 0; ExpiredOrders = 0; FixturesLost = 0;
            for (int i = 0; i < PlayerCount; i++) { metrics.PlayerCulprits[i] = 0; metrics.PlayerChugs[i] = 0; }
            roundSummary = "";
            Roast("Big Vinny: Ya got hands. Use 'em. Quota's " + RoundRules.QuotaTarget + " kehds.");
            RefreshMetrics();
        }

        // ------------------------------------------------------------------
        // Command surface: every gameplay intent from every actor.
        // ------------------------------------------------------------------

        public void Command(int actor, ShiftVerb verb, ShiftArgs args) {
            if (actor < 0 || actor >= PlayerCount) return;
            ActorState a = actors[actor];
            switch (verb) {
                case ShiftVerb.Move:
                    a.move = Vector2.ClampMagnitude(args.MoveDirection, 1f);
                    break;
                case ShiftVerb.Aim: {
                    Vector3 flat = args.AimPoint - world.PlayerPosition(actor);
                    flat.y = 0f;
                    if (flat.sqrMagnitude > .01f) a.aim = flat.normalized;
                    break;
                }
                case ShiftVerb.Grab:
                    if ((world.PlayerPosition(actor) - RackPosition).sqrMagnitude < RackGrabDistanceSquared) {
                        if (a.heldCup >= 0) { Roast("Big Vinny: Hands full, kehd."); }
                        else if (!GrabOrSpawnRackCup(actor)) { Roast("Big Vinny: Pool's dry, ya animal."); }
                    } else if (a.heldCup < 0 && a.stun <= 0f) {
                        TryGrab(actor);
                    }
                    break;
                case ShiftVerb.FlingBegin:
                    if (a.heldCup >= 0 && a.stun <= 0f) a.charging = true;
                    break;
                case ShiftVerb.FlingRelease:
                    if (a.heldCup < 0 || !a.charging) return;
                    {
                        float speed = FlingRules.SpeedForCharge(a.charge);
                        Vector3 velocity = a.aim * speed + Vector3.up * .5f + world.PlayerVelocity(actor);
                        ThrowHeldCup(actor, velocity);
                    }
                    break;
                case ShiftVerb.ChugBegin:
                    if (a.stun > 0f || a.chugging) return;
                    if (!VerbRules.CanChug(a.heldCup >= 0, a.heldCup >= 0 ? cups[a.heldCup].liquid : 0f)) {
                        Roast("Big Vinny: Nothin' to slug, kehd."); return;
                    }
                    a.chugging = true;
                    Chugs++; ChaosEvents++;
                    if (actor >= 0 && actor < metrics.PlayerChugs.Length) metrics.PlayerChugs[actor]++;
                    Roast("Big Vinny: Drinkin' coffee like it owes ya money.");
                    break;
                case ShiftVerb.ChugEnd:
                    a.chugging = false;
                    break;
                case ShiftVerb.Drop:
                    if (a.heldCup >= 0) {
                        world.DropCup(a.heldCup, world.PlayerVelocity(actor));
                        CupRecord cup = cups[a.heldCup];
                        cup.state = CupState.Airborne; cup.age = 0f;
                        a.heldCup = -1; a.charging = false;
                    }
                    break;
                case ShiftVerb.Fix:
                    if (a.stun > 0f) return;
                    TryFix(actor);
                    break;
                case ShiftVerb.Steal:
                    if (a.stun > 0f || a.heldCup >= 0) return;
                    TrySteal(actor);
                    break;
                case ShiftVerb.Sabotage:
                    if (a.stun > 0f) return;
                    TrySabotage(actor);
                    break;
            }
            RefreshMetrics();
        }

        // ------------------------------------------------------------------
        // Physics callbacks forwarded by the MonoBehaviours; verdicts live here.
        // ------------------------------------------------------------------

        /// ServeZone trigger: a flung cup crosses the tray.
        public void OnServe(int cupId) {
            if (cupId < 0 || cupId >= CupCount) return;
            CupRecord cup = cups[cupId];
            if (cup.state != CupState.Airborne || !cup.wasAirborne || cup.ownerId < 0 || cup.liquid < .2f || shiftOver) return;
            ShiftOrder order = FindWaitingOrder();
            if (order == null) { Roast("Big Vinny: No one waitin', kehd. Drink it yourself."); RefreshMetrics(); return; }
            cup.state = CupState.Served;
            world.MarkCupServed(cupId);
            Serves++; order.Served = true; HideOrder(order);
            int tip = FlingRules.TipsForServe(cup.liquid); tips.AddServe(cup.ownerId, tip); ChaosEvents++;
            Roast("Big Vinny: " + order.Demand + " — handled. P" + (cup.ownerId + 1) + " banks $" + tip + ".");
            RefreshMetrics();
        }

        /// CoffeeCup collision: possible hit on a coworker (plus speed spill).
        public void OnCupHit(int cupId, Vector3 point, float relativeSpeed) {
            if (cupId < 0 || cupId >= CupCount) return;
            CupRecord cup = cups[cupId];
            if (cup.state != CupState.Airborne) return;
            if (relativeSpeed > 2f) cup.liquid = Mathf.Max(0f, cup.liquid - Mathf.Clamp01((relativeSpeed - 2f) * .08f));
            if (relativeSpeed < 3f || cup.grace > 0f) { RefreshMetrics(); return; }
            for (int i = 0; i < actors.Length; i++) {
                if (i == cup.ownerId) continue;
                if ((point - world.PlayerPosition(i)).sqrMagnitude < 1.4f * 1.4f) {
                    Stun(i);
                    cup.liquid = Mathf.Max(0f, cup.liquid - .5f);
                    cup.hitPlayer = true;
                    Hits++; ChaosEvents++;
                    if (cup.ownerId >= 0 && cup.ownerId < metrics.PlayerCulprits.Length) metrics.PlayerCulprits[cup.ownerId]++;
                    Roast("Big Vinny: Ya beaned a coworker. Payroll loves it.");
                    break;
                }
            }
            RefreshMetrics();
        }

        // ------------------------------------------------------------------
        // The fixed-sim tick: Step(dt) then physics Simulate(dt), in lockstep.
        // ------------------------------------------------------------------

        public void Step(float dt) {
            if (!initialized || dt <= 0f) return;
            if (!shiftOver) {
                shiftElapsed += dt;
                for (int i = 0; i < 4; i++) {
                    machineHealth[i] = MachineRules.Degrade(machineHealth[i], dt);
                    if (!MachineRules.IsBroken(machineHealth[i])) {
                        machineTimers[i] += dt;
                        float delay = MachineRules.ProductionDelay(machineHealth[i]);
                        while (machineTimers[i] >= delay) { machineTimers[i] -= delay; TryProduce(i); }
                    }
                }
                UpdateOrders(dt);
                if (RoundRules.ShouldSpawnOrder(shiftElapsed, lastOrderSpawn, ActiveOrders())) { SpawnOrder(); lastOrderSpawn = shiftElapsed; }
                if (shiftElapsed >= RoundRules.ShiftLength && !tonyCalled) BigTony();
                if (shiftElapsed >= RoundRules.ShiftLength + RoundRules.ShiftEndGrace) EndShift();
            }
            for (int i = 0; i < actors.Length; i++) TickActor(i, dt);
            TickCups(dt);
            if (roastTime > 0) roastTime -= dt;
            RefreshMetrics();
        }

        void TickActor(int actor, float dt) {
            ActorState a = actors[actor];
            if (a.stun > 0f) {
                a.stun -= dt;
                world.FreezePlayer(actor);
                if (a.stun <= 0f && a.overdosed) {
                    a.overdosed = false;
                    world.RespawnPlayer(actor);
                }
                a.charging = false;
                return;
            }
            float boost = VerbRules.ChugMultiplier(a.chugSeconds);
            // Note: old Barista jitter used `move.y - jitter` on the z axis; kept
            // verbatim — the wobble asymmetry is deterministic and playtested.
            float jitter = a.chugSeconds > 0f ? Mathf.Sin(a.chugSeconds * 18f + actor) * .06f : 0f;
            Vector3 velocity = new Vector3(a.move.x + jitter, 0f, a.move.y - jitter) * MoveSpeed * boost;
            world.DrivePlayer(actor, velocity, a.aim);
            if (a.charging) a.charge = Mathf.Min(FlingRules.ChargeDuration, a.charge + dt);
            if (a.chugging) {
                a.chugSeconds += dt;
                if (a.heldCup >= 0 && cups[a.heldCup].liquid > 0f) {
                    cups[a.heldCup].liquid = VerbRules.ChugConsume(cups[a.heldCup].liquid, dt);
                    if (cups[a.heldCup].liquid < VerbRules.MinChugLiquid) a.chugging = false;
                } else a.chugging = false;
                if (VerbRules.IsOverdose(a.chugSeconds)) { TriggerOverdose(actor); return; }
            } else a.chugSeconds = Mathf.Max(0f, a.chugSeconds - dt * .65f);
        }

        void TickCups(float dt) {
            for (int i = 0; i < cups.Length; i++) {
                CupRecord cup = cups[i];
                if (cup.grace > 0f) cup.grace -= dt;
                if (cup.state == CupState.Airborne || cup.state == CupState.Served) cup.age += dt;
                if (cup.state == CupState.Airborne && world.CupUpDot(i) < .2f) cup.liquid = Mathf.Max(0f, cup.liquid - dt * .55f);
                if (cup.state == CupState.Served && cup.age > 1.1f) { ResetCupToPool(i); continue; }
                if (cup.state == CupState.Airborne && !cup.hitPlayer) {
                    Vector3 pos = world.CupPosition(i);
                    if (cup.age > 8f || pos.y < -2f || Mathf.Abs(pos.x) > 10f || Mathf.Abs(pos.z) > 8f) {
                        Misses++; ResetCupToPool(i);
                        Roast("Big Vinny: That coffee took the T outta town.");
                    }
                }
            }
        }

        // ------------------------------------------------------------------
        // Verbs.
        // ------------------------------------------------------------------

        void TryGrab(int actor) {
            CupRecord bestCup = null; int bestId = -1; float bestDistance = 1.2f * 1.2f;
            for (int i = 0; i < cups.Length; i++) {
                CupRecord cup = cups[i];
                if (cup.state == CupState.Pool || cup.state == CupState.Served || cup.state == CupState.Held) continue;
                float d = (world.CupPosition(i) - world.PlayerPosition(actor)).sqrMagnitude;
                if (d < bestDistance) { bestCup = cup; bestId = i; bestDistance = d; }
            }
            if (bestCup != null) {
                if (bestCup.state == CupState.Airborne) Catches++;
                HoldCup(actor, bestId);
                Roast("Big Vinny: Hands work. Who knew?");
            }
        }

        bool GrabOrSpawnRackCup(int actor) {
            int cupId = FindCupInState(CupState.Rack);
            if (cupId >= 0) {
                HoldCup(actor, cupId);
                Roast("Big Vinny: Rack's got one. Don't baptize it.");
                return true;
            }
            for (int i = 0; i < machineBacklog.Length; i++) if (machineBacklog[i] > 0) {
                machineBacklog[i]--;
                cupId = FindCupInState(CupState.Pool);
                if (cupId < 0) return false;
                cups[cupId].state = CupState.Rack; cups[cupId].liquid = 1f; cups[cupId].age = 0f;
                world.PutOnRack(cupId);
                HoldCup(actor, cupId);
                Roast("Big Vinny: Rack's got one. Don't baptize it.");
                return true;
            }
            return false;
        }

        void HoldCup(int actor, int cupId) {
            CupRecord cup = cups[cupId];
            cup.state = CupState.Held; cup.ownerId = actor; cup.age = 0f;
            actors[actor].heldCup = cupId;
            world.HoldCup(actor, cupId);
        }

        void ThrowHeldCup(int actor, Vector3 velocity) {
            CupRecord cup = cups[actors[actor].heldCup];
            int cupId = actors[actor].heldCup;
            cup.state = CupState.Airborne; cup.wasAirborne = true; cup.age = 0f; cup.grace = .15f;
            actors[actor].heldCup = -1;
            actors[actor].charging = false;
            actors[actor].charge = 0f;
            world.ThrowCup(cupId, velocity, SpinForCup() * 13f);
            Shots++; ChaosEvents++;
            Roast("Big Vinny: Incoming, ya maniac.");
        }

        void TryFix(int actor) {
            int index = NearestMachine(world.PlayerPosition(actor));
            if (index < 0) { Roast("Big Vinny: The wrench ain't psychic, kehd."); return; }
            machineHealth[index] = VerbRules.Repair(machineHealth[index]); Fixes++;
            Roast("Big Vinny: Fixed it. Try breakin' it slower.");
        }

        void TrySteal(int actor) {
            int target = -1; int cupId = -1; float best = 1.6f * 1.6f;
            for (int i = 0; i < actors.Length; i++) {
                if (i == actor || actors[i].heldCup < 0) continue;
                float d = (world.PlayerPosition(i) - world.PlayerPosition(actor)).sqrMagnitude;
                if (d < best && VerbRules.CanSteal(true, actors[actor].heldCup >= 0, Mathf.Sqrt(d))) { target = i; cupId = actors[i].heldCup; best = d; }
            }
            if (target < 0) { Roast("Big Vinny: Nobody's holdin' anything worth stealin'. Yet."); return; }
            CupRecord cup = cups[cupId];
            actors[target].heldCup = -1; actors[target].charging = false;
            HoldCup(actor, cupId);
            Steals++; ChaosEvents++;
            if (actor >= 0 && actor < metrics.PlayerCulprits.Length) metrics.PlayerCulprits[actor]++;
            Roast("Big Vinny: Theft of the night, and it's only Tuesday.");
        }

        void TrySabotage(int actor) {
            int index = NearestMachine(world.PlayerPosition(actor));
            if (index < 0) { Roast("Big Vinny: No machine, no mischief. Walk two steps."); return; }
            machineHealth[index] = VerbRules.Sabotage(machineHealth[index]); Sabotages++; ChaosEvents++;
            if (actor >= 0 && actor < metrics.PlayerCulprits.Length) metrics.PlayerCulprits[actor]++;
            Roast("Big Vinny: That's gonna be somebody else's problem.");
        }

        void Stun(int actor) {
            ActorState a = actors[actor];
            a.stun = Mathf.Max(a.stun, .65f);
            a.charging = false;
            if (a.heldCup >= 0) {
                world.DropCup(a.heldCup, world.PlayerVelocity(actor));
                CupRecord cup = cups[a.heldCup];
                cup.state = CupState.Airborne; cup.age = 0f;
                a.heldCup = -1;
            }
        }

        void TriggerOverdose(int actor) {
            ActorState a = actors[actor];
            Overdoses++; ChaosEvents++;
            a.chugging = false; a.chugSeconds = 0f; a.overdosed = true;
            a.stun = Mathf.Max(a.stun, 3f);
            if (a.heldCup >= 0) {
                world.DropCup(a.heldCup, world.PlayerVelocity(actor));
                CupRecord cup = cups[a.heldCup];
                cup.state = CupState.Airborne; cup.age = 0f;
                a.heldCup = -1;
            }
            Roast("Big Vinny: The jitters got a BODY COUNT, kehd.");
        }

        // ------------------------------------------------------------------
        // Machines, orders, round lifecycle.
        // ------------------------------------------------------------------

        void TryProduce(int machineIndex) {
            // Each produced cup wears the machine; a worn machine works slower.
            machineHealth[machineIndex] = Mathf.Max(0f, machineHealth[machineIndex] - MachineRules.UseDegradePerCup);
            if (FindCupInState(CupState.Rack) < 0) {
                int poolCup = FindCupInState(CupState.Pool);
                if (poolCup >= 0) {
                    cups[poolCup].state = CupState.Rack; cups[poolCup].liquid = 1f; cups[poolCup].age = 0f;
                    world.PutOnRack(poolCup);
                    return;
                }
            }
            machineBacklog[machineIndex] = Mathf.Min(3, machineBacklog[machineIndex] + 1);
        }

        void UpdateOrders(float dt) {
            for (int i = 0; i < orders.Length; i++) {
                ShiftOrder order = orders[i];
                if (!order.Spawned || order.Served || order.Expired) continue;
                order.Patience = RoundRules.PatienceAfter(order.Patience, dt);
                if (RoundRules.OrderExpired(order.Patience)) ExpireOrder(order);
            }
        }

        void SpawnOrder() {
            for (int i = 0; i < orders.Length; i++) if (!orders[i].Spawned) {
                ShiftOrder order = orders[i];
                order.Spawned = true; order.Served = false; order.Expired = false;
                order.Demand = demands[(int)(random.NextDouble() * demands.Length) % demands.Length];
                order.Patience = RoundRules.CustomerPatience;
                world.SpawnCustomer(order.Slot);
                Roast("Big Vinny: New kehd. Wants: " + order.Demand);
                return;
            }
        }

        void ExpireOrder(ShiftOrder order) {
            order.Expired = true; ExpiredOrders++; ChaosEvents++; HideOrder(order);
            Roast("Big Vinny: Customer walked. Tips went with 'em. That's on you.");
        }

        void HideOrder(ShiftOrder order) {
            order.Spawned = false; order.Served = false; order.Expired = false;
            world.HideCustomer(order.Slot);
        }

        ShiftOrder FindWaitingOrder() {
            for (int i = 0; i < orders.Length; i++) if (orders[i].Spawned && !orders[i].Served && !orders[i].Expired) return orders[i];
            return null;
        }

        int ActiveOrders() {
            int n = 0;
            for (int i = 0; i < orders.Length; i++) if (orders[i].Spawned && !orders[i].Served && !orders[i].Expired) n++;
            return n;
        }

        void BigTony() {
            tonyCalled = true; quotaMet = RoundRules.QuotaMet(Serves);
            if (!quotaMet) {
                int fixture = (int)(random.NextDouble() * 4) % 4;
                machineHealth[fixture] = 0f; FixturesLost++;
                Roast("Big Vinny: Quota missed. Big Tony took da " + machineNames[fixture] + ". Good luck fixin' that.");
            }
        }

        void EndShift() {
            shiftOver = true;
            int topTipper = 0, topTips = tips.Balance(0), junkie = 0, junkieChugs = metrics.PlayerChugs[0], culprit = 0, culpritScore = metrics.PlayerCulprits[0];
            for (int i = 1; i < PlayerCount; i++) {
                int t = tips.Balance(i); if (t > topTips) { topTips = t; topTipper = i; }
                int c = metrics.PlayerChugs[i]; if (c > junkieChugs) { junkieChugs = c; junkie = i; }
                int u = metrics.PlayerCulprits[i]; if (u > culpritScore) { culpritScore = u; culprit = i; }
            }
            roundSummary = (quotaMet ? "SHIFT SAVED. Vinny's shocked." : "QUOTA MISSED. Big Tony took a fixture.") +
                "  TOP TIPPER: P" + (topTipper + 1) + " $" + topTips + "  COFFEE JUNKIE: P" + (junkie + 1) +
                "  BIGGEST CULPRIT: P" + (culprit + 1) + "  Wrekt: " + culpritScore + "  SERVED " + Serves + "/" + RoundRules.QuotaTarget;
            Roast(roundSummary);
        }

        int NearestMachine(Vector3 position) {
            int best = -1; float distance = 2.25f * 2.25f;
            for (int i = 0; i < MachinePositions.Length; i++) {
                float d = (position - MachinePositions[i]).sqrMagnitude;
                if (d < distance) { distance = d; best = i; }
            }
            return best;
        }

        int FindCupInState(CupState state) {
            for (int i = 0; i < cups.Length; i++) if (cups[i].state == state) return i;
            return -1;
        }

        void ResetCupToPool(int cupId) {
            CupRecord cup = cups[cupId];
            cup.state = CupState.Pool; cup.ownerId = -1; cup.liquid = 0f; cup.age = 0f;
            cup.grace = 0f; cup.wasAirborne = false; cup.hitPlayer = false;
            world.ResetCupToPool(cupId);
        }

        Vector3 SpinForCup() {
            Vector3 spin = new Vector3((float)random.NextDouble() * 2f - 1f, (float)random.NextDouble() * 2f - 1f, (float)random.NextDouble() * 2f - 1f);
            return spin.sqrMagnitude < .01f ? Vector3.up : spin.normalized;
        }

        void Roast(string line) { roast = line; roastTime = 3.5f; }

        // ------------------------------------------------------------------
        // Metrics: the single exit.
        // ------------------------------------------------------------------

        public ShiftMetrics Metrics() { return metrics; }

        void RefreshMetrics() {
            metrics.Shots = Shots; metrics.Serves = Serves; metrics.Misses = Misses; metrics.Catches = Catches;
            metrics.Hits = Hits; metrics.Chugs = Chugs; metrics.Overdoses = Overdoses; metrics.Fixes = Fixes;
            metrics.Steals = Steals; metrics.Sabotages = Sabotages; metrics.ChaosEvents = ChaosEvents;
            metrics.ExpiredOrders = ExpiredOrders; metrics.FixturesLost = FixturesLost;
            metrics.ShiftElapsed = shiftElapsed; metrics.ShiftOver = shiftOver; metrics.QuotaMet = quotaMet;
            metrics.RoundSummary = roundSummary; metrics.Roast = roast; metrics.RoastTime = roastTime;
            metrics.TotalTips = tips != null ? tips.Total : 0;
            metrics.ActiveOrders = ActiveOrders();
            metrics.BrokenMachines = 0;
            metrics.RackCups = 0;
            for (int i = 0; i < 4; i++) {
                metrics.MachineHealth[i] = machineHealth[i];
                metrics.MachineBacklog[i] = machineBacklog[i];
                if (MachineRules.IsBroken(machineHealth[i])) metrics.BrokenMachines++;
                metrics.PlayerTips[i] = tips != null ? tips.Balance(i) : 0;
                metrics.Charge01[i] = FlingRules.Charge01(actors[i].charge);
                metrics.Chug01[i] = Mathf.Clamp01(actors[i].chugSeconds / VerbRules.OverdoseSeconds);
                metrics.Stun[i] = actors[i].stun;
            }
            for (int i = 0; i < CupCount; i++) {
                metrics.CupStates[i] = cups[i].state;
                metrics.CupLiquid[i] = cups[i].liquid;
                metrics.CupOwner[i] = cups[i].ownerId;
                if (cups[i].state == CupState.Rack) metrics.RackCups++;
            }
            for (int i = 0; i < MaxOrders; i++) {
                ShiftOrderView view = metrics.Orders[i];
                ShiftOrder order = orders[i];
                view.Slot = order.Slot;
                view.Active = order.Spawned && !order.Served && !order.Expired;
                view.Demand = order.Demand;
                view.Patience = order.Patience;
            }
        }
    }
}
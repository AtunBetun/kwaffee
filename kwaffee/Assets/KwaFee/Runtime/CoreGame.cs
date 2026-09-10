using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KwaFee {
    public sealed class CoreGame : MonoBehaviour {
        [Header("Blender prefabs")]
        public GameObject cupPrefab, baristaPrefab, customerPrefab, counterPrefab, floorPrefab, wallPrefab, trayPrefab, signPrefab;
        public GameObject espressoPrefab, grinderPrefab, steamWandPrefab, iceMachinePrefab;
        public float MoveSpeed = 5f;
        public int Shots { get; private set; } public int Serves { get; private set; } public int Misses { get; private set; }
        public int Catches { get; private set; } public int Hits { get; private set; }
        public int Chugs { get; private set; } public int Overdoses { get; private set; } public int Fixes { get; private set; }
        public int Steals { get; private set; } public int Sabotages { get; private set; } public int ChaosEvents { get; private set; }
        public float MachineHealth(MachineKind machine) => machineHealth[(int)machine];
        public float SharedChaos => Mathf.Clamp01(ChaosEvents / 24f);
        public IReadOnlyList<Barista> Players => players; public IReadOnlyList<CoffeeCup> Cups => cups;
        public Vector3 PoolPosition => transform.position + Vector3.down * 10f;
        public int TotalTips => tips != null ? tips.Total : 0;
        public bool Initialized => initialized;
        public int PlayerTips(int playerId) => tips != null ? tips.Balance(playerId) : 0;
        public int BrokenMachines { get { int n = 0; for (int i = 0; i < machineHealth.Length; i++) if (MachineRules.IsBroken(machineHealth[i])) n++; return n; } }
        readonly List<Barista> players = new List<Barista>(4); readonly List<CoffeeCup> cups = new List<CoffeeCup>(16);
        readonly List<Machine> machines = new List<Machine>(4);
        readonly float[] machineHealth = new float[4];
        readonly float[] machineTimers = new float[4];
        readonly int[] machineBacklog = new int[4];
        static readonly Vector3[] machinePositions = { new Vector3(-2.5f, 0f, 1.8f), new Vector3(-.8f, 0f, 1.8f), new Vector3(.9f, 0f, 1.8f), new Vector3(2.6f, 0f, 1.8f) };
        TipLedger tips;
        bool initialized, bots, manualSim; System.Random random; string roast = "Get a cup movin', ya statue."; float roastTime;
        Barista human; Vector3 rack = new Vector3(-4f, 0.95f, 0f);

        void Start() { if (Application.isPlaying) Initialize(7, false); }

        public void Initialize(int seed, bool botMode) {
            if (!Application.isPlaying || initialized) return;
            CoreInit(seed, botMode, simMode: false);
        }

        // Sim path: no Application.isPlaying requirement; used by the headless
        // /sim harness (isolated runtime) and identical to the play path.
        public void InitializeSim(int seed, bool botMode) {
            if (initialized) return;
            CoreInit(seed, botMode, simMode: true);
        }

        void CoreInit(int seed, bool botMode, bool simMode) {
            if (!ValidatePrefabs()) return;
            initialized = true; bots = botMode; manualSim = simMode; random = new System.Random(seed);
            tips = new TipLedger(4);
            for (int i = 0; i < machineHealth.Length; i++) machineHealth[i] = 1f;
            BuildShop(); BuildMachines(); BuildActors(); BuildCupPool();
            Roast("Big Vinny: Ya got hands. Use 'em.");
        }

        bool ValidatePrefabs() {
            if (cupPrefab && baristaPrefab && customerPrefab && counterPrefab && floorPrefab && wallPrefab && trayPrefab && signPrefab &&
                espressoPrefab && grinderPrefab && steamWandPrefab && iceMachinePrefab) return true;
            Debug.LogError("KWA FEE CoreGame needs every Blender prefab assigned: cup, barista, customer, counter, floor, wall, tray, sign, espresso, grinder, steamWand, iceMachine.", this); return false;
        }

        GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation) { return Instantiate(prefab, position, rotation, transform); }
        void BuildShop() {
            GameObject floor = Spawn(floorPrefab, Vector3.zero, Quaternion.identity); AddMeshCollider(floor, false);
            Spawn(counterPrefab, new Vector3(-4, 0f, 0), Quaternion.identity);
            GameObject tray = Spawn(trayPrefab, new Vector3(0, 0.9f, 3.5f), Quaternion.identity); AddMeshCollider(tray, false); tray.AddComponent<ServeZone>().Configure(this);
            Spawn(customerPrefab, new Vector3(0, 0f, 4.6f), Quaternion.identity); Spawn(signPrefab, new Vector3(0, 0.55f, 5.8f), Quaternion.identity);
            MakeWall(new Vector3(0, 0, 6), Quaternion.identity); MakeWall(new Vector3(0, 0, -6), Quaternion.identity);
            MakeWall(new Vector3(-8, 0, 0), Quaternion.Euler(0, 90, 0)); MakeWall(new Vector3(8, 0, 0), Quaternion.Euler(0, 90, 0));
        }
        void MakeWall(Vector3 position, Quaternion rotation) { GameObject wall = Spawn(wallPrefab, position, rotation); AddMeshCollider(wall, false); }
        static void AddMeshCollider(GameObject item, bool convex) {
            MeshCollider collider = item.GetComponent<MeshCollider>(); if (collider == null) collider = item.AddComponent<MeshCollider>();
            if (collider.sharedMesh == null) { MeshFilter mesh = item.GetComponentInChildren<MeshFilter>(); if (mesh != null) collider.sharedMesh = mesh.sharedMesh; }
            collider.convex = convex;
        }
        void BuildMachines() {
            GameObject[] prefabs = { espressoPrefab, grinderPrefab, steamWandPrefab, iceMachinePrefab };
            for (int i = 0; i < machinePositions.Length; i++) {
                GameObject item = Spawn(prefabs[i], machinePositions[i], Quaternion.identity); AddMeshCollider(item, false);
                Machine machine = item.GetComponent<Machine>(); if (machine == null) machine = item.AddComponent<Machine>();
                machine.Configure(this, i); machines.Add(machine);
            }
        }
        void BuildActors() {
            for (int i = 0; i < 4; i++) { GameObject item = Spawn(baristaPrefab, new Vector3(-3 + i * 2, 0f, -3), Quaternion.Euler(0, 180, 0)); if (item.GetComponent<Collider>() == null) { CapsuleCollider capsule = item.AddComponent<CapsuleCollider>(); capsule.center = Vector3.up * .9f; capsule.height = 1.8f; capsule.radius = .35f; } if (item.GetComponent<Rigidbody>() == null) { Rigidbody rb = item.AddComponent<Rigidbody>(); rb.mass = 70f; rb.constraints = RigidbodyConstraints.FreezeRotation; } Barista p = item.GetComponent<Barista>(); if (p == null) p = item.AddComponent<Barista>(); p.Configure(this, i); players.Add(p); } human = players[0];
        }
        void BuildCupPool() {
            for (int i = 0; i < 16; i++) { GameObject item = Spawn(cupPrefab, PoolPosition, Quaternion.identity); if (item.GetComponent<Rigidbody>() == null) { Rigidbody rb = item.AddComponent<Rigidbody>(); rb.mass = .35f; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; } CoffeeCup cup = item.GetComponent<CoffeeCup>(); if (cup == null) cup = item.AddComponent<CoffeeCup>(); cup.Configure(this); cups.Add(cup); }
        }
        void Update() {
            if (!initialized || !Application.isPlaying || human == null) return;
            Keyboard key = Keyboard.current; Mouse mouse = Mouse.current; if (key == null || mouse == null) return;
            human.Move(new Vector2((key.dKey.isPressed ? 1 : 0) - (key.aKey.isPressed ? 1 : 0), (key.wKey.isPressed ? 1 : 0) - (key.sKey.isPressed ? 1 : 0)));
            Ray ray = Camera.main != null ? Camera.main.ScreenPointToRay(mouse.position.ReadValue()) : new Ray(human.transform.position, human.transform.forward);
            if (Mathf.Abs(ray.direction.y) > .001f) { float t = -ray.origin.y / ray.direction.y; if (t > 0) human.Aim(ray.GetPoint(t)); }
            if (key.eKey.wasPressedThisFrame) {
                if ((human.transform.position - rack).sqrMagnitude < 3.2f) {
                    if (human.HeldCup == null) { if (!GrabOrSpawnRackCup(human)) Roast("Big Vinny: Pool's dry, ya animal."); }
                    else Roast("Big Vinny: Hands full, kehd.");
                } else human.TryGrab();
            }
            if (key.rKey.wasPressedThisFrame) human.Drop();
            if (key.spaceKey.isPressed) human.BeginChug(); else human.EndChug();
            if (key.lKey.wasPressedThisFrame) human.FixNearest();
            if (key.pKey.wasPressedThisFrame) human.SabotageNearest();
            if (key.qKey.wasPressedThisFrame) human.Steal();
            if (mouse.leftButton.wasPressedThisFrame) human.BeginCharge(); if (mouse.leftButton.wasReleasedThisFrame) human.ReleaseCharge();
        }
        // Game logic steps with the SAME dt as the physics step. In play the
        // physics engine auto-simulates at Time.fixedDeltaTime, so this uses
        // that value; the /sim harness drives both via Simulate().
        void FixedUpdate() { if (initialized && !manualSim && Application.isPlaying) Step(Time.fixedDeltaTime); }
        public void Step(float dt) {
            if (!initialized || dt <= 0f) return;
            for (int i = 0; i < machineHealth.Length; i++) {
                machineHealth[i] = MachineRules.Degrade(machineHealth[i], dt);
                if (!MachineRules.IsBroken(machineHealth[i])) {
                    machineTimers[i] += dt;
                    float delay = MachineRules.ProductionDelay(machineHealth[i]);
                    while (machineTimers[i] >= delay) { machineTimers[i] -= delay; TryProduce(i); }
                }
            }
            for (int i = 0; i < players.Count; i++) { if (bots && i > 0 && !manualSim) BotMove(players[i]); players[i].Tick(dt); }
            for (int i = 0; i < cups.Count; i++) { CoffeeCup cup = cups[i]; cup.Tick(dt); if (cup.State == CupState.Served && cup.Age > 1.1f) cup.ResetToPool(PoolPosition); else if (cup.State == CupState.Airborne && !cup.HitPlayer && (cup.Age > 8f || cup.transform.position.y < -2f || Mathf.Abs(cup.transform.position.x) > 10f || Mathf.Abs(cup.transform.position.z) > 8f)) { Misses++; cup.ResetToPool(PoolPosition); Roast("Big Vinny: That coffee took the T outta town."); } }
            if (roastTime > 0) roastTime -= dt;
        }
        public void Tick(float dt) { Step(dt); }
        // Simulate drives the real engine physics scene, stepping game logic
        // and physics with the SAME dt. Sets manualSim so FixedUpdate cannot
        // double-step during a manual pass.
        public void Simulate(UnityEngine.PhysicsScene scene, float dt) { if (initialized && dt > 0f) { manualSim = true; Step(dt); scene.Simulate(dt); } }
        void BotMove(Barista p) { p.Move(new Vector2((float)random.NextDouble() * 2 - 1, (float)random.NextDouble() * 2 - 1)); p.Aim(new Vector3(0, 0, 3.5f)); }
        CoffeeCup FindRackCup() { for (int i = 0; i < cups.Count; i++) if (cups[i].State == CupState.Rack) return cups[i]; return null; }
        CoffeeCup FindPoolCup() { for (int i = 0; i < cups.Count; i++) if (cups[i].State == CupState.Pool) return cups[i]; return null; }
        void TryProduce(int machineIndex) {
            // Every cup a machine pushes out wears it: use-degrade plus the
            // passive drain makes breakage a real cascade under heavy traffic.
            machineHealth[machineIndex] = Mathf.Max(0f, machineHealth[machineIndex] - MachineRules.UseDegradePerCup);
            if (FindRackCup() == null) { CoffeeCup cup = FindPoolCup(); if (cup != null) { cup.PutOnRack(rack); return; } }
            machineBacklog[machineIndex] = Mathf.Min(3, machineBacklog[machineIndex] + 1);
        }
        internal bool GrabOrSpawnRackCup(Barista holder) {
            CoffeeCup cup = FindRackCup();
            if (cup != null) { holder.Take(cup); Roast("Big Vinny: Rack's got one. Don't baptize it."); return true; }
            for (int i = 0; i < machineBacklog.Length; i++) if (machineBacklog[i] > 0) {
                machineBacklog[i]--; cup = FindPoolCup(); if (cup == null) return false;
                cup.PutOnRack(rack); holder.Take(cup); Roast("Big Vinny: Rack's got one. Don't baptize it."); return true;
            }
            return false;
        }
        internal void TryGrab(Barista holder) {
            CoffeeCup best = null; float bestDistance = 1.2f * 1.2f;
            for (int i = 0; i < cups.Count; i++) { CoffeeCup cup = cups[i]; if (cup.State == CupState.Pool || cup.State == CupState.Served || cup.State == CupState.Held) continue; float d = (cup.transform.position - holder.transform.position).sqrMagnitude; if (d < bestDistance) { best = cup; bestDistance = d; } }
            if (best != null) { if (best.State == CupState.Airborne) Catches++; holder.Take(best); Roast("Big Vinny: Hands work. Who knew?"); }
        }
        internal void RecordShot() { Shots++; ChaosEvents++; Roast("Big Vinny: Incoming, ya maniac."); }
        internal void RecordVerb(VerbKind verb) { if (verb == VerbKind.Chug) { Chugs++; ChaosEvents++; Roast("Big Vinny: Drinkin' coffee like it owes ya money."); } }
        internal void TriggerOverdose(Barista player) { Overdoses++; ChaosEvents++; player.Overdose(); Roast("Big Vinny: The jitters got a BODY COUNT, kehd."); }
        internal Vector3 SpinForCup() {
            Vector3 spin = new Vector3((float)random.NextDouble() * 2f - 1f, (float)random.NextDouble() * 2f - 1f, (float)random.NextDouble() * 2f - 1f);
            return spin.sqrMagnitude < .01f ? Vector3.up : spin.normalized;
        }
        internal void TryServe(CoffeeCup cup) {
            if (cup.State != CupState.Airborne || !cup.WasAirborne || cup.OwnerId < 0 || cup.Liquid < .2f) return;
            cup.MarkServed(); Serves++; tips.AddServe(cup.OwnerId, FlingRules.TipsForServe(cup.Liquid)); ChaosEvents++; Roast("Big Vinny: Customer survived. Barely.");
        }
        internal void TryCupHit(CoffeeCup cup, Vector3 point, bool ownerGrace) {
            if (ownerGrace) return;
            for (int i = 0; i < players.Count; i++) if (players[i].Id != cup.OwnerId && players[i].IsTargetAt(point)) { players[i].Stun(); cup.Spill(.5f); cup.HitPlayer = true; Hits++; ChaosEvents++; Roast("Big Vinny: Ya beaned a coworker. Payroll loves it."); return; }
        }
        internal void TryFix(Barista actor) {
            int index = NearestMachine(actor.transform.position);
            if (index < 0) { Roast("Big Vinny: The wrench ain't psychic, kehd."); return; }
            machineHealth[index] = VerbRules.Repair(machineHealth[index]); Fixes++; Roast("Big Vinny: Fixed it. Try breakin' it slower.");
        }
        internal void TrySabotage(Barista actor) {
            int index = NearestMachine(actor.transform.position);
            if (index < 0) { Roast("Big Vinny: No machine, no mischief. Walk two steps."); return; }
            machineHealth[index] = VerbRules.Sabotage(machineHealth[index]); Sabotages++; ChaosEvents++; Roast("Big Vinny: That's gonna be somebody else's problem.");
        }
        internal void TrySteal(Barista actor) {
            Barista target = null; CoffeeCup cup = null; float best = 1.6f * 1.6f;
            for (int i = 0; i < players.Count; i++) {
                Barista candidate = players[i]; if (candidate == actor || candidate.HeldCup == null) continue;
                float d = (candidate.transform.position - actor.transform.position).sqrMagnitude;
                if (d < best && VerbRules.CanSteal(true, actor.HeldCup != null, Mathf.Sqrt(d))) { target = candidate; cup = candidate.HeldCup; best = d; }
            }
            if (target == null) { Roast("Big Vinny: Nobody's holdin' anything worth stealin'. Yet."); return; }
            target.Relinquish(); actor.Take(cup); Steals++; ChaosEvents++; Roast("Big Vinny: Theft of the night, and it's only Tuesday.");
        }
        int NearestMachine(Vector3 position) {
            int best = -1; float distance = 2.25f * 2.25f;
            for (int i = 0; i < machinePositions.Length; i++) { float d = (position - machinePositions[i]).sqrMagnitude; if (d < distance) { distance = d; best = i; } }
            return best;
        }
        internal void Roast(string line) { roast = line; roastTime = 3.5f; }
        void OnGUI() {
            if (!initialized || !Application.isPlaying) return;
            GUI.color = new Color(1f, .82f, .22f); GUI.Label(new Rect(18, 16, 600, 32), "KWA FEE", GUI.skin.box);
            GUI.color = Color.white; GUI.Label(new Rect(18, 52, 900, 24), "WASD move  •  mouse aim  •  E rack/grab  •  hold click FLING  •  SPACE CHUG  •  L FIX  •  Q STEAL  •  P SABOTAGE  •  R drop");
            float charge = human != null ? human.Charge01 : 0; GUI.Box(new Rect(18, 84, 180, 18), ""); GUI.color = new Color(1f, .3f, .35f); GUI.Box(new Rect(18, 84, 180 * charge, 18), "CHARGE");
            GUI.color = Color.white; GUI.Label(new Rect(18, 110, 700, 24), "ORDER: one hot, less floor.  SERVED " + Serves + "  TIPS $" + TotalTips + "  HITS " + Hits + "  CHAOS " + ChaosEvents);
            GUI.Label(new Rect(18, 136, 900, 24), "MACHINES  E" + MachineLabel(0) + "  G" + MachineLabel(1) + "  W" + MachineLabel(2) + "  I" + MachineLabel(3) + "   CHUGS " + Chugs + "  STEALS " + Steals + "  SABOTAGE " + Sabotages);
            if (human != null) GUI.Label(new Rect(18, 162, 500, 24), (human.transform.position - rack).sqrMagnitude < 3.2f ? "E: grab a cup off the rack" : "E: grab a nearby cup");
            if (roastTime > 0) { GUI.color = new Color(.4f, 1f, .9f); GUI.Label(new Rect(18, 196, 800, 28), roast); }
        }
        string MachineLabel(int index) {
            bool broken = MachineRules.IsBroken(machineHealth[index]);
            return Mathf.RoundToInt(machineHealth[index] * 100) + "%" + (broken ? "(OUT)" : "");
        }
    }
}
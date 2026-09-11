using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KwaFee {
    // Scene-side adapter for the Shift seam. Owns prefab refs, builds the shop,
    // implements ShiftWorld over the real MonoBehaviours, translates keyboard/
    // mouse/bot/sim intent into Shift.Command calls, and renders the HUD from
    // Shift.Metrics(). No gameplay rule lives here — every verdict lives in
    // Shift and every kinematic write is a direct ShiftWorld effect.
    public sealed class CoreGame : MonoBehaviour, ShiftWorld {
        [Header("Blender prefabs")]
        public GameObject cupPrefab, baristaPrefab, customerPrefab, counterPrefab, floorPrefab, wallPrefab, trayPrefab, signPrefab;
        public GameObject espressoPrefab, grinderPrefab, steamWandPrefab, iceMachinePrefab;
        public float MoveSpeed = 5f;
        public bool Initialized { get { return initialized; } }
        public IReadOnlyList<Barista> Players => players;
        public IReadOnlyList<CoffeeCup> Cups => cups;
        public ShiftMetrics Metrics() { return shift != null ? shift.Metrics() : null; }
        // Sole entry for gameplay intent from every actor (keyboard, bot, sim).
        public void Command(int actor, ShiftVerb verb, ShiftArgs args) { if (shift != null) shift.Command(actor, verb, args); }

        readonly List<Barista> players = new List<Barista>(4);
        readonly List<CoffeeCup> cups = new List<CoffeeCup>(16);
        readonly List<Machine> machines = new List<Machine>(4);
        readonly List<GameObject> customers = new List<GameObject>(Shift.MaxOrders);
        Shift shift;
        bool initialized, manualSim;
        Barista human;
        public Vector3 PoolPosition => transform.position + Vector3.down * 10f;

        void Start() { if (Application.isPlaying) Initialize(7); }

        // Play path: only inside a running game.
        public void Initialize(int seed) {
            if (!Application.isPlaying || initialized) return;
            CoreInit(seed, simMode: false);
        }

        // Sim binding: adopt the prefab assignments of a scene-placed CoreGame
        // so a fresh harness root can build the same world without the harness
        // knowing the prefab field names (the seam binds through InitializeSim).
        public void AdoptPrefabs(CoreGame donor) {
            if (donor == null) return;
            cupPrefab = donor.cupPrefab; baristaPrefab = donor.baristaPrefab;
            customerPrefab = donor.customerPrefab; counterPrefab = donor.counterPrefab;
            floorPrefab = donor.floorPrefab; wallPrefab = donor.wallPrefab;
            trayPrefab = donor.trayPrefab; signPrefab = donor.signPrefab;
            espressoPrefab = donor.espressoPrefab; grinderPrefab = donor.grinderPrefab;
            steamWandPrefab = donor.steamWandPrefab; iceMachinePrefab = donor.iceMachinePrefab;
        }

        // Sim path: no Application.isPlaying requirement; used by the headless
        // /sim harness (in-editor) and it binds exactly like play.
        public void InitializeSim(int seed) {
            if (initialized) return;
            CoreInit(seed, simMode: true);
        }

        void CoreInit(int seed, bool simMode) {
            if (!ValidatePrefabs()) return;
            manualSim = simMode;
            shift = new Shift(this);
            shift.MoveSpeed = MoveSpeed;
            BuildShop(); BuildMachines(); BuildActors(); BuildCupPool(); BuildOrderBoard();
            shift.Begin(seed, Shift.PlayerCount);
            // The Shift's own world-facing reset already puts every cup back in
            // the pool and every barista on its pad; scene builds above match.
            initialized = true;
        }

        bool ValidatePrefabs() {
            if (cupPrefab && baristaPrefab && customerPrefab && counterPrefab && floorPrefab && wallPrefab && trayPrefab && signPrefab &&
                espressoPrefab && grinderPrefab && steamWandPrefab && iceMachinePrefab) return true;
            Debug.LogError("KWA FEE CoreGame needs every Blender prefab assigned: cup, barista, customer, counter, floor, wall, tray, sign, espresso, grinder, steamWand, iceMachine.", this); return false;
        }

        // ---------------- scene authorship ----------------

        GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation) { return Instantiate(prefab, position, rotation, transform); }

        void BuildShop() {
            GameObject floor = Spawn(floorPrefab, Vector3.zero, Quaternion.identity); AddMeshCollider(floor, false);
            Spawn(counterPrefab, new Vector3(-4, 0f, 0), Quaternion.identity);
            GameObject tray = Spawn(trayPrefab, new Vector3(0, 0.9f, 3.5f), Quaternion.identity); AddMeshCollider(tray, false);
            tray.AddComponent<ServeZone>().Configure(this);
            Spawn(signPrefab, new Vector3(0, 0.55f, 5.8f), Quaternion.identity);
            MakeWall(new Vector3(0, 0, 6), Quaternion.identity); MakeWall(new Vector3(0, 0, -6), Quaternion.identity);
            MakeWall(new Vector3(-8, 0, 0), Quaternion.Euler(0, 90, 0)); MakeWall(new Vector3(8, 0, 0), Quaternion.Euler(0, 90, 0));
        }
        void MakeWall(Vector3 position, Quaternion rotation) { GameObject wall = Spawn(wallPrefab, position, rotation); AddMeshCollider(wall, false); }
        static void AddMeshCollider(GameObject item, bool convex) {
            MeshCollider collider = item.GetComponent<MeshCollider>();
            if (collider == null) collider = item.AddComponent<MeshCollider>();
            if (collider.sharedMesh == null) {
                MeshFilter mesh = item.GetComponentInChildren<MeshFilter>();
                if (mesh != null) collider.sharedMesh = mesh.sharedMesh;
            }
            collider.convex = convex;
        }
        void BuildMachines() {
            GameObject[] prefabs = { espressoPrefab, grinderPrefab, steamWandPrefab, iceMachinePrefab };
            for (int i = 0; i < Shift.MachinePositions.Length; i++) {
                GameObject item = Spawn(prefabs[i], Shift.MachinePositions[i], Quaternion.identity); AddMeshCollider(item, false);
                Machine machine = item.GetComponent<Machine>();
                if (machine == null) machine = item.AddComponent<Machine>();
                machine.Configure(i);
                machines.Add(machine);
            }
        }
        void BuildActors() {
            for (int i = 0; i < Shift.PlayerCount; i++) {
                GameObject item = Spawn(baristaPrefab, new Vector3(-3 + i * 2, 0f, -3), Quaternion.Euler(0, 180, 0));
                if (item.GetComponent<Collider>() == null) {
                    CapsuleCollider capsule = item.AddComponent<CapsuleCollider>();
                    capsule.center = Vector3.up * .9f; capsule.height = 1.8f; capsule.radius = .35f;
                }
                if (item.GetComponent<Rigidbody>() == null) {
                    Rigidbody rb = item.AddComponent<Rigidbody>();
                    rb.mass = 70f; rb.constraints = RigidbodyConstraints.FreezeRotation;
                }
                Barista p = item.GetComponent<Barista>();
                if (p == null) p = item.AddComponent<Barista>();
                p.Configure(i);
                players.Add(p);
            }
            human = players[0];
        }
        void BuildCupPool() {
            for (int i = 0; i < Shift.CupCount; i++) {
                GameObject item = Spawn(cupPrefab, PoolPosition, Quaternion.identity);
                if (item.GetComponent<Rigidbody>() == null) {
                    Rigidbody rb = item.AddComponent<Rigidbody>();
                    rb.mass = .35f; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                }
                CoffeeCup cup = item.GetComponent<CoffeeCup>();
                if (cup == null) cup = item.AddComponent<CoffeeCup>();
                cup.Configure(this, i);
                cups.Add(cup);
            }
        }
        void BuildOrderBoard() {
            for (int i = 0; i < Shift.MaxOrders; i++) {
                GameObject customer = Spawn(customerPrefab, new Vector3(0, -10f, 0), Quaternion.Euler(0, 180, 0));
                customers.Add(customer);
            }
        }

        // ---------------- ShiftWorld: kinematic/visual surface ----------------

        // A cup leaving a barista's logical hold (throw/drop/steal/reset) must
        // clear every MonoBehaviour ref to it: the victim of a steal and the
        // thrower both keep HeldCup otherwise, and every held cup would be
        // dragged by two FollowHand calls. Shift owns logical ownership; this
        // keeps the scene mirror exact.
        void ClearHeldRef(int cup) {
            CoffeeCup cupObject = cups[cup];
            for (int i = 0; i < players.Count; i++)
                if (players[i].HeldCup != null && players[i].HeldCup == cupObject) players[i].Relinquish();
        }

        public Vector3 PlayerPosition(int actor) { return players[actor].transform.position; }
        public Vector3 PlayerVelocity(int actor) { return players[actor].VelocityRef; }
        public Vector3 CupPosition(int cup) { return cups[cup].transform.position; }
        public float CupUpDot(int cup) { return cups[cup].transform.up.y; }

        public void DrivePlayer(int actor, Vector3 xzVelocity, Vector3 aim) {
            Barista p = players[actor];
            Rigidbody body = p.GetComponent<Rigidbody>();
            body.linearVelocity = new Vector3(xzVelocity.x, body.linearVelocity.y, xzVelocity.z);
            body.WakeUp(); // Unity autosleep swallows velocity writes on sleeping bodies
            if (aim.sqrMagnitude > .01f) p.transform.rotation = Quaternion.LookRotation(aim, Vector3.up);
            if (p.HeldCup != null) p.HeldCup.FollowHand(p.HandFollowTarget(aim), p.transform.rotation);
        }

        public void FreezePlayer(int actor) {
            Rigidbody body = players[actor].GetComponent<Rigidbody>();
            // Old Barista.Tick stunned with a full-zero write and no wake;
            // keep that exact kinematic so frozen actors don't drift.
            body.linearVelocity = Vector3.zero;
        }

        public void HoldCup(int actor, int cup) {
            ClearHeldRef(cup);
            players[actor].Take(cups[cup]);
        }
        public void ThrowCup(int cup, Vector3 velocity, Vector3 spinVelocity) {
            ClearHeldRef(cup);
            cups[cup].Throw(velocity, spinVelocity);
        }
        public void DropCup(int cup, Vector3 velocity) {
            ClearHeldRef(cup);
            cups[cup].Drop(velocity);
        }
        public void PutOnRack(int cup) { cups[cup].PutOnRack(Shift.RackPosition); }
        public void ResetCupToPool(int cup) {
            ClearHeldRef(cup);
            cups[cup].ResetToPool(PoolPosition);
        }
        public void MarkCupServed(int cup) { cups[cup].MarkServed(); }
        public void RespawnPlayer(int actor) { players[actor].Respawn(); }
        public void SpawnCustomer(int slot) { customers[slot].transform.position = Shift.SlotPositions[slot]; }
        public void HideCustomer(int slot) { customers[slot].transform.position = new Vector3(0, -10f, 0); }

        // ---------------- physics callback routing into Shift ----------------

        public void OnServeZone(int cupId) { if (shift != null) shift.OnServe(cupId); }
        public void OnCupCollision(int cupId, Vector3 point, float relativeSpeed) { if (shift != null) shift.OnCupHit(cupId, point, relativeSpeed); }

        // ---------------- fixed-sim stepping ----------------

        void FixedUpdate() { if (initialized && !manualSim && Application.isPlaying) Step(Time.fixedDeltaTime); }

        // Play path and sim path both run the same Shift.Step; the sim adapter
        // owns physics and calls Step then Simulate in lockstep.
        public void Step(float dt) {
            if (!initialized || dt <= 0f) return;
            shift.Step(dt);
            for (int i = 0; i < cups.Count; i++) cups[i].TickVisual();
        }

        public void Simulate(UnityEngine.PhysicsScene scene, float dt) {
            if (!initialized || dt <= 0f) return;
            manualSim = true;
            Step(dt);
            scene.Simulate(dt);
        }

        // ---------------- human input adapter ----------------

        void Update() {
            if (!initialized || !Application.isPlaying || human == null) return;
            ShiftMetrics m = shift.Metrics();
            if (m.ShiftOver) {
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) RestartShift();
                return;
            }
            Keyboard key = Keyboard.current; Mouse mouse = Mouse.current;
            if (key == null || mouse == null) return;
            shift.Command(0, ShiftVerb.Move, ShiftArgs.Move(new Vector2(
                (key.dKey.isPressed ? 1 : 0) - (key.aKey.isPressed ? 1 : 0),
                (key.wKey.isPressed ? 1 : 0) - (key.sKey.isPressed ? 1 : 0))));
            Ray ray = Camera.main != null ? Camera.main.ScreenPointToRay(mouse.position.ReadValue()) : new Ray(human.transform.position, human.transform.forward);
            if (Mathf.Abs(ray.direction.y) > .001f) {
                float t = -ray.origin.y / ray.direction.y;
                if (t > 0) shift.Command(0, ShiftVerb.Aim, ShiftArgs.Aim(ray.GetPoint(t)));
            }
            if (key.eKey.wasPressedThisFrame) shift.Command(0, ShiftVerb.Grab, ShiftArgs.None());
            if (key.rKey.wasPressedThisFrame) shift.Command(0, ShiftVerb.Drop, ShiftArgs.None());
            if (key.spaceKey.isPressed) shift.Command(0, ShiftVerb.ChugBegin, ShiftArgs.None());
            else shift.Command(0, ShiftVerb.ChugEnd, ShiftArgs.None());
            if (key.lKey.wasPressedThisFrame) shift.Command(0, ShiftVerb.Fix, ShiftArgs.None());
            if (key.pKey.wasPressedThisFrame) shift.Command(0, ShiftVerb.Sabotage, ShiftArgs.None());
            if (key.qKey.wasPressedThisFrame) shift.Command(0, ShiftVerb.Steal, ShiftArgs.None());
            if (mouse.leftButton.wasPressedThisFrame) shift.Command(0, ShiftVerb.FlingBegin, ShiftArgs.None());
            if (mouse.leftButton.wasReleasedThisFrame) shift.Command(0, ShiftVerb.FlingRelease, ShiftArgs.None());
        }

        public void RestartShift() { if (shift != null) shift.RestartShift(); }

        // ---------------- HUD: reads Metrics only ----------------

        void OnGUI() {
            if (!initialized || !Application.isPlaying || shift == null) return;
            ShiftMetrics m = shift.Metrics();
            GUI.color = new Color(1f, .82f, .22f); GUI.Label(new Rect(18, 16, 320, 32), "KWA FEE", GUI.skin.box);
            GUI.color = Color.white;
            GUI.Label(new Rect(360, 16, 300, 24), "TIME " + Mathf.CeilToInt(Mathf.Max(0f, RoundRules.ShiftLength - m.ShiftElapsed)) + "s  QUOTA " + m.Serves + "/" + RoundRules.QuotaTarget);
            GUI.Label(new Rect(18, 52, 900, 24), "WASD move  •  mouse aim  •  E rack/grab  •  hold click FLING  •  SPACE CHUG  •  L FIX  •  Q STEAL  •  P SABOTAGE  •  R drop");
            float charge = m.Charge01[0]; GUI.Box(new Rect(18, 84, 180, 18), ""); GUI.color = new Color(1f, .3f, .35f); GUI.Box(new Rect(18, 84, 180 * charge, 18), "CHARGE");
            GUI.color = Color.white;
            int line = 110;
            for (int i = 0; i < m.Orders.Length; i++) {
                ShiftOrderView order = m.Orders[i];
                if (!order.Active) continue;
                GUI.color = Color.Lerp(new Color(1f, .2f, .2f), new Color(.3f, 1f, .5f), Mathf.Clamp01(order.Patience / RoundRules.CustomerPatience));
                GUI.Label(new Rect(18, line, 900, 22), "ORDER " + (order.Slot + 1) + ": " + order.Demand + "  (" + Mathf.CeilToInt(order.Patience) + "s)");
                line += 24;
            }
            GUI.color = Color.white; GUI.Label(new Rect(18, line, 700, 24), "TIPS $" + m.TotalTips + "  HITS " + m.Hits + "  CHAOS " + m.ChaosEvents + "  MACHINES  E" + MachineLabel(m, 0) + "  G" + MachineLabel(m, 1) + "  W" + MachineLabel(m, 2) + "  I" + MachineLabel(m, 3));
            line += 24; GUI.Label(new Rect(18, line, 700, 24), "CHUGS " + m.Chugs + "  STEALS " + m.Steals + "  SABOTAGE " + m.Sabotages + "  EXPIRED " + m.ExpiredOrders + (m.FixturesLost > 0 ? "  BIG TONY TOOK ONE" : ""));
            if (m.ShiftOver) { GUI.color = new Color(1f, .9f, .3f); GUI.Label(new Rect(18, line + 30, 900, 40), m.RoundSummary); GUI.Label(new Rect(18, line + 56, 900, 24), "Press R for a fresh shift."); }
            if (m.RoastTime > 0) { GUI.color = new Color(.4f, 1f, .9f); GUI.Label(new Rect(18, 196, 800, 28), m.Roast); }
        }
        static string MachineLabel(ShiftMetrics m, int index) {
            bool broken = MachineRules.IsBroken(m.MachineHealth[index]);
            return Mathf.RoundToInt(m.MachineHealth[index] * 100) + "%" + (broken ? "(OUT)" : "");
        }
    }
}
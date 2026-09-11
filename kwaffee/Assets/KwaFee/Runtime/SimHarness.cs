using System.IO;
using UnityEngine;

namespace KwaFee {
    // Scripted 4-bot simulation harness. Drives the SAME CoreGame Step() and
    // the REAL Unity engine physics (Physics.defaultPhysicsScene.Simulate) at
    // the fixed 50 Hz engine step (TimeManager: 0.02 s), using scripted
    // behavior policies per bot. Produces sim-report.txt metrics for artifact
    // gating: chaos/min, serves, per-player tips, hits, misses, chug/overdose
    // tallies, machine health.
    //
    // Determinism: CoreGame.Simulate sets manualSim so FixedUpdate no-ops; the
    // harness calls scene.Simulate(dt) in lockstep with CoreGame.Step(dt).
    // System.Random is seeded in CoreGame and the harness.
    public sealed class SimHarness {
        // Bumped so the editor log proves WHICH class version executed.
        public static readonly string Build = "v5-waypoint";
        CoreGame game;
        readonly float fixedDt = 0.02f; // TimeManager: Fixed Timestep 0.02 (50 Hz)
        public float simSeconds;
        public string report;
        readonly bool[] scriptedFired = new bool[4];
        readonly int[] heldCycles = new int[4];
        readonly int[] chargeCalls = new int[4];
        readonly int[] releaseCalls = new int[4];
        readonly Vector3[] legA = new Vector3[4]; // approach waypoint south of counter
        readonly Vector3[] fireLine = new Vector3[4];

        static readonly Vector3 rackPoint = new Vector3(-4f, 1.05f, -0.6f);
        static readonly Vector3 trayPoint = new Vector3(0f, 0.9f, 3.5f);
        static readonly Vector3 rackApproach = new Vector3(-4f, 0f, -1.6f); // open corridor

        public void Run(int seed, float seconds) {
            var root = new GameObject("SimCoreGame");
            game = root.AddComponent<CoreGame>();
            // The prefab refs live on the scene's CoreGame; copy them onto the
            // fresh sim instance so ValidatePrefabs passes in edit mode.
            var all = Object.FindObjectsByType<CoreGame>(FindObjectsInactive.Include);
            for (var i = 0; i < all.Length; i++) if (all[i].cupPrefab != null) {
                game.cupPrefab = all[i].cupPrefab; game.baristaPrefab = all[i].baristaPrefab;
                game.customerPrefab = all[i].customerPrefab; game.counterPrefab = all[i].counterPrefab;
                game.floorPrefab = all[i].floorPrefab; game.wallPrefab = all[i].wallPrefab;
                game.trayPrefab = all[i].trayPrefab; game.signPrefab = all[i].signPrefab;
                game.espressoPrefab = all[i].espressoPrefab; game.grinderPrefab = all[i].grinderPrefab;
                game.steamWandPrefab = all[i].steamWandPrefab; game.iceMachinePrefab = all[i].iceMachinePrefab;
                break;
            }
            game.InitializeSim(seed, botMode: true);
            for (int i = 0; i < 4; i++) {
                legA[i] = new Vector3(rackApproach.x + i * 0.6f, 0f, rackApproach.z);
                fireLine[i] = new Vector3(-1.5f + i * 1f, 0f, 0.8f);
            }
            if (!game.Initialized) { report = "KWA FEE /sim: CoreGame failed to initialize (prefabs assigned?)."; Debug.LogWarning(report); return; }
            simSeconds = seconds;
            int simSteps = Mathf.Max(1, Mathf.RoundToInt(seconds / fixedDt));
            var scene = Physics.defaultPhysicsScene;
            bool wasAuto = UnityEngine.Physics.autoSimulation;
            UnityEngine.Physics.autoSimulation = false; // harness owns the step
            try {
                for (int i = 0; i < simSteps; i++) {
                    float t = i * fixedDt;
                    for (int p = 0; p < game.Players.Count; p++) {
                        ScriptTick(game.Players[p], t);
                        if (t <= 15f && (i % 50 == 0)) Trace(game.Players[p], t);
                    }
                    game.Simulate(scene, fixedDt);
                }
            } finally {
                UnityEngine.Physics.autoSimulation = wasAuto;
            }
            report = BuildReport(seconds);
            WriteReport();
        }

        // Two-leg waypoint nav avoids wedging on the counter corner: leg A
        // south of the counter (z -1.6, clear corridor), then north to the
        // rack's front face; grab; then to the firing line.
        void ScriptTick(Barista player, float time) {
            Vector3 here = player.transform.position;
            player.Move(Toward(legA[player.Id], here));
            if (player.HeldCup == null) {
                if (Outside(Horizontal(here), legA[player.Id], 0.3f)) return; // still on leg A
                // At the approach point: walk north to the rack and grab.
                player.Aim(rackPoint);
                Vector3 toRack = rackPoint - here; toRack.y = 0f;
                player.Move(Toward(rackPoint, here));
                if (toRack.magnitude < 1.9f && player.HeldCup == null) { grabTries++; if (game.GrabOrSpawnRackCup(player)) grabSuccess++; }
                return;
            }
            // Holding: proceed to the firing line in front of the machine row.
            Vector3 fire = fireLine[player.Id];
            Vector3 toFire = fire - here; toFire.y = 0f;
            if (toFire.magnitude > 0.3f) {
                player.Aim(fire);
                player.Move(Toward(fire, here));
                return;
            }
            player.Move(Vector2.zero);
            heldCycles[player.Id]++;
            player.Aim(trayPoint);
            if ((int)(time * .5f) % 4 == 1) player.BeginChug(); else player.EndChug();
            if ((int)(time * .5f) % 4 < 3) { chargeCalls[player.Id]++; player.BeginCharge(); }
            else { releaseCalls[player.Id]++; player.ReleaseCharge(); }
            // Griefing is RARE and fires exactly once per scripted moment.
            if (!scriptedFired[0] && time > 20f) { scriptedFired[0] = true; if (player.Id == 1) player.SabotageNearest(); }
            if (!scriptedFired[1] && time > 45f) { scriptedFired[1] = true; if (player.Id == 2) player.SabotageNearest(); }
            if (!scriptedFired[2] && time > 70f) { scriptedFired[2] = true; if (player.Id == 3) player.Steal(); }
        }

        int grabTries;
        int grabSuccess;

        void Trace(Barista player, float t) {
            var pos = player.transform.position;
            UnityEngine.Debug.Log("KWA TRACE t=" + string.Format("{0:0.00}", t) + " p" + player.Id + " pos=" + string.Format("{0:0.0}/{1:0.0}", pos.x, pos.z) +
                " held=" + (player.HeldCup != null) + " stun=" + string.Format("{0:0.0}", player.StunTime) + " vel=" + string.Format("{0:0.0}/{1:0.0}", player.VelocityRef.x, player.VelocityRef.z));
        }

        Vector2 Toward(Vector3 target, Vector3 from) { Vector3 d = target - from; d.y = 0f; return new Vector2(Mathf.Clamp(d.x, -1f, 1f), Mathf.Clamp(d.z, -1f, 1f)); }
        bool Outside(Vector3 here, Vector3 target, float radius) { Vector3 d = target - here; return (d.x * d.x + d.z * d.z) > radius * radius; }
        Vector3 Horizontal(Vector3 v) { return new Vector3(v.x, 0f, v.z); }

        string BuildReport(float seconds) {
            int[] playerTips = new int[4];
            for (int i = 0; i < game.Players.Count; i++) playerTips[i] = game.PlayerTips(i);
            float chaosPerMin = game.ChaosEvents / Mathf.Max(1f, seconds / 60f);
            return "KWA FEE /sim report\n" +
                "seconds=" + seconds + " steps=" + Mathf.RoundToInt(seconds / fixedDt) + " dt=" + fixedDt + " physics=engine-50Hz build=" + Build + "\n" +
                "chaos_per_min=" + Stringify(chaosPerMin) + " chaos_total=" + game.ChaosEvents + "\n" +
                "serves=" + game.Serves + " tips_total=$" + game.TotalTips +
                " tips_by_player=" + playerTips[0] + "," + playerTips[1] + "," + playerTips[2] + "," + playerTips[3] + "\n" +
                "shots=" + game.Shots + " hits=" + game.Hits + " misses=" + game.Misses + " catches=" + game.Catches + "\n" +
                "chugs=" + game.Chugs + " overdoses=" + game.Overdoses + " fixes=" + game.Fixes + " sabotages=" + game.Sabotages + " steals=" + game.Steals + "\n" +
                "machine_esp=" + Stringify(game.MachineHealth(MachineKind.Espresso)) + " grinder=" + Stringify(game.MachineHealth(MachineKind.Grinder)) +
                " steam=" + Stringify(game.MachineHealth(MachineKind.SteamWand)) + " ice=" + Stringify(game.MachineHealth(MachineKind.IceMachine)) + " broken=" + game.BrokenMachines + "\n" +
                "rack_cups=" + RackCups() + " expired_orders=" + game.ExpiredOrders + " grab_tries=" + grabTries + " grab_success=" + grabSuccess + "\n" +
                "bot_positions=" + Pos(0) + "|" + Pos(1) + "|" + Pos(2) + "|" + Pos(3) + "\n" +
                "verb_attempts_held=" + heldCycles[0] + "/" + heldCycles[1] + "/" + heldCycles[2] + "/" + heldCycles[3] +
                " charge=" + chargeCalls[0] + "/" + chargeCalls[1] + "/" + chargeCalls[2] + "/" + chargeCalls[3] +
                " release=" + releaseCalls[0] + "/" + releaseCalls[1] + "/" + releaseCalls[2] + "/" + releaseCalls[3] + "\n";
        }

        string Pos(int i) {
            var t = game.Players[i].transform.position;
            return string.Format("{0:0.0},{1:0.0},{2:0.0}", t.x, t.y, t.z);
        }

        string Stringify(float v) { return string.Format("{0}", v); }

        int RackCups() {
            int n = 0; for (int i = 0; i < game.Cups.Count; i++) if (game.Cups[i].State == CupState.Rack) n++;
            return n;
        }

        void WriteReport() {
            var dirPath = Application.persistentDataPath + "/kwafee-sim";
            Directory.CreateDirectory(dirPath);
            var path = dirPath + "/sim-report.txt";
            File.WriteAllText(path, report);
            Debug.Log("KWA FEE /sim wrote " + path + "\n" + report);
        }
    }
}
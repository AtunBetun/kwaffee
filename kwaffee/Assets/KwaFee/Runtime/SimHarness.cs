using System.IO;
using UnityEngine;

namespace KwaFee {
    // Scripted 4-bot simulation harness. Drives the SAME CoreGame Step() and
    // the REAL Unity engine physics (Physics.defaultPhysicsScene.Simulate) at
    // the fixed 50 Hz engine step (TimeManager: 0.02 s), using scripted
    // behavior policies per bot. Produces sim-report.txt metrics for the
    // artifact gating: chaos events/min, serves, per-player tips, hits,
    // misses, chug/overdose tallies, machine health.
    //
    // Determinism: CoreGame.Simulate sets manualSim so CoreGame.FixedUpdate
    // no-ops; the harness calls scene.Simulate(dt) itself in lockstep with
    // CoreGame.Step(dt). System.Random is seeded in CoreGame and the harness.
    public sealed class SimHarness {
        CoreGame game;
        readonly float fixedDt = 0.02f; // TimeManager: Fixed Timestep 0.02 (50 Hz)
        public float simSeconds;
        public string report;

        public void Run(int seed, float seconds) {
            var root = new GameObject("SimCoreGame");
            game = root.AddComponent<CoreGame>();
            game.InitializeSim(seed, botMode: true);
            if (!game.Initialized) { report = "KWA FEE /sim: CoreGame failed to initialize (prefabs assigned?)."; Debug.LogWarning(report); return; }
            simSeconds = seconds;
            int simSteps = Mathf.Max(1, Mathf.RoundToInt(seconds / fixedDt));
            var scene = Physics.defaultPhysicsScene;
            bool wasAuto = UnityEngine.Physics.autoSimulation;
            UnityEngine.Physics.autoSimulation = false; // harness owns the step
            try {
                for (int i = 0; i < simSteps; i++) {
                    float t = i * fixedDt;
                    for (int p = 0; p < game.Players.Count; p++) ScriptTick(game.Players[p], t);
                    game.Simulate(scene, fixedDt);
                }
            } finally {
                UnityEngine.Physics.autoSimulation = wasAuto;
            }
            report = BuildReport(seconds);
            WriteReport();
        }

        void ScriptTick(Barista player, float time) {
            if (player.HeldCup == null) {
                // Stand near the rack; machines refill it as the shift runs.
                player.Aim(new Vector3(-4f, 0.95f, 0f));
                player.Move(new Vector2(1f, 0f));
                player.TryGrab();
                // Scripted griefing: occasional steal attempts keep the steal
                // counter honest even when nobody is holding nearby.
                if ((int)(time * .5f) % 2 == 0 && player.Id != 0) player.Steal();
            } else {
                // Hold the rack cup, take a couple of sips (never a full
                // chug to overdose), then charge-fly it at the tray. A miss
                // lands in the tray; a hit lands on a coworker; a lucky serve
                // routes to the customer.
                if ((int)(time * .35f) % 3 == 0 && time > 0.2f) player.BeginChug();
                if ((int)(time * .6f) % 5 == 0 && time > 1.2f) player.BeginCharge();
                if ((int)(time * .6f) % 5 == 3 && time > 1.2f) player.ReleaseCharge();
                if (player.Chug01 > 0.15f) player.EndChug();
                player.Aim(new Vector3(0f, 0f, 3.5f));
                player.Move(new Vector2(0f, 1f));
                if (time > 2f && (int)(time * .25f) % 4 == 0) player.SabotageNearest();
                if (time > 3f && (int)(time * .4f) % 5 == 1) player.FixNearest();
            }
        }

        string BuildReport(float seconds) {
            int[] playerTips = new int[4];
            for (int i = 0; i < game.Players.Count; i++) playerTips[i] = game.PlayerTips(i);
            float chaosPerMin = game.ChaosEvents / Mathf.Max(1f, seconds / 60f);
            return "KWA FEE /sim report\n" +
                "seconds=" + seconds + " steps=" + Mathf.RoundToInt(seconds / fixedDt) + " dt=" + fixedDt + " physics=" + "engine-50Hz\n" +
                "chaos_events/min=" + Stringify(chaosPerMin) + " chaos_total=" + game.ChaosEvents + "\n" +
                "serves=" + game.Serves + " tips_total=$" + game.TotalTips +
                " tips_by_player=" + playerTips[0] + "," + playerTips[1] + "," + playerTips[2] + "," + playerTips[3] + "\n" +
                "shots=" + game.Shots + " hits=" + game.Hits + " misses=" + game.Misses + " catches=" + game.Catches + "\n" +
                "chugs=" + game.Chugs + " overdoses=" + game.Overdoses + " fixes=" + game.Fixes + " sabotages=" + game.Sabotages + " steals=" + game.Steals + "\n" +
                "machine_health=" + Stringify(game.MachineHealth(MachineKind.Espresso)) + "," +
                Stringify(game.MachineHealth(MachineKind.Grinder)) + "," + Stringify(game.MachineHealth(MachineKind.SteamWand)) + "," +
                Stringify(game.MachineHealth(MachineKind.IceMachine)) + " broken=" + game.BrokenMachines + "\n" +
                "rack_cups=" + RackCups() + "\n";
        }

        string Stringify(float v) { return string.Format("{0:.3f}", v); }

        int RackCups() {
            int n = 0; for (int i = 0; i < game.Cups.Count; i++) if (game.Cups[i].State == CupState.Rack) n++;
            return n;
        }

        void WriteReport() {
            // The report is written to persistentDataPath (writable in builds)
            // and also echoed to the log, so an Editor/container run can read
            // it back; numbers are always re-derived by re-running the harness.
            var dirPath = Application.persistentDataPath + "/kwafee-sim";
            Directory.CreateDirectory(dirPath);
            var path = dirPath + "/sim-report.txt";
            File.WriteAllText(path, report);
            Debug.Log("KWA FEE /sim wrote " + path + "\n" + report);
        }
    }
}
using UnityEngine;

namespace KwaFee {
    // Kinematic shell only: position, velocity, held-cup binding. All gameplay
    // verdicts (charge, chug, stun, steal range, fling speed) live in Shift;
    // CoreGame routes Shift effects here, and this component exposes nothing
    // but kinematic state. No rule, no timers.
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Barista : MonoBehaviour {
        public int Id { get; private set; }
        public CoffeeCup HeldCup { get; private set; }
        public Vector3 VelocityRef => body != null ? body.linearVelocity : Vector3.zero;
        Rigidbody body;
        Vector3 spawnPosition;

        static readonly Vector3[] spawnPositions = { new Vector3(-3f, 0f, -3f), new Vector3(-1f, 0f, -3f), new Vector3(1f, 0f, -3f), new Vector3(3f, 0f, -3f) };

        public void Configure(int id) {
            Id = id;
            body = GetComponent<Rigidbody>();
            if (body == null) body = gameObject.AddComponent<Rigidbody>();
            body.freezeRotation = true;
            body.sleepThreshold = 0f; // bots/players must never auto-sleep mid-drive
            spawnPosition = spawnPositions[id % spawnPositions.Length];
            transform.position = spawnPosition;
        }

        public void Take(CoffeeCup cup) { HeldCup = cup; cup.Hold(); }
        internal CoffeeCup Relinquish() { CoffeeCup cup = HeldCup; HeldCup = null; return cup; }
        // Teleport a stunned/overdosed actor back to its spawn pad.
        internal void Respawn() {
            transform.position = spawnPosition;
            body.linearVelocity = Vector3.zero;
        }
        internal Vector3 HandFollowTarget(Vector3 aim) { return transform.position + aim * .55f + Vector3.up * 1.05f; }
    }
}
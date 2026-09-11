using UnityEngine;

namespace KwaFee {
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Barista : MonoBehaviour {
        public int Id { get; private set; }
        public CoffeeCup HeldCup { get; private set; }
        public bool IsStunned => stun > 0f;
        public float Charge01 => FlingRules.Charge01(charge);
        public float Chug01 => Mathf.Clamp01(chugSeconds / VerbRules.OverdoseSeconds);
        public float StunTime => stun;
        public Vector3 VelocityRef => body != null ? body.linearVelocity : Vector3.zero;
        Rigidbody body; CoreGame game; Vector2 move; Vector3 aim = Vector3.forward; float charge; bool charging; float stun; float chugSeconds; bool chugging; bool overdosed;
        Vector3 spawnPosition;

        static readonly Vector3[] spawnPositions = { new Vector3(-3f, 0f, -3f), new Vector3(-1f, 0f, -3f), new Vector3(1f, 0f, -3f), new Vector3(3f, 0f, -3f) };

        public void Configure(CoreGame owner, int id) {
            game = owner; Id = id; body = GetComponent<Rigidbody>(); if (body == null) body = gameObject.AddComponent<Rigidbody>();
            body.freezeRotation = true;
            body.sleepThreshold = 0f; // bots/players must never auto-sleep mid-drive
            spawnPosition = spawnPositions[id % spawnPositions.Length];
            transform.position = spawnPosition;
        }
        public void Move(Vector2 direction) { move = Vector2.ClampMagnitude(direction, 1f); }
        public void Aim(Vector3 worldPoint) { Vector3 flat = worldPoint - transform.position; flat.y = 0f; if (flat.sqrMagnitude > .01f) aim = flat.normalized; }
        public void TryGrab() { if (HeldCup == null && !IsStunned) game.TryGrab(this); }
        public void RestartPosition() { transform.position = spawnPosition; body.linearVelocity = Vector3.zero; if (HeldCup != null) Drop(); }
        public void Drop() { if (HeldCup != null) { HeldCup.Drop(body.linearVelocity); HeldCup = null; charging = false; } }
        public void BeginCharge() { if (HeldCup != null && !IsStunned) charging = true; }
        public void ReleaseCharge() {
            if (HeldCup == null || !charging) return;
            float speed = FlingRules.SpeedForCharge(charge);
            HeldCup.Throw(aim * speed + Vector3.up * .5f + body.linearVelocity); game.RecordShot(); HeldCup = null; charging = false; charge = 0f;
        }
        // CHUG requires holding a cup with coffee in it; each sip drains the
        // cup via the shared VerbRules.ChugConsume path. Empty cup -> no chug.
        public void BeginChug() {
            if (IsStunned || chugging) return;
            if (!VerbRules.CanChug(HeldCup != null, HeldCup != null ? HeldCup.Liquid : 0f)) { game.Roast("Big Vinny: Nothin' to slug, kehd."); return; }
            chugging = true; game.RecordChug(Id);
        }
        public void EndChug() { chugging = false; }
        public void FixNearest() { if (!IsStunned) game.TryFix(this); }
        public void Steal() { if (!IsStunned && HeldCup == null) game.TrySteal(this); }
        public void SabotageNearest() { if (!IsStunned) game.TrySabotage(this); }
        internal void Take(CoffeeCup cup) { HeldCup = cup; cup.Hold(this); }
        internal CoffeeCup Relinquish() { CoffeeCup cup = HeldCup; HeldCup = null; charging = false; return cup; }
        internal void Stun() { StunFor(.65f); }
        internal void Overdose() { chugging = false; chugSeconds = 0f; overdosed = true; StunFor(3f); }
        void StunFor(float seconds) { stun = Mathf.Max(stun, seconds); charging = false; if (HeldCup != null) Drop(); }
        internal bool IsTargetAt(Vector3 point) { return (point - transform.position).sqrMagnitude < 1.4f * 1.4f; }
        internal void Tick(float dt) {
            if (stun > 0f) {
                stun -= dt;
                body.linearVelocity = Vector3.zero;
                if (stun <= 0f && overdosed) { overdosed = false; transform.position = spawnPosition; }
                return;
            }
            float boost = VerbRules.ChugMultiplier(chugSeconds);
            float jitter = chugSeconds > 0f ? Mathf.Sin(chugSeconds * 18f + Id) * .06f : 0f;
            Vector3 velocity = new Vector3(move.x + jitter, 0f, move.y - jitter) * game.MoveSpeed * boost;
            body.linearVelocity = new Vector3(velocity.x, body.linearVelocity.y, velocity.z);
            body.WakeUp(); // Unity autosleep swallows velocity writes on sleeping bodies
            if (aim.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(aim, Vector3.up);
            if (charging) charge = Mathf.Min(FlingRules.ChargeDuration, charge + dt);
            if (chugging) {
                chugSeconds += dt;
                if (HeldCup != null && HeldCup.Liquid > 0f) {
                    HeldCup.Spill(HeldCup.Liquid - VerbRules.ChugConsume(HeldCup.Liquid, dt));
                    if (HeldCup.Liquid < VerbRules.MinChugLiquid) chugging = false;
                } else chugging = false;
                if (VerbRules.IsOverdose(chugSeconds)) { game.TriggerOverdose(this); return; }
            } else chugSeconds = Mathf.Max(0f, chugSeconds - dt * .65f);
            if (HeldCup != null) HeldCup.FollowHand(transform.position + aim * .55f + Vector3.up * 1.05f, transform.rotation);
        }
    }
}
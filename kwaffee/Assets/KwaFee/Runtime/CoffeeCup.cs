using UnityEngine;

namespace KwaFee {
    public enum CupState { Pool, Rack, Held, Airborne, Served }

    [RequireComponent(typeof(Rigidbody))]
    public sealed class CoffeeCup : MonoBehaviour {
        public CupState State { get; private set; }
        public int OwnerId { get; private set; } = -1;
        public float Liquid { get; private set; }
        public float Age { get; private set; }
        public bool WasAirborne { get; private set; }
        public bool HitPlayer { get; internal set; }
        Rigidbody body; Collider cupCollider; CoreGame game; Transform visual;
        float ownerGrace; bool scored;

        public void Configure(CoreGame owner) {
            game = owner; body = GetComponent<Rigidbody>(); if (body == null) body = gameObject.AddComponent<Rigidbody>(); cupCollider = GetComponent<Collider>();
            if (cupCollider == null) { SphereCollider sphere = gameObject.AddComponent<SphereCollider>(); sphere.center = Vector3.up * .2f; sphere.radius = .2f; cupCollider = sphere; }
            visual = transform.childCount > 0 ? transform.GetChild(0) : null;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            ResetToPool(owner.PoolPosition);
        }
        public void ResetToPool(Vector3 position) {
            State = CupState.Pool; OwnerId = -1; Liquid = 0f; Age = 0f; ownerGrace = 0f; scored = false; WasAirborne = false; HitPlayer = false;
            body.isKinematic = true; body.detectCollisions = false; cupCollider.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.identity); body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            if (visual != null) visual.localScale = Vector3.one;
        }
        public void PutOnRack(Vector3 position) {
            State = CupState.Rack; Liquid = 1f; Age = 0f; OwnerId = -1; scored = false; WasAirborne = false;
            // Rack cups are inventory slots: kinematic and non-colliding so a
            // barista can walk up to the rack and press E without the cup
            // acting as a solid shelf obstacle.
            body.isKinematic = true; body.detectCollisions = false; cupCollider.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.identity); body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
        }
        public void Hold(Barista holder) {
            State = CupState.Held; OwnerId = holder.Id; Age = 0f; body.isKinematic = true; body.detectCollisions = false; cupCollider.enabled = false;
        }
        public void FollowHand(Vector3 position, Quaternion rotation) { if (State == CupState.Held) transform.SetPositionAndRotation(position, rotation); }
        public void Throw(Vector3 velocity) {
            State = CupState.Airborne; WasAirborne = true; Age = 0f; ownerGrace = .15f; body.isKinematic = false; body.detectCollisions = true; cupCollider.enabled = true;
            body.linearVelocity = velocity; body.angularVelocity = game.SpinForCup() * 13f;
        }
        public void Drop(Vector3 velocity) {
            State = CupState.Airborne; Age = 0f; body.isKinematic = false; body.detectCollisions = true; cupCollider.enabled = true; body.linearVelocity = velocity;
        }
        public void MarkServed() { if (!scored) { scored = true; State = CupState.Served; body.linearVelocity = Vector3.zero; body.isKinematic = true; } }
        public void Spill(float amount) { Liquid = Mathf.Max(0f, Liquid - amount); }
        public void Tick(float dt) {
            if (ownerGrace > 0f) ownerGrace -= dt;
            if (State == CupState.Airborne || State == CupState.Served) Age += dt;
            if (State == CupState.Airborne && transform.up.y < .2f) Spill(dt * .55f);
            if (visual != null && State == CupState.Airborne) visual.localScale = Vector3.one + new Vector3(0.08f, -0.06f, 0.08f) * Mathf.Clamp01(body.linearVelocity.magnitude / 12f);
        }
        void OnCollisionEnter(Collision collision) {
            if (State != CupState.Airborne) return;
            float speed = collision.relativeVelocity.magnitude;
            if (speed > 2f) Spill(Mathf.Clamp01((speed - 2f) * .08f));
            if (speed >= 3f && game != null) game.TryCupHit(this, collision.GetContact(0).point, ownerGrace > 0f);
        }
    }
}
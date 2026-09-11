using UnityEngine;

namespace KwaFee {
    // Kinematic cup states. The authoritative gameplay copy lives in Shift's
    // cup records; this enum is also the mirror the MonoBehaviour keeps for
    // physics/collider config (a served cup must stop colliding, etc.) — a
    // kinematic concern, not a rule.
    public enum CupState { Pool, Rack, Held, Airborne, Served }

    [RequireComponent(typeof(Rigidbody))]
    public sealed class CoffeeCup : MonoBehaviour {
        public int SlotId { get; private set; }
        // Kinematic mirror only: decides collider/rigidbody behavior. Gameplay
        // reads (owner, liquid, hit flow) come from Shift.Metrics().
        public CupState State { get; private set; }
        Rigidbody body; Collider cupCollider; CoreGame game; Transform visual;

        public void Configure(CoreGame owner, int slotId) {
            game = owner; SlotId = slotId;
            body = GetComponent<Rigidbody>();
            if (body == null) body = gameObject.AddComponent<Rigidbody>();
            cupCollider = GetComponent<Collider>();
            if (cupCollider == null) {
                SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
                sphere.center = Vector3.up * .2f;
                sphere.radius = .2f;
                cupCollider = sphere;
            }
            visual = transform.childCount > 0 ? transform.GetChild(0) : null;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            ResetToPool(owner.PoolPosition);
        }

        public void ResetToPool(Vector3 position) {
            State = CupState.Pool;
            body.isKinematic = true; body.detectCollisions = false; cupCollider.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            if (visual != null) visual.localScale = Vector3.one;
        }

        public void PutOnRack(Vector3 position) {
            State = CupState.Rack;
            // Rack cups are inventory slots: kinematic and non-colliding so a
            // barista can walk up to the rack and press E without the cup
            // acting as a solid shelf obstacle.
            body.isKinematic = true; body.detectCollisions = false; cupCollider.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
        }

        public void Hold() {
            State = CupState.Held;
            body.isKinematic = true; body.detectCollisions = false; cupCollider.enabled = false;
        }

        public void FollowHand(Vector3 position, Quaternion rotation) {
            if (State == CupState.Held) transform.SetPositionAndRotation(position, rotation);
        }

        public void Throw(Vector3 velocity, Vector3 angularVelocity) {
            State = CupState.Airborne;
            body.isKinematic = false; body.detectCollisions = true; cupCollider.enabled = true;
            body.linearVelocity = velocity; body.angularVelocity = angularVelocity;
        }

        public void Drop(Vector3 velocity) {
            State = CupState.Airborne;
            body.isKinematic = false; body.detectCollisions = true; cupCollider.enabled = true;
            body.linearVelocity = velocity;
        }

        public void MarkServed() {
            State = CupState.Served;
            body.linearVelocity = Vector3.zero; body.isKinematic = true;
        }

        // Visual-only: squash with flight speed. Age/liquid/spill verdicts are
        // Shift's (Metrics.CupLiquid), read by the rules every Step.
        public void TickVisual() {
            if (visual != null && State == CupState.Airborne) {
                visual.localScale = Vector3.one + new Vector3(0.08f, -0.06f, 0.08f) * Mathf.Clamp01(body.linearVelocity.magnitude / 12f);
            }
        }

        void OnCollisionEnter(Collision collision) {
            if (State != CupState.Airborne || game == null) return;
            game.OnCupCollision(SlotId, collision.GetContact(0).point, collision.relativeVelocity.magnitude);
        }
    }
}
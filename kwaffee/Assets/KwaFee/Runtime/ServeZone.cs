using UnityEngine;

namespace KwaFee {
    // Convex trigger box on the serving counter. Forwarding only: the trigger
    // event is translated into a SlotId and handed to CoreGame, which routes it
    // into the Shift seam (Shift.OnServe). No verdict lives here.
    public sealed class ServeZone : MonoBehaviour {
        CoreGame game;
        public void Configure(CoreGame owner) {
            game = owner;
            Collider existing = GetComponent<Collider>();
            // A served tray must use a convex trigger volume. Culling an
            // invariant/performance defeat: swap any MeshCollider for a
            // primitive BoxCollider sized to this node's bounds, else drop a
            // fresh box.
            if (existing is MeshCollider) { DestroyImmediate(existing); existing = null; }
            if (existing == null) {
                BoxCollider box = gameObject.AddComponent<BoxCollider>();
                // Arcade catch shelf in front of the customers: wide and tall
                // enough that a flung cup reads as "served" like every
                // catch-game tray, not a 0.2-high invisible plate.
                box.center = new Vector3(0f, .7f, -.3f); box.size = new Vector3(2.4f, 1.6f, 1.2f);
                existing = box;
            }
            existing.isTrigger = true;
        }
        void OnTriggerEnter(Collider other) {
            CoffeeCup cup = other.GetComponent<CoffeeCup>();
            if (cup != null && game != null) game.OnServeZone(cup.SlotId);
        }
    }
}
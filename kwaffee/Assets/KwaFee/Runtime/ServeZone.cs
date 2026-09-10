using UnityEngine;

namespace KwaFee {
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
                box.center = Vector3.up * .05f; box.size = new Vector3(1.4f, .2f, .8f);
                existing = box;
            }
            existing.isTrigger = true;
        }
        void OnTriggerEnter(Collider other) { CoffeeCup cup = other.GetComponent<CoffeeCup>(); if (cup != null) game.TryServe(cup); }
    }
}
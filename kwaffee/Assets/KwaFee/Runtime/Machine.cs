using UnityEngine;

namespace KwaFee {
    // A coffee machine. Its static MeshCollider (non-convex, non-trigger) is
    // a valid environment surface that blocks players and cups; FIX/SABOTAGE
    // target machines by proximity (CoreGame.NearestMachine), not by this
    // collider, so it must NOT be a trigger (Unity rejects triggers on
    // concave MeshColliders). Health lives in CoreGame (single source of
    // truth); this component is the scene-side identity.
    public sealed class Machine : MonoBehaviour {
        public int Id { get; private set; }

        public void Configure(CoreGame owner, int id) { Id = id; }
    }
}
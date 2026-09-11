using UnityEngine;

namespace KwaFee {
    // A coffee machine. Its static MeshCollider (non-convex, non-trigger) is
    // a valid environment surface that blocks players and cups; FIX/SABOTAGE
    // target machines by proximity (Shift.NearestMachine), not by this
    // collider, so it must NOT be a trigger (Unity rejects triggers on
    // concave MeshColliders). Health/timers/backlog live in Shift (single
    // source of truth); this component is the scene-side identity only.
    public sealed class Machine : MonoBehaviour {
        public int Id { get; private set; }

        public void Configure(int id) { Id = id; }
    }
}
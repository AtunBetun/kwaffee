using UnityEngine;

namespace KwaFee {
    // A coffee machine: its trigger volume marks it as a FIX/SABOTAGE target
    // for the player verbs. Health lives in CoreGame (single source of truth);
    // this component is the scene-side identity + collision surface.
    [RequireComponent(typeof(MeshCollider))]
    public sealed class Machine : MonoBehaviour {
        public int Id { get; private set; }
        CoreGame game;

        public void Configure(CoreGame owner, int id) {
            game = owner; Id = id;
            Collider collider = GetComponent<Collider>();
            if (collider == null) { MeshCollider mesh = gameObject.AddComponent<MeshCollider>(); MeshFilter filter = GetComponent<MeshFilter>(); if (filter != null) mesh.sharedMesh = filter.sharedMesh; collider = mesh; }
            collider.isTrigger = true;
        }
    }
}
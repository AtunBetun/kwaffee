using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KwaFee.Editor
{
    [Serializable] sealed class ArtManifest
    {
        public MaterialSpec[] materials;
        public MeshSpec[] meshes;
    }
    [Serializable] sealed class MaterialSpec
    {
        public string name;
        public float[] color;
        public float roughness;
    }
    [Serializable] sealed class MeshSpec
    {
        public string name;
        public float[] vertices;
        public float[] normals;
        public SubmeshSpec[] submeshes;
        public BoundsSpec bounds;
    }
    [Serializable] sealed class SubmeshSpec
    {
        public int material;
        public int[] triangles;
    }
    [Serializable] sealed class BoundsSpec { public float[] min; public float[] max; }

    public static class CoreSceneBuilder
    {
        const string ArtPath = "Assets/KwaFee/Art/core-meshes.json";
        const string Generated = "Assets/KwaFee/Generated";
        const string ScenePath = "Assets/KwaFee/Scenes/CoreLoop.unity";
        static readonly string[] Names = { "Cup", "Barista", "Customer", "Counter", "Floor", "Wall", "Tray", "Sign", "Espresso", "Grinder", "SteamWand", "IceMachine" };

        [MenuItem("KWA FEE/Import Authored Art", priority = 100)]
        public static void ImportAuthoredArt()
        {
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), ArtPath)))
                throw new InvalidOperationException("Missing authored art manifest at " + ArtPath);
            var manifest = JsonUtility.FromJson<ArtManifest>(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), ArtPath)));
            Validate(manifest);
            EnsureFolders();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP/Lit shader is unavailable; install the configured URP package.");
            var materials = new Material[manifest.materials.Length];
            for (var i = 0; i < materials.Length; i++)
            {
                var spec = manifest.materials[i];
                var path = Generated + "/Material_" + Safe(spec.name) + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                var fresh = new Material(shader) { name = "Material_" + Safe(spec.name) };
                fresh.SetColor("_BaseColor", new Color(spec.color[0], spec.color[1], spec.color[2], spec.color[3]));
                fresh.SetFloat("_Smoothness", Mathf.Clamp01(1f - spec.roughness));
                if (material == null) { AssetDatabase.CreateAsset(fresh, path); material = fresh; }
                else { EditorUtility.CopySerialized(fresh, material); UnityEngine.Object.DestroyImmediate(fresh); EditorUtility.SetDirty(material); }
                materials[i] = material;
            }
            foreach (var spec in manifest.meshes)
            {
                var mesh = BuildMesh(spec);
                var meshPath = Generated + "/Mesh_" + Safe(spec.name) + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (existing == null) { AssetDatabase.CreateAsset(mesh, meshPath); }
                else { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; EditorUtility.SetDirty(mesh); }
                CreatePrefab(spec, mesh, materials);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("KWA FEE authored art imported to " + Generated);
        }

        [MenuItem("KWA FEE/Create Core Loop Scene", priority = 101)]
        public static void CreateCoreLoopScene()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save dirty scene(s) before creating " + ScenePath + ".");
            EnsureFolders();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("CoreGame");
            var game = root.AddComponent<KwaFee.CoreGame>();
            SetPrefab(game, "cupPrefab", "Cup"); SetPrefab(game, "baristaPrefab", "Barista");
            SetPrefab(game, "customerPrefab", "Customer"); SetPrefab(game, "counterPrefab", "Counter");
            SetPrefab(game, "floorPrefab", "Floor"); SetPrefab(game, "wallPrefab", "Wall");
            SetPrefab(game, "trayPrefab", "Tray"); SetPrefab(game, "signPrefab", "Sign");
            SetPrefab(game, "espressoPrefab", "Espresso"); SetPrefab(game, "grinderPrefab", "Grinder");
            SetPrefab(game, "steamWandPrefab", "SteamWand"); SetPrefab(game, "iceMachinePrefab", "IceMachine");
            var camera = new GameObject("Main Camera", typeof(Camera));
            camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 12, -12);
            camera.transform.LookAt(new Vector3(0, 0, 1));
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor = new Color(0.12f, 0.06f, 0.09f);
            var light = new GameObject("Main Directional Light", typeof(Light));
            light.GetComponent<Light>().type = LightType.Directional; light.GetComponent<Light>().intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.22f, 0.18f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("KWA FEE core loop scene created at " + ScenePath);
        }

        static void SetPrefab(KwaFee.CoreGame game, string field, string name)
        {
            var property = new SerializedObject(game).FindProperty(field);
            if (property == null) throw new InvalidOperationException("CoreGame is missing public prefab field " + field + ".");
            property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Generated + "/" + name + ".prefab");
            if (property.objectReferenceValue == null) throw new InvalidOperationException("Import Authored Art first; missing " + name + ".prefab.");
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreatePrefab(MeshSpec spec, Mesh mesh, Material[] materials)
        {
            var path = Generated + "/" + spec.name + ".prefab";
            var root = new GameObject(spec.name);
            var filter = root.AddComponent<MeshFilter>(); filter.sharedMesh = mesh;
            var renderer = root.AddComponent<MeshRenderer>();
            var slots = new Material[mesh.subMeshCount];
            for (var i = 0; i < slots.Length; i++) slots[i] = materials[spec.submeshes[i].material];
            renderer.sharedMaterials = slots;
            // Dynamic bodies use primitive colliders: Unity limits convex
            // MeshColliders to a small hull, while the authored silhouettes
            // intentionally retain thousands of render vertices.
            if (spec.name == "Cup")
            {
                var collider = root.AddComponent<SphereCollider>();
                collider.center = Vector3.up * .2f;
                collider.radius = .2f;
            }
            else if (spec.name == "Barista" || spec.name == "Customer")
            {
                var collider = root.AddComponent<CapsuleCollider>();
                collider.center = Vector3.up * .9f;
                collider.height = 1.8f;
                collider.radius = spec.name == "Customer" ? .42f : .35f;
            }
            else
            {
                var collider = root.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
                collider.convex = false;
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        static Mesh BuildMesh(MeshSpec spec)
        {
            var mesh = new Mesh { name = "Mesh_" + Safe(spec.name) };
            if (spec.vertices.Length / 3 > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            var vertices = new Vector3[spec.vertices.Length / 3];
            for (var i = 0; i < vertices.Length; i++) vertices[i] = new Vector3(spec.vertices[i * 3], spec.vertices[i * 3 + 1], spec.vertices[i * 3 + 2]);
            mesh.vertices = vertices;
            if (spec.normals != null && spec.normals.Length == spec.vertices.Length)
            {
                var normals = new Vector3[vertices.Length]; for (var i = 0; i < normals.Length; i++) normals[i] = new Vector3(spec.normals[i * 3], spec.normals[i * 3 + 1], spec.normals[i * 3 + 2]); mesh.normals = normals;
            }
            else mesh.RecalculateNormals();
            mesh.subMeshCount = spec.submeshes.Length;
            for (var i = 0; i < spec.submeshes.Length; i++)
            {
                var triangles = spec.submeshes[i].triangles;
                mesh.SetTriangles(triangles, i, false);
            }
            mesh.RecalculateBounds();
            return mesh;
        }

        static void Validate(ArtManifest m)
        {
            if (m == null || m.materials == null || m.meshes == null) throw new InvalidOperationException("Manifest must contain materials and meshes.");
            var valid = new HashSet<string>(); foreach (var n in Names) valid.Add(n);
            var present = new HashSet<string>();
            foreach (var spec in m.meshes)
            {
                if (!valid.Contains(spec.name)) throw new InvalidOperationException("Unknown mesh name in manifest: " + spec.name);
                if (!present.Add(spec.name)) throw new InvalidOperationException("Duplicate mesh in manifest: " + spec.name);
                if (spec.vertices == null || spec.vertices.Length == 0 || spec.vertices.Length % 3 != 0 || spec.submeshes == null) throw new InvalidOperationException("Invalid vertices/submeshes for " + spec.name);
                CheckFinite(spec.vertices, spec.name + " vertices"); if (spec.normals != null) { if (spec.normals.Length != spec.vertices.Length) throw new InvalidOperationException("Normal count mismatch for " + spec.name); CheckFinite(spec.normals, spec.name + " normals"); }
                foreach (var sub in spec.submeshes) { if (sub.material < 0 || sub.material >= m.materials.Length) throw new InvalidOperationException("Bad material index in " + spec.name); if (sub.triangles == null || sub.triangles.Length % 3 != 0) throw new InvalidOperationException("Invalid triangles in " + spec.name); foreach (var index in sub.triangles) if (index < 0 || index >= spec.vertices.Length / 3) throw new InvalidOperationException("Bad vertex index in " + spec.name); }
            }
            var missing = new List<string>(); foreach (var n in Names) if (!present.Contains(n)) missing.Add(n);
            if (missing.Count > 0) throw new InvalidOperationException("Manifest missing required meshes: " + String.Join(", ", missing));
            foreach (var mat in m.materials) { if (mat.color == null || mat.color.Length != 4 || mat.roughness < 0f || mat.roughness > 1f) throw new InvalidOperationException("Invalid material reference/color: " + mat.name); CheckFinite(mat.color, mat.name + " color"); }
        }
        static void CheckFinite(float[] values, string label) { foreach (var value in values) if (float.IsNaN(value) || float.IsInfinity(value)) throw new InvalidOperationException("Non-finite " + label); }
        static string Safe(string value) { return string.IsNullOrEmpty(value) ? "Unnamed" : value.Replace("/", "_").Replace("\\", "_"); }
        static void EnsureFolders() { foreach (var path in new[] { Generated, "Assets/KwaFee/Scenes" }) if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path)); }
    }
}

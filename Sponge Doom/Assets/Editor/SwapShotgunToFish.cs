using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SwapShotgunToFish
{
    [MenuItem("Tools/Swap Shotgun With Fish")]
    public static void Run()
    {
        // Find the GunModel under the gun rig
        var gun = GameObject.Find("CameraHolder/PlayerCam/ShotGun_C/GunModel");
        if (gun == null) { Debug.LogError("GunModel not found at expected path."); return; }

        var fishAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/FishGun/scene.gltf");
        if (fishAsset == null) { Debug.LogError("Fish gltf not imported yet."); return; }

        // Wipe the existing mesh children
        var oldKids = new System.Collections.Generic.List<Transform>();
        foreach (Transform c in gun.transform) oldKids.Add(c);
        foreach (var c in oldKids) Object.DestroyImmediate(c.gameObject);

        // Instantiate the fish as a child
        var fish = (GameObject)PrefabUtility.InstantiatePrefab(fishAsset);
        Undo.RegisterCreatedObjectUndo(fish, "Spawn FishMesh");
        fish.name = "FishMesh";
        fish.transform.SetParent(gun.transform, false);
        fish.transform.localPosition = Vector3.zero;
        // glTF authoring convention is +Z forward in many tools but exports vary. Start facing forward;
        // the user can rotate in the Inspector if needed.
        fish.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        fish.transform.localScale = Vector3.one * 0.3f;

        // Kill any colliders on the fish so it doesn't shove the player's capsule
        foreach (var col in fish.GetComponentsInChildren<Collider>(true)) col.enabled = false;

        // Measure for sanity
        var rends = fish.GetComponentsInChildren<Renderer>(true);
        Bounds b = new Bounds(fish.transform.position, Vector3.zero);
        bool first = true;
        foreach (var r in rends) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Shotgun swapped. renderers={rends.Length} worldBoundsSize={b.size:F2}");
    }
}

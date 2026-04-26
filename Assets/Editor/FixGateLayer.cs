using UnityEngine;
using UnityEditor;

public static class FixGateLayer
{
    public static void Execute()
    {
        string[] gatePaths = {
            "Assets/Prefabs/Interactables/FuelGate_High.prefab",
            "Assets/Prefabs/Interactables/FuelGate_Mid.prefab",
            "Assets/Prefabs/Interactables/FuelGate_Low.prefab",
        };

        foreach (var path in gatePaths)
        {
            var contents = PrefabUtility.LoadPrefabContents(path);
            contents.layer = 8; // Ground
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
            Debug.Log($"[FixGateLayer] Set {path} to Layer 8 (Ground)");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[FixGateLayer] Done. Gates are now on Ground layer.");
    }
}

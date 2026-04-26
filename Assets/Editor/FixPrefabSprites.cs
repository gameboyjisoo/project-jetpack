using UnityEngine;
using UnityEditor;

public static class FixPrefabSprites
{
    public static void Execute()
    {
        var updates = new (string prefabPath, string spritePath)[]
        {
            ("Assets/Prefabs/Interactables/Hazard.prefab",        "Assets/Sprites/Placeholders/Hazard.png"),
            ("Assets/Prefabs/Interactables/FuelPickup.prefab",    "Assets/Sprites/Placeholders/FuelPickup.png"),
            ("Assets/Prefabs/Interactables/DashPickup.prefab",    "Assets/Sprites/Placeholders/DashPickup.png"),
            ("Assets/Prefabs/Interactables/FuelGate_High.prefab", "Assets/Sprites/Placeholders/FuelGate.png"),
            ("Assets/Prefabs/Interactables/FuelGate_Mid.prefab",  "Assets/Sprites/Placeholders/FuelGate.png"),
            ("Assets/Prefabs/Interactables/FuelGate_Low.prefab",  "Assets/Sprites/Placeholders/FuelGate.png"),
        };

        foreach (var (prefabPath, spritePath) in updates)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (prefab == null || sprite == null)
            {
                Debug.LogWarning($"[FixPrefabSprites] Missing: {prefabPath} or {spritePath}");
                continue;
            }

            // Open prefab for editing
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            var sr = contents.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = sprite;
                Debug.Log($"[FixPrefabSprites] Updated {prefabPath} sprite to {sprite.name}");
            }
            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[FixPrefabSprites] All prefab sprites updated.");
    }
}

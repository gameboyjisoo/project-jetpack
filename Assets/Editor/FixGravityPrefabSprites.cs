using UnityEngine;
using UnityEditor;

/// <summary>
/// Updates GravitySwitch prefabs to use directional arrow sprites
/// instead of the generic SpawnPoint arrow.
/// </summary>
public static class FixGravityPrefabSprites
{
    [MenuItem("Project Jetpack/Setup/Fix Gravity Prefab Sprites")]
    public static void Execute()
    {
        var updates = new (string prefab, string sprite)[]
        {
            ("Assets/Prefabs/Interactables/GravitySwitch_Down.prefab",  "Assets/Sprites/Placeholders/GravityArrow_Down.png"),
            ("Assets/Prefabs/Interactables/GravitySwitch_Up.prefab",    "Assets/Sprites/Placeholders/GravityArrow_Up.png"),
            ("Assets/Prefabs/Interactables/GravitySwitch_Left.prefab",  "Assets/Sprites/Placeholders/GravityArrow_Left.png"),
            ("Assets/Prefabs/Interactables/GravitySwitch_Right.prefab", "Assets/Sprites/Placeholders/GravityArrow_Right.png"),
        };

        foreach (var (prefabPath, spritePath) in updates)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (prefab == null || sprite == null)
            {
                Debug.LogWarning($"[FixGravityPrefabSprites] Missing: {prefabPath} or {spritePath}");
                continue;
            }

            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            var sr = contents.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sprite = sprite;

            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
            Debug.Log($"[FixGravityPrefabSprites] Updated {prefabPath}");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[FixGravityPrefabSprites] Done — all 4 prefabs now have directional arrows.");
    }
}

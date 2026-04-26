using UnityEngine;
using UnityEditor;

public static class FixSpawnTileSprites
{
    public static void Execute()
    {
        var updates = new (string tilePath, string spritePath)[]
        {
            ("Assets/Tiles/Interactables/Hazard.asset",        "Assets/Sprites/Placeholders/Hazard.png"),
            ("Assets/Tiles/Interactables/FuelPickup.asset",    "Assets/Sprites/Placeholders/FuelPickup.png"),
            ("Assets/Tiles/Interactables/DashPickup.asset",    "Assets/Sprites/Placeholders/DashPickup.png"),
            ("Assets/Tiles/Interactables/FuelGate_High.asset", "Assets/Sprites/Placeholders/FuelGate.png"),
            ("Assets/Tiles/Interactables/FuelGate_Mid.asset",  "Assets/Sprites/Placeholders/FuelGate.png"),
            ("Assets/Tiles/Interactables/FuelGate_Low.asset",  "Assets/Sprites/Placeholders/FuelGate.png"),
            ("Assets/Tiles/Interactables/SpawnPoint.asset",    "Assets/Sprites/Placeholders/SpawnPoint.png"),
        };

        foreach (var (tilePath, spritePath) in updates)
        {
            var tile = AssetDatabase.LoadAssetAtPath<SpawnTile>(tilePath);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            if (tile == null) { Debug.LogError($"Tile not found: {tilePath}"); continue; }
            if (sprite == null) { Debug.LogError($"Sprite not found: {spritePath}"); continue; }

            Debug.Log($"[FixSprites] {tilePath}: current sprite = {(tile.editorSprite != null ? tile.editorSprite.name : "null")}, new sprite = {sprite.name}");

            var so = new SerializedObject(tile);
            var prop = so.FindProperty("editorSprite");
            prop.objectReferenceValue = sprite;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(tile);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Rebuild palette too
        string palettePath = "Assets/Tiles/Palettes/Interactables Palette.prefab";
        if (System.IO.File.Exists(palettePath))
            AssetDatabase.DeleteAsset(palettePath);
        CreateSpawnTiles.Execute();

        Debug.Log("[FixSprites] Done! Check the Interactables Palette.");
    }
}

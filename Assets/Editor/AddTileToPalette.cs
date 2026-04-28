using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

/// <summary>
/// Appends a new tile to the existing Interactables Palette without rebuilding it.
/// </summary>
public static class AddTileToPalette
{
    public static void Execute()
    {
        string palettePath = "Assets/Tiles/Palettes/Interactables Palette.prefab";
        string tilePath = "Assets/Tiles/Interactables/ClosingPlatform.asset";

        var palettePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(palettePath);
        var tile = AssetDatabase.LoadAssetAtPath<SpawnTile>(tilePath);

        if (palettePrefab == null) { Debug.LogError("Palette not found!"); return; }
        if (tile == null) { Debug.LogError("ClosingPlatform tile not found!"); return; }

        // Open the prefab for editing
        var contents = PrefabUtility.LoadPrefabContents(palettePath);
        var tilemap = contents.GetComponentInChildren<Tilemap>();

        if (tilemap == null) { Debug.LogError("No tilemap in palette!"); PrefabUtility.UnloadPrefabContents(contents); return; }

        // Find the next empty slot (scan existing tiles to avoid overlap)
        int maxX = -1;
        BoundsInt bounds = tilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                if (tilemap.GetTile(new Vector3Int(x, y, 0)) != null)
                    maxX = Mathf.Max(maxX, x);
            }
        }

        // Place ClosingPlatform at the next slot
        int newX = maxX + 1;
        tilemap.SetTile(new Vector3Int(newX, 0, 0), tile);

        // Save and close
        PrefabUtility.SaveAsPrefabAsset(contents, palettePath);
        PrefabUtility.UnloadPrefabContents(contents);

        Debug.Log($"[AddTileToPalette] Added ClosingPlatform tile at position ({newX}, 0) in palette.");
    }
}

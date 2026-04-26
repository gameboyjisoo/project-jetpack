using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.IO;

/// <summary>
/// Creates SpawnTile assets for all interactable prefabs and builds
/// an "Interactables" tile palette so gates, pickups, and hazards
/// can be painted directly onto tilemaps.
/// </summary>
public static class CreateSpawnTiles
{
    struct TileDef
    {
        public string name;
        public string prefabPath;
        public string spritePath;
        public Color color;
    }

    static readonly TileDef[] Definitions =
    {
        new TileDef { name = "Hazard",         prefabPath = "Assets/Prefabs/Interactables/Hazard.prefab",         spritePath = "Assets/Sprites/Placeholders/Hazard.png",     color = new Color(1f, 0.35f, 0f) },
        new TileDef { name = "FuelPickup",     prefabPath = "Assets/Prefabs/Interactables/FuelPickup.prefab",     spritePath = "Assets/Sprites/Placeholders/FuelPickup.png", color = new Color(0f, 0.9f, 1f) },
        new TileDef { name = "DashPickup",     prefabPath = "Assets/Prefabs/Interactables/DashPickup.prefab",     spritePath = "Assets/Sprites/Placeholders/DashPickup.png", color = new Color(1f, 0f, 1f) },
        new TileDef { name = "FuelGate_High",  prefabPath = "Assets/Prefabs/Interactables/FuelGate_High.prefab",  spritePath = "Assets/Sprites/Placeholders/FuelGate.png",   color = new Color(0f, 0.9f, 1f) },
        new TileDef { name = "FuelGate_Mid",   prefabPath = "Assets/Prefabs/Interactables/FuelGate_Mid.prefab",   spritePath = "Assets/Sprites/Placeholders/FuelGate.png",   color = new Color(1f, 0.6f, 0f) },
        new TileDef { name = "FuelGate_Low",   prefabPath = "Assets/Prefabs/Interactables/FuelGate_Low.prefab",   spritePath = "Assets/Sprites/Placeholders/FuelGate.png",   color = new Color(1f, 0.15f, 0.15f) },
        new TileDef { name = "SpawnPoint",     prefabPath = "Assets/Prefabs/Interactables/SpawnPoint.prefab",     spritePath = "Assets/Sprites/Placeholders/SpawnPoint.png",  color = new Color(0f, 1f, 0f) },
        new TileDef { name = "Checkpoint",     prefabPath = "Assets/Prefabs/Interactables/Checkpoint.prefab",     spritePath = "Assets/Sprites/Placeholders/Checkpoint.png",  color = new Color(0.4f, 1f, 0.4f) },
    };

    public static void Execute()
    {
        CreateTileAssets();
        CreatePalette();
    }

    [MenuItem("Project Jetpack/Level Editor Setup/5 — Create Spawn Tiles and Palette")]
    static void CreateTileAssets()
    {
        string dir = "Assets/Tiles/Interactables";
        EnsureFolder(dir);

        foreach (var def in Definitions)
        {
            string tilePath = $"{dir}/{def.name}.asset";

            // Always recreate to pick up changes
            if (File.Exists(tilePath))
                AssetDatabase.DeleteAsset(tilePath);

            // Load the shape-coded placeholder sprite for this specific tile type
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(def.spritePath);
            if (sprite == null)
            {
                Debug.LogWarning($"[CreateSpawnTiles] Sprite not found: {def.spritePath}, falling back to GroundSprite");
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiles/GroundSprite.png");
            }

            var tile = ScriptableObject.CreateInstance<SpawnTile>();
            tile.editorSprite = sprite;
            tile.editorColor = def.color;
            tile.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(def.prefabPath);

            if (tile.prefab == null)
                Debug.LogWarning($"[CreateSpawnTiles] Prefab not found: {def.prefabPath}");

            AssetDatabase.CreateAsset(tile, tilePath);
            Debug.Log($"[CreateSpawnTiles] Created: {tilePath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void CreatePalette()
    {
        string palettesDir = "Assets/Tiles/Palettes";
        EnsureFolder(palettesDir);

        string palettePath = $"{palettesDir}/Interactables Palette.prefab";
        if (File.Exists(palettePath))
            AssetDatabase.DeleteAsset(palettePath);

        // Build palette: Grid > Layer1 (Tilemap)
        GameObject paletteGO = new GameObject("Interactables Palette");
        var grid = paletteGO.AddComponent<Grid>();
        grid.cellSize = new Vector3(1, 1, 0);

        GameObject layerGO = new GameObject("Layer1");
        layerGO.transform.SetParent(paletteGO.transform);
        var tilemap = layerGO.AddComponent<Tilemap>();
        layerGO.AddComponent<TilemapRenderer>();

        // Place tiles in a row for easy selection
        for (int i = 0; i < Definitions.Length; i++)
        {
            string tilePath = $"Assets/Tiles/Interactables/{Definitions[i].name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<SpawnTile>(tilePath);
            if (tile != null)
                tilemap.SetTile(new Vector3Int(i, 0, 0), tile);
        }

        // Save as prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(paletteGO, palettePath);
        Object.DestroyImmediate(paletteGO);

        // Attach GridPalette so Tile Palette window recognizes it
        var gridPalette = ScriptableObject.CreateInstance<GridPalette>();
        gridPalette.name = "GridPalette";
        gridPalette.cellSizing = GridPalette.CellSizing.Automatic;
        AssetDatabase.AddObjectToAsset(gridPalette, prefab);
        AssetDatabase.SaveAssets();

        Debug.Log($"[CreateSpawnTiles] Created palette: {palettePath} ({Definitions.Length} tiles)");
        Debug.Log("[CreateSpawnTiles] Done! Select 'Interactables Palette' in Window > 2D > Tile Palette.");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string folder = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }
}

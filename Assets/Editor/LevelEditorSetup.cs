using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// One-time setup: fixes tileset imports, slices into 16×16 grids,
/// creates Tile assets, builds Tile Palettes, and creates interactable prefabs.
/// Run via Project Jetpack menu.
/// </summary>
public static class LevelEditorSetup
{
    static readonly string[] TilesetPaths =
    {
        "Assets/Sprites/Tiles/PrtCave.png",
        "Assets/Sprites/Tiles/PrtMimi.png",
        "Assets/Sprites/Tiles/PrtOside.png",
        "Assets/Sprites/Tiles/PrtFall.png",
        "Assets/Sprites/Tiles/PrtHell.png",
    };

    // ───────────────────────────────────────────────────────────────
    // Step 1: Fix import settings and slice all tilesets into 16×16
    // Uses Unity 6 ISpriteEditorDataProvider (spritesheet is deprecated)
    // ───────────────────────────────────────────────────────────────
    [MenuItem("Project Jetpack/Level Editor Setup/1 — Fix Imports and Slice Tilesets")]
    public static void FixImportAndSlice()
    {
        var factory = new SpriteDataProviderFactories();
        factory.Init();

        foreach (var path in TilesetPaths)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[LevelEditorSetup] Tileset not found: {path}");
                continue;
            }

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            // Fix settings: pixel art, 16 PPU, Point filter, Multiple sprite mode
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.SaveAndReimport();

            // Read texture dimensions
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            int w = tex.width;
            int h = tex.height;
            int cols = w / 16;
            int rows = h / 16;
            string tilesetName = Path.GetFileNameWithoutExtension(path);

            // Use ISpriteEditorDataProvider to set sprite rects (Unity 6 API)
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();

            var spriteRects = new List<SpriteRect>();
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int y = h - (row + 1) * 16;
                    var sr = new SpriteRect();
                    sr.name = $"{tilesetName}_{row}_{col}";
                    sr.rect = new Rect(col * 16, y, 16, 16);
                    sr.alignment = SpriteAlignment.Center;
                    sr.pivot = new Vector2(0.5f, 0.5f);
                    spriteRects.Add(sr);
                }
            }

            dataProvider.SetSpriteRects(spriteRects.ToArray());
            dataProvider.Apply();

            // Reimport with the new sprite data
            AssetDatabase.ForceReserializeAssets(new[] { path });
            importer.SaveAndReimport();

            Debug.Log($"[LevelEditorSetup] Sliced {tilesetName}: {cols}×{rows} = {spriteRects.Count} tiles ({w}×{h} px)");
        }

        AssetDatabase.Refresh();
        Debug.Log("[LevelEditorSetup] ✓ All tilesets imported and sliced.");
    }

    // ───────────────────────────────────────────────────────────────
    // Step 2: Create Tile ScriptableObjects from sliced sprites
    // ───────────────────────────────────────────────────────────────
    [MenuItem("Project Jetpack/Level Editor Setup/2 — Create Tile Assets")]
    public static void CreateTileAssets()
    {
        EnsureFolder("Assets/Tiles");

        int totalCreated = 0;

        foreach (var path in TilesetPaths)
        {
            if (!File.Exists(path)) continue;

            string tilesetName = Path.GetFileNameWithoutExtension(path);
            string tileDir = $"Assets/Tiles/{tilesetName}";
            EnsureFolder(tileDir);

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            int count = 0;

            foreach (var asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    string tilePath = $"{tileDir}/{sprite.name}.asset";
                    if (File.Exists(tilePath)) continue;

                    Tile tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = sprite;
                    tile.color = Color.white;
                    tile.colliderType = Tile.ColliderType.Grid;

                    AssetDatabase.CreateAsset(tile, tilePath);
                    count++;
                }
            }

            totalCreated += count;
            Debug.Log($"[LevelEditorSetup] Created {count} tile assets in {tileDir}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[LevelEditorSetup] ✓ {totalCreated} tile assets created total.");
    }

    // ───────────────────────────────────────────────────────────────
    // Step 3: Create Tile Palettes (one per tileset)
    // ───────────────────────────────────────────────────────────────
    [MenuItem("Project Jetpack/Level Editor Setup/3 — Create Tile Palettes")]
    public static void CreateTilePalettes()
    {
        string palettesDir = "Assets/Tiles/Palettes";
        EnsureFolder(palettesDir);

        foreach (var path in TilesetPaths)
        {
            if (!File.Exists(path)) continue;

            string tilesetName = Path.GetFileNameWithoutExtension(path);
            string tileDir = $"Assets/Tiles/{tilesetName}";
            string palettePath = $"{palettesDir}/{tilesetName} Palette.prefab";

            if (File.Exists(palettePath))
            {
                Debug.Log($"[LevelEditorSetup] Palette already exists: {palettePath}");
                continue;
            }

            // Load the source texture to get grid layout
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) continue;

            int cols = tex.width / 16;
            int rows = tex.height / 16;

            // Create palette GameObject hierarchy: Grid > Layer1 (Tilemap)
            GameObject paletteGO = new GameObject(tilesetName + " Palette");
            var grid = paletteGO.AddComponent<Grid>();
            grid.cellSize = new Vector3(1, 1, 0);

            GameObject layerGO = new GameObject("Layer1");
            layerGO.transform.SetParent(paletteGO.transform);
            var tilemap = layerGO.AddComponent<Tilemap>();
            layerGO.AddComponent<TilemapRenderer>();

            // Place tiles mirroring the spritesheet layout
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            int placed = 0;
            foreach (var asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    string tilAssetPath = $"{tileDir}/{sprite.name}.asset";
                    Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilAssetPath);
                    if (tile == null) continue;

                    // sprite.rect origin is bottom-left; palette keeps same layout
                    int col = Mathf.FloorToInt(sprite.rect.x / 16);
                    int row = Mathf.FloorToInt(sprite.rect.y / 16);
                    tilemap.SetTile(new Vector3Int(col, row, 0), tile);
                    placed++;
                }
            }

            // Save as prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(paletteGO, palettePath);
            Object.DestroyImmediate(paletteGO);

            // Attach GridPalette sub-asset so the Tile Palette window recognizes it
            var gridPalette = ScriptableObject.CreateInstance<GridPalette>();
            gridPalette.name = "GridPalette";
            gridPalette.cellSizing = GridPalette.CellSizing.Automatic;
            AssetDatabase.AddObjectToAsset(gridPalette, prefab);
            AssetDatabase.SaveAssets();

            Debug.Log($"[LevelEditorSetup] Created palette: {palettePath} ({placed} tiles)");
        }

        AssetDatabase.Refresh();
        Debug.Log("[LevelEditorSetup] ✓ All tile palettes created. Open Window > 2D > Tile Palette.");
    }

    // ───────────────────────────────────────────────────────────────
    // Step 4: Create interactable prefabs (Hazard, Pickups, Gates)
    // ───────────────────────────────────────────────────────────────
    [MenuItem("Project Jetpack/Level Editor Setup/4 — Create Interactable Prefabs")]
    public static void CreatePrefabs()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/Interactables");
        string dir = "Assets/Prefabs/Interactables";

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiles/GroundSprite.png");

        // --- Hazard (spike strip) ---
        MakePrefab("Hazard", dir, sprite, new Color(1f, 0.35f, 0f), go =>
        {
            go.layer = 10; // Hazard
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            go.AddComponent<Hazard>();
        });

        // --- Fuel Pickup ---
        MakePrefab("FuelPickup", dir, sprite, new Color(0f, 0.9f, 1f), go =>
        {
            go.layer = 11; // Collectible
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;
            go.AddComponent<GasRechargePickup>();
        });

        // --- Dash Pickup ---
        MakePrefab("DashPickup", dir, sprite, new Color(1f, 0f, 1f), go =>
        {
            go.layer = 11; // Collectible
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;
            go.AddComponent<DashRechargePickup>();
        });

        // --- Fuel Gate (High — cyan) ---
        MakePrefab("FuelGate_High", dir, sprite, new Color(0f, 0.9f, 1f), go =>
        {
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);
            var gate = go.AddComponent<FuelGate>();
            var so = new SerializedObject(gate);
            var prop = so.FindProperty("requiredTier");
            if (prop != null) { prop.enumValueIndex = (int)FuelTier.High; so.ApplyModifiedPropertiesWithoutUndo(); }
        });

        // --- Fuel Gate (Mid — orange) ---
        MakePrefab("FuelGate_Mid", dir, sprite, new Color(1f, 0.6f, 0f), go =>
        {
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);
            var gate = go.AddComponent<FuelGate>();
            var so = new SerializedObject(gate);
            var prop = so.FindProperty("requiredTier");
            if (prop != null) { prop.enumValueIndex = (int)FuelTier.Mid; so.ApplyModifiedPropertiesWithoutUndo(); }
        });

        // --- Fuel Gate (Low — red) ---
        MakePrefab("FuelGate_Low", dir, sprite, new Color(1f, 0.15f, 0.15f), go =>
        {
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);
            var gate = go.AddComponent<FuelGate>();
            // Default is already Low, no change needed
        });

        // --- Spawn Point (editor-only marker) ---
        MakePrefab("SpawnPoint", dir, null, Color.white, go =>
        {
            // Empty transform — just a position marker.
            // Draw a gizmo via a tiny component for editor visibility.
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[LevelEditorSetup] ✓ All prefabs created in Assets/Prefabs/Interactables/");
    }

    // ───────────────────────────────────────────────────────────────
    // Run everything in order
    // ───────────────────────────────────────────────────────────────
    [MenuItem("Project Jetpack/Level Editor Setup/Run All Steps")]
    public static void RunAll()
    {
        // Clean up empty palettes from previous failed runs
        CleanEmptyPalettes();
        FixImportAndSlice();
        CreateTileAssets();
        CreateTilePalettes();
        CreatePrefabs();
        Debug.Log("═══════════════════════════════════════════════");
        Debug.Log("[LevelEditorSetup] ALL DONE. Level editor is ready!");
        Debug.Log("  • Open Window > 2D > Tile Palette to paint rooms");
        Debug.Log("  • Drag prefabs from Assets/Prefabs/Interactables/");
        Debug.Log("  • Use Project Jetpack > New Room to create room shells");
        Debug.Log("═══════════════════════════════════════════════");
    }

    static void CleanEmptyPalettes()
    {
        string palettesDir = "Assets/Tiles/Palettes";
        if (!AssetDatabase.IsValidFolder(palettesDir)) return;

        foreach (var path in TilesetPaths)
        {
            string tilesetName = Path.GetFileNameWithoutExtension(path);
            string palettePath = $"{palettesDir}/{tilesetName} Palette.prefab";
            if (File.Exists(palettePath))
            {
                AssetDatabase.DeleteAsset(palettePath);
                Debug.Log($"[LevelEditorSetup] Deleted old palette: {palettePath}");
            }
        }
    }

    // ───────────────────────────────────────────────────────────────
    // Helpers
    // ───────────────────────────────────────────────────────────────
    static void MakePrefab(string name, string dir, Sprite sprite, Color color, System.Action<GameObject> configure)
    {
        string path = $"{dir}/{name}.prefab";
        if (File.Exists(path))
        {
            Debug.Log($"[LevelEditorSetup] Prefab exists, skipping: {path}");
            return;
        }

        GameObject go = new GameObject(name);
        if (sprite != null)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
        }

        configure(go);
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log($"[LevelEditorSetup] Created prefab: {path}");
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

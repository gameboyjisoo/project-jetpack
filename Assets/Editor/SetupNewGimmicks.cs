using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// One-shot setup: creates GravitySwitch (4 directions) + Crusher assets.
/// Sprites, prefabs, SpawnTiles, and palette entries.
/// DOES NOT rebuild existing tiles or palette — only appends new entries.
/// </summary>
public static class SetupNewGimmicks
{
    const int S = 16;
    static readonly string SpriteDir = "Assets/Sprites/Placeholders";
    static readonly string PrefabDir = "Assets/Prefabs/Interactables";
    static readonly string TileDir = "Assets/Tiles/Interactables";
    static readonly string PalettePath = "Assets/Tiles/Palettes/Interactables Palette.prefab";

    [MenuItem("Project Jetpack/Setup/Create Gravity Switch + Crusher Assets")]
    public static void Execute()
    {
        CreateCrusherSprite();
        CreateGravitySwitchPrefabs();
        CreateCrusherPrefab();
        CreateSpawnTileAssets();
        AppendToPalette();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SetupNewGimmicks] Done! 4 GravitySwitch + 1 Crusher added to palette.");
    }

    // ── Crusher Sprite ──────────────────────────────────────────────────────

    static void CreateCrusherSprite()
    {
        string path = $"{SpriteDir}/Crusher.png";
        if (File.Exists(path)) return; // don't overwrite

        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var clear = new Color(0, 0, 0, 0);

        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                // Heavy block with jagged bottom edge (teeth)
                bool filled;
                if (y >= 4)
                {
                    // Solid block top portion
                    filled = true;
                }
                else
                {
                    // Jagged teeth at bottom: 4px-wide triangles pointing down
                    int toothX = x % 4;
                    int toothCenter = 2;
                    int dist = Mathf.Abs(toothX - toothCenter);
                    filled = y >= dist;
                }
                tex.SetPixel(x, y, filled ? Color.white : clear);
            }
        }

        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.Refresh();

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        Debug.Log("[SetupNewGimmicks] Created Crusher placeholder sprite.");
    }

    // ── Gravity Switch Prefabs ──────────────────────────────────────────────

    static readonly (string name, GravityDir dir, Color color)[] GravityDefs =
    {
        ("GravitySwitch_Down",  GravityDir.Down,  new Color(1f, 1f, 1f, 0.8f)),
        ("GravitySwitch_Up",    GravityDir.Up,    new Color(0.3f, 0.8f, 1f, 0.8f)),
        ("GravitySwitch_Left",  GravityDir.Left,  new Color(1f, 0.6f, 0.2f, 0.8f)),
        ("GravitySwitch_Right", GravityDir.Right, new Color(0.6f, 1f, 0.3f, 0.8f)),
    };

    static void CreateGravitySwitchPrefabs()
    {
        // Reuse SpawnPoint arrow sprite for all gravity switches
        var arrowSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDir}/SpawnPoint.png");

        foreach (var (name, dir, color) in GravityDefs)
        {
            string prefabPath = $"{PrefabDir}/{name}.prefab";
            if (File.Exists(prefabPath)) continue; // don't overwrite

            var go = new GameObject(name);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = arrowSprite;
            sr.color = color;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 1f);

            var gs = go.AddComponent<GravitySwitch>();
            // Set targetDirection via SerializedObject so it persists in the prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            // Set the direction field on the saved prefab asset
            var so = new SerializedObject(prefab.GetComponent<GravitySwitch>());
            var dirProp = so.FindProperty("targetDirection");
            if (dirProp != null)
            {
                dirProp.enumValueIndex = (int)dir;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            Debug.Log($"[SetupNewGimmicks] Created prefab: {prefabPath}");
        }
    }

    // ── Crusher Prefab ──────────────────────────────────────────────────────

    static void CreateCrusherPrefab()
    {
        string prefabPath = $"{PrefabDir}/Crusher.prefab";
        if (File.Exists(prefabPath)) return;

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDir}/Crusher.png");

        var go = new GameObject("Crusher");
        go.layer = 8; // Ground

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.6f, 0.2f, 0.2f, 1f);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(4f, 1f);

        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(4f, 1f);

        go.AddComponent<Crusher>();

        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);

        Debug.Log($"[SetupNewGimmicks] Created prefab: {prefabPath}");
    }

    // ── SpawnTile Assets ────────────────────────────────────────────────────

    struct TileInfo
    {
        public string name;
        public string prefabPath;
        public string spritePath;
        public Color color;
    }

    static void CreateSpawnTileAssets()
    {
        var arrowSpritePath = $"{SpriteDir}/SpawnPoint.png";
        var crusherSpritePath = $"{SpriteDir}/Crusher.png";

        var tiles = new List<TileInfo>();

        foreach (var (name, dir, color) in GravityDefs)
        {
            tiles.Add(new TileInfo
            {
                name = name,
                prefabPath = $"{PrefabDir}/{name}.prefab",
                spritePath = arrowSpritePath,
                color = color
            });
        }

        tiles.Add(new TileInfo
        {
            name = "Crusher",
            prefabPath = $"{PrefabDir}/Crusher.prefab",
            spritePath = crusherSpritePath,
            color = new Color(0.6f, 0.2f, 0.2f)
        });

        foreach (var tile in tiles)
        {
            string tilePath = $"{TileDir}/{tile.name}.asset";
            if (File.Exists(tilePath)) continue; // don't overwrite

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(tile.spritePath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(tile.prefabPath);

            var spawnTile = ScriptableObject.CreateInstance<SpawnTile>();
            spawnTile.editorSprite = sprite;
            spawnTile.editorColor = tile.color;
            spawnTile.prefab = prefab;

            AssetDatabase.CreateAsset(spawnTile, tilePath);
            Debug.Log($"[SetupNewGimmicks] Created tile: {tilePath}");
        }
    }

    // ── Palette Append (SAFE — no rebuild) ──────────────────────────────────

    static void AppendToPalette()
    {
        var palettePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath);
        if (palettePrefab == null)
        {
            Debug.LogError("[SetupNewGimmicks] Palette not found!");
            return;
        }

        var contents = PrefabUtility.LoadPrefabContents(PalettePath);
        var tilemap = contents.GetComponentInChildren<Tilemap>();
        if (tilemap == null)
        {
            Debug.LogError("[SetupNewGimmicks] No tilemap in palette!");
            PrefabUtility.UnloadPrefabContents(contents);
            return;
        }

        // Find the next empty X slot
        int maxX = -1;
        BoundsInt bounds = tilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax + 10; x++)
        {
            if (tilemap.GetTile(new Vector3Int(x, 0, 0)) != null)
                maxX = x;
        }

        string[] newTileNames = {
            "GravitySwitch_Down", "GravitySwitch_Up",
            "GravitySwitch_Left", "GravitySwitch_Right",
            "Crusher"
        };

        int nextX = maxX + 1;
        int added = 0;

        foreach (var name in newTileNames)
        {
            var tile = AssetDatabase.LoadAssetAtPath<SpawnTile>($"{TileDir}/{name}.asset");
            if (tile == null)
            {
                Debug.LogWarning($"[SetupNewGimmicks] Tile not found: {name}");
                continue;
            }

            // Check if already in palette
            bool alreadyExists = false;
            for (int x = bounds.xMin; x <= maxX; x++)
            {
                if (tilemap.GetTile(new Vector3Int(x, 0, 0)) == tile)
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (alreadyExists)
            {
                Debug.Log($"[SetupNewGimmicks] {name} already in palette, skipping.");
                continue;
            }

            tilemap.SetTile(new Vector3Int(nextX, 0, 0), tile);
            nextX++;
            added++;
        }

        PrefabUtility.SaveAsPrefabAsset(contents, PalettePath);
        PrefabUtility.UnloadPrefabContents(contents);

        Debug.Log($"[SetupNewGimmicks] Appended {added} new tiles to palette.");
    }
}

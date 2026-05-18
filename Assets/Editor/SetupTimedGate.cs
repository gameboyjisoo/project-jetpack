using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.IO;

/// <summary>
/// One-shot setup: creates TimedGate placeholder sprite, prefab, SpawnTile, and palette entry.
/// DOES NOT rebuild existing tiles or palette — only appends.
/// Run via: Project Jetpack > Setup > Create Timed Gate Assets
/// </summary>
public static class SetupTimedGate
{
    const int S = 16;
    static readonly string SpriteDir = "Assets/Sprites/Placeholders";
    static readonly string PrefabDir = "Assets/Prefabs/Interactables";
    static readonly string TileDir = "Assets/Tiles/Interactables";
    static readonly string PalettePath = "Assets/Tiles/Palettes/Interactables Palette.prefab";

    static readonly Color GateColor = new Color(0.6f, 0.4f, 0.8f, 1f); // Purple

    [MenuItem("Project Jetpack/Setup/Create Timed Gate Assets")]
    public static void Execute()
    {
        CreateSprite();
        CreatePrefab();
        CreateSpawnTileAsset();
        AppendToPalette();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SetupTimedGate] Done! TimedGate added to palette.");
    }

    // ── Placeholder Sprite ─────────────────────────────────────────────────

    static void CreateSprite()
    {
        string path = $"{SpriteDir}/TimedGate.png";
        if (File.Exists(path)) return;

        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var clear = new Color(0, 0, 0, 0);

        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                // Hourglass shape: solid borders + hourglass center
                bool isBorder = x == 0 || x == S - 1 || y == 0 || y == S - 1;
                // Hourglass: narrow in middle, wide at top/bottom
                int halfH = S / 2;
                int distFromCenter = Mathf.Abs(y - halfH);
                int narrowing = (halfH - distFromCenter) / 2;
                bool isHourglass = x >= narrowing && x < S - narrowing;

                tex.SetPixel(x, y, (isBorder || isHourglass) ? Color.white : clear);
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

        Debug.Log("[SetupTimedGate] Created placeholder sprite.");
    }

    // ── Prefab ─────────────────────────────────────────────────────────────

    static void CreatePrefab()
    {
        string prefabPath = $"{PrefabDir}/TimedGate.prefab";
        if (File.Exists(prefabPath)) return;

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDir}/TimedGate.png");

        var go = new GameObject("TimedGate");
        go.layer = 8; // Ground — player can stand on closed gates

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = GateColor;

        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1f, 1f);

        go.AddComponent<TimedGate>();

        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);

        Debug.Log($"[SetupTimedGate] Created prefab: {prefabPath}");
    }

    // ── SpawnTile Asset ────────────────────────────────────────────────────

    static void CreateSpawnTileAsset()
    {
        string tilePath = $"{TileDir}/TimedGate.asset";
        if (File.Exists(tilePath)) return;

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDir}/TimedGate.png");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/TimedGate.prefab");

        var spawnTile = ScriptableObject.CreateInstance<SpawnTile>();
        spawnTile.editorSprite = sprite;
        spawnTile.editorColor = GateColor;
        spawnTile.prefab = prefab;

        AssetDatabase.CreateAsset(spawnTile, tilePath);
        Debug.Log($"[SetupTimedGate] Created tile: {tilePath}");
    }

    // ── Palette Append ─────────────────────────────────────────────────────

    static void AppendToPalette()
    {
        var palettePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath);
        if (palettePrefab == null)
        {
            Debug.LogError("[SetupTimedGate] Palette not found!");
            return;
        }

        var contents = PrefabUtility.LoadPrefabContents(PalettePath);
        var tilemap = contents.GetComponentInChildren<Tilemap>();
        if (tilemap == null)
        {
            Debug.LogError("[SetupTimedGate] No tilemap in palette!");
            PrefabUtility.UnloadPrefabContents(contents);
            return;
        }

        var tile = AssetDatabase.LoadAssetAtPath<SpawnTile>($"{TileDir}/TimedGate.asset");
        if (tile == null)
        {
            Debug.LogError("[SetupTimedGate] TimedGate tile asset not found!");
            PrefabUtility.UnloadPrefabContents(contents);
            return;
        }

        // Check if already in palette
        BoundsInt bounds = tilemap.cellBounds;
        int maxX = -1;
        for (int x = bounds.xMin; x < bounds.xMax + 10; x++)
        {
            var existing = tilemap.GetTile(new Vector3Int(x, 0, 0));
            if (existing != null)
            {
                maxX = x;
                if (existing == tile)
                {
                    Debug.Log("[SetupTimedGate] TimedGate already in palette, skipping.");
                    PrefabUtility.UnloadPrefabContents(contents);
                    return;
                }
            }
        }

        tilemap.SetTile(new Vector3Int(maxX + 1, 0, 0), tile);

        PrefabUtility.SaveAsPrefabAsset(contents, PalettePath);
        PrefabUtility.UnloadPrefabContents(contents);

        Debug.Log($"[SetupTimedGate] Appended TimedGate to palette at position ({maxX + 1}, 0).");
    }
}

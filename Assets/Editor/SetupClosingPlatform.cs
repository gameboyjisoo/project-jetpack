using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// One-shot setup: creates ClosingPlatform placeholder sprite, prefab, SpawnTile, and rebuilds palette.
/// Run via: Project Jetpack > Setup > Create Closing Platform Assets
/// </summary>
public static class SetupClosingPlatform
{
    [MenuItem("Project Jetpack/Setup/Create Closing Platform Assets")]
    public static void Execute()
    {
        CreateSprite();
        CreatePrefab();
        CreateSpawnTiles.Execute(); // Rebuilds all tiles + palette including ClosingPlatform
        Debug.Log("[SetupClosingPlatform] Done! ClosingPlatform sprite, prefab, tile, and palette updated.");
    }

    static void CreateSprite()
    {
        string dir = "Assets/Sprites/Placeholders";
        EnsureFolder(dir);

        int S = 16;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        var clear = new Color(0, 0, 0, 0);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                // Horizontal slats: 2px bar, 1px gap, alternating inset
                int row = y % 3;
                bool filled = false;
                if (row <= 1)
                {
                    int barIndex = y / 3;
                    int inset = (barIndex % 2 == 0) ? 0 : 2;
                    filled = x >= inset && x < S - inset;
                }
                tex.SetPixel(x, y, filled ? Color.white : clear);
            }
        }

        tex.Apply();
        string path = $"{dir}/ClosingPlatform.png";
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

            // Full Rect required for SpriteDrawMode.Tiled
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }

        Debug.Log("[SetupClosingPlatform] Created placeholder sprite.");
    }

    static void CreatePrefab()
    {
        string dir = "Assets/Prefabs/Interactables";
        EnsureFolder(dir);

        string prefabPath = $"{dir}/ClosingPlatform.prefab";

        // Build the GameObject
        var go = new GameObject("ClosingPlatform");
        go.layer = 8; // Ground

        // SpriteRenderer — tiled draw mode so it can be any size
        var sr = go.AddComponent<SpriteRenderer>();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Placeholders/ClosingPlatform.png");
        if (sprite != null) sr.sprite = sprite;
        sr.color = new Color(0.85f, 0.55f, 0.1f, 1f);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(4f, 1f); // Default: 4 tiles wide, 1 tile tall

        // BoxCollider2D sized to match
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(4f, 1f);

        // ClosingPlatform script
        go.AddComponent<ClosingPlatform>();

        // Save as prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);

        Debug.Log($"[SetupClosingPlatform] Created prefab: {prefabPath}");
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

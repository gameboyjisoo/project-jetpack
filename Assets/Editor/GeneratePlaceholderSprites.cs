using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Generates simple 16×16 shape-coded placeholder sprites for interactable tiles.
/// Each type gets a distinct silhouette filling the full tile.
/// </summary>
public static class GeneratePlaceholderSprites
{
    const int S = 16; // tile size
    static readonly string OutputDir = "Assets/Sprites/Placeholders";

    public static void Execute()
    {
        EnsureFolder(OutputDir);

        // Hazard: row of spikes filling the full tile (like a spike trap floor)
        CreateSprite("Hazard", (x, y) =>
        {
            // 4 full-width triangular spikes, base at y=0, tips at y=15
            // Each spike is 4px wide: [0-3], [4-7], [8-11], [12-15]
            int spikeX = x % 4;        // position within this spike (0-3)
            int center = 2;             // center of each 4px spike
            int distFromCenter = Mathf.Abs(spikeX - center);
            // Max height at center (y up to 15), narrows toward edges
            int maxY = 15 - distFromCenter * 5;
            return y <= maxY;
        });

        // FuelPickup: large filled circle, nearly full tile
        CreateSprite("FuelPickup", (x, y) =>
        {
            float cx = 7.5f, cy = 7.5f, r = 7.5f;
            float dx = x - cx, dy = y - cy;
            return dx * dx + dy * dy <= r * r;
        });

        // DashPickup: large diamond filling the tile
        CreateSprite("DashPickup", (x, y) =>
        {
            float cx = 7.5f, cy = 7.5f;
            float dist = Mathf.Abs(x - cx) + Mathf.Abs(y - cy);
            return dist <= 8f;
        });

        // FuelGate: full-tile vertical bars (jail/barrier look)
        CreateSprite("FuelGate", (x, y) =>
        {
            // Top and bottom solid bars (full width)
            if (y <= 1 || y >= 14) return true;
            // 5 evenly-spaced vertical bars filling the tile
            if (x == 1 || x == 4 || x == 7 || x == 10 || x == 13) return true;
            if (x == 2 || x == 5 || x == 8 || x == 11 || x == 14) return true;
            return false;
        });

        // SpawnPoint: large downward arrow filling the tile
        CreateSprite("SpawnPoint", (x, y) =>
        {
            // Shaft: wide, top portion
            if (x >= 5 && x <= 10 && y >= 8 && y <= 15) return true;
            // Arrowhead: triangle pointing down, full width
            if (y < 8)
            {
                int halfWidth = y; // wider at top (y=7), narrower at bottom (y=0)
                int cx = 7;
                if (x >= cx - halfWidth && x <= cx + halfWidth + 1) return true;
            }
            return false;
        });

        AssetDatabase.Refresh();

        // Fix import settings
        string[] sprites = { "Hazard", "FuelPickup", "DashPickup", "FuelGate", "SpawnPoint" };
        foreach (var name in sprites)
        {
            string path = $"{OutputDir}/{name}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        Debug.Log("[GeneratePlaceholderSprites] Created 5 placeholder sprites in " + OutputDir);
        UpdateSpawnTiles();
        RebuildPalette();
    }

    static void CreateSprite(string name, System.Func<int, int, bool> pixelTest)
    {
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        var clear = new Color(0, 0, 0, 0);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
                tex.SetPixel(x, y, pixelTest(x, y) ? Color.white : clear);

        tex.Apply();
        File.WriteAllBytes($"{OutputDir}/{name}.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    static void UpdateSpawnTiles()
    {
        var updates = new (string tilePath, string spritePath)[]
        {
            ("Assets/Tiles/Interactables/Hazard.asset",        $"{OutputDir}/Hazard.png"),
            ("Assets/Tiles/Interactables/FuelPickup.asset",    $"{OutputDir}/FuelPickup.png"),
            ("Assets/Tiles/Interactables/DashPickup.asset",    $"{OutputDir}/DashPickup.png"),
            ("Assets/Tiles/Interactables/FuelGate_High.asset", $"{OutputDir}/FuelGate.png"),
            ("Assets/Tiles/Interactables/FuelGate_Mid.asset",  $"{OutputDir}/FuelGate.png"),
            ("Assets/Tiles/Interactables/FuelGate_Low.asset",  $"{OutputDir}/FuelGate.png"),
            ("Assets/Tiles/Interactables/SpawnPoint.asset",    $"{OutputDir}/SpawnPoint.png"),
        };

        foreach (var (tilePath, spritePath) in updates)
        {
            var tile = AssetDatabase.LoadAssetAtPath<SpawnTile>(tilePath);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (tile != null && sprite != null)
            {
                tile.editorSprite = sprite;
                EditorUtility.SetDirty(tile);
            }
        }
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Delete and recreate the Interactables Palette from scratch to avoid ghost overlap.
    /// </summary>
    static void RebuildPalette()
    {
        string palettePath = "Assets/Tiles/Palettes/Interactables Palette.prefab";
        if (File.Exists(palettePath))
            AssetDatabase.DeleteAsset(palettePath);

        // Rebuild via CreateSpawnTiles (which handles palette creation)
        CreateSpawnTiles.Execute();
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

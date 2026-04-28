using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Creates 4 directional arrow sprites for GravitySwitch tiles and updates
/// the existing SpawnTile assets to reference them. Does NOT rebuild palette.
/// </summary>
public static class FixGravityArrows
{
    const int S = 16;
    static readonly string Dir = "Assets/Sprites/Placeholders";

    [MenuItem("Project Jetpack/Setup/Fix Gravity Arrow Sprites")]
    public static void Execute()
    {
        CreateArrowSprites();
        UpdateTileAssets();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FixGravityArrows] Created 4 directional arrows and updated tile assets.");
    }

    static void CreateArrowSprites()
    {
        // Generate a down-pointing arrow in a bool grid, then rotate for each direction
        bool[,] downArrow = new bool[S, S];
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                // Shaft: wide, top portion (y 8-15)
                if (x >= 5 && x <= 10 && y >= 8 && y <= 15)
                    downArrow[x, y] = true;
                // Arrowhead: triangle pointing down (y 0-7)
                if (y < 8)
                {
                    int halfWidth = y;
                    int cx = 7;
                    if (x >= cx - halfWidth && x <= cx + halfWidth + 1)
                        downArrow[x, y] = true;
                }
            }
        }

        WriteSprite("GravityArrow_Down", downArrow, false, false);
        WriteSprite("GravityArrow_Up", downArrow, false, true);   // flip Y
        WriteSprite("GravityArrow_Right", downArrow, true, false); // rotate 90 CW
        WriteSprite("GravityArrow_Left", downArrow, true, true);   // rotate 90 CCW
    }

    static void WriteSprite(string name, bool[,] source, bool rotate, bool flip)
    {
        string path = $"{Dir}/{name}.png";

        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var clear = new Color(0, 0, 0, 0);

        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                int sx, sy;
                if (!rotate)
                {
                    sx = x;
                    sy = flip ? (S - 1 - y) : y;
                }
                else
                {
                    // Rotate 90: (x,y) <- (y, S-1-x) for CW
                    if (!flip)
                    {
                        sx = y;
                        sy = S - 1 - x;
                    }
                    else
                    {
                        // Rotate 90 CCW: (x,y) <- (S-1-y, x)
                        sx = S - 1 - y;
                        sy = x;
                    }
                }

                bool filled = sx >= 0 && sx < S && sy >= 0 && sy < S && source[sx, sy];
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
            importer.SaveAndReimport();
        }
    }

    static void UpdateTileAssets()
    {
        var updates = new (string tileName, string spriteName)[]
        {
            ("GravitySwitch_Down",  "GravityArrow_Down"),
            ("GravitySwitch_Up",    "GravityArrow_Up"),
            ("GravitySwitch_Left",  "GravityArrow_Left"),
            ("GravitySwitch_Right", "GravityArrow_Right"),
        };

        foreach (var (tileName, spriteName) in updates)
        {
            string tilePath = $"Assets/Tiles/Interactables/{tileName}.asset";
            string spritePath = $"{Dir}/{spriteName}.png";

            var tile = AssetDatabase.LoadAssetAtPath<SpawnTile>(tilePath);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            if (tile == null) { Debug.LogWarning($"Tile not found: {tilePath}"); continue; }
            if (sprite == null) { Debug.LogWarning($"Sprite not found: {spritePath}"); continue; }

            // Update via SerializedObject for reliable persistence
            var so = new SerializedObject(tile);
            var spriteProp = so.FindProperty("editorSprite");
            if (spriteProp != null)
            {
                spriteProp.objectReferenceValue = sprite;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(tile);
            Debug.Log($"[FixGravityArrows] Updated {tileName} -> {spriteName}");
        }
    }
}

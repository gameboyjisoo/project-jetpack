using UnityEngine;
using UnityEditor;
using System.IO;

public static class CreateCheckpointTile
{
    public static void Execute()
    {
        // 1. Generate a checkpoint placeholder sprite (flag shape)
        string spriteDir = "Assets/Sprites/Placeholders";
        string spritePath = $"{spriteDir}/Checkpoint.png";

        if (!File.Exists(spritePath))
        {
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    bool pixel = false;
                    // Vertical pole on the left
                    if (x >= 2 && x <= 3 && y >= 0 && y <= 15) pixel = true;
                    // Flag triangle on upper-right
                    if (x >= 4 && x <= 4 + (15 - y) && y >= 8 && y <= 15) pixel = true;

                    tex.SetPixel(x, y, pixel ? Color.white : clear);
                }

            tex.Apply();
            File.WriteAllBytes(spritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.Refresh();

            var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
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

        // 2. Create the Checkpoint prefab
        string prefabDir = "Assets/Prefabs/Interactables";
        string prefabPath = $"{prefabDir}/Checkpoint.prefab";

        if (!File.Exists(prefabPath))
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            var go = new GameObject("Checkpoint");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.4f, 1f, 0.4f, 0.8f);

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            go.AddComponent<Checkpoint>();

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log($"[CreateCheckpointTile] Created prefab: {prefabPath}");
        }

        // 3. Create the SpawnTile asset
        string tileDir = "Assets/Tiles/Interactables";
        string tilePath = $"{tileDir}/Checkpoint.asset";

        if (File.Exists(tilePath))
            AssetDatabase.DeleteAsset(tilePath);

        var checkpointSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        var checkpointPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        var tile = ScriptableObject.CreateInstance<SpawnTile>();
        tile.editorSprite = checkpointSprite;
        tile.editorColor = new Color(0.4f, 1f, 0.4f, 0.8f);
        tile.prefab = checkpointPrefab;

        AssetDatabase.CreateAsset(tile, tilePath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[CreateCheckpointTile] Created tile: {tilePath}");

        // 4. Rebuild Interactables palette to include the new tile
        string palettePath = "Assets/Tiles/Palettes/Interactables Palette.prefab";
        if (File.Exists(palettePath))
            AssetDatabase.DeleteAsset(palettePath);

        CreateSpawnTiles.Execute();

        Debug.Log("[CreateCheckpointTile] Done! Checkpoint added to Interactables Palette.");
    }
}

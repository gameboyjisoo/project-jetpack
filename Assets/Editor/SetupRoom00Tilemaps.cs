using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class SetupRoom00Tilemaps
{
    [MenuItem("Project Jetpack/Setup Room 00 Tilemaps")]
    public static void Execute()
    {
        var wallsObj = GameObject.Find("ch1-Room-00/Grid/Walls");
        var interObj = GameObject.Find("ch1-Room-00/Grid/Interactables");

        if (wallsObj == null || interObj == null)
        {
            Debug.LogError("Could not find ch1-Room-00/Grid/Walls or Interactables");
            return;
        }

        // Walls: Tilemap + TilemapRenderer + TilemapCollider2D + Rigidbody2D + CompositeCollider2D
        var wallsTilemap = wallsObj.AddComponent<Tilemap>();
        var wallsRenderer = wallsObj.AddComponent<TilemapRenderer>();

        var wallsCollider = wallsObj.AddComponent<TilemapCollider2D>();
        wallsCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

        var wallsRb = wallsObj.AddComponent<Rigidbody2D>();
        wallsRb.bodyType = RigidbodyType2D.Static;

        var wallsComposite = wallsObj.AddComponent<CompositeCollider2D>();
        wallsComposite.geometryType = CompositeCollider2D.GeometryType.Polygons;

        // Set layer to match ch1-Room-01 walls (layer 8 = Ground)
        wallsObj.layer = 8;

        // Interactables: Tilemap + TilemapRenderer + SpawnTileManager
        var interTilemap = interObj.AddComponent<Tilemap>();
        var interRenderer = interObj.AddComponent<TilemapRenderer>();
        interObj.AddComponent<SpawnTileManager>();

        Debug.Log("Room 00 tilemaps set up successfully.");

        EditorUtility.SetDirty(wallsObj);
        EditorUtility.SetDirty(interObj);
    }
}

using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using UnityEditor.SceneManagement;

public static class AddInteractablesLayer
{
    public static void Execute()
    {
        // Find or create the Interactables layer under ch1-Room-01/Grid
        var grid = GameObject.Find("ch1-Room-01/Grid");
        if (grid == null)
        {
            Debug.LogError("[AddInteractablesLayer] ch1-Room-01/Grid not found");
            return;
        }

        // Check if it already exists
        var existing = grid.transform.Find("Interactables");
        if (existing != null)
        {
            Debug.Log("[AddInteractablesLayer] Interactables already exists, skipping creation");
            return;
        }

        // Create the Interactables GameObject
        var go = new GameObject("Interactables");
        go.transform.SetParent(grid.transform);
        go.transform.localPosition = Vector3.zero;

        go.AddComponent<Tilemap>();
        var renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = 1;
        go.AddComponent<SpawnTileManager>();

        // Mark scene dirty so it saves
        EditorUtility.SetDirty(go);
        EditorSceneManager.MarkSceneDirty(go.scene);
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[AddInteractablesLayer] Created and saved Interactables layer under ch1-Room-01/Grid");
    }
}

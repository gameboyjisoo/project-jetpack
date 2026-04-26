using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using UnityEditor.SceneManagement;

public static class MigrateInteractableTiles
{
    public static void Execute()
    {
        var wallsGO = GameObject.Find("ch1-Room-01/Grid/Walls");
        var interactablesGO = GameObject.Find("ch1-Room-01/Grid/Interactables");

        if (wallsGO == null || interactablesGO == null)
        {
            Debug.LogError("[Migrate] Could not find Walls or Interactables tilemap");
            return;
        }

        var wallsTilemap = wallsGO.GetComponent<Tilemap>();
        var interactablesTilemap = interactablesGO.GetComponent<Tilemap>();

        BoundsInt bounds = wallsTilemap.cellBounds;
        int moved = 0;

        foreach (var pos in bounds.allPositionsWithin)
        {
            var tile = wallsTilemap.GetTile(pos);
            if (tile is SpawnTile)
            {
                // Copy to Interactables tilemap
                interactablesTilemap.SetTile(pos, tile);
                // Remove from Walls tilemap
                wallsTilemap.SetTile(pos, null);
                moved++;
            }
        }

        EditorUtility.SetDirty(wallsGO);
        EditorUtility.SetDirty(interactablesGO);
        EditorSceneManager.MarkSceneDirty(wallsGO.scene);
        EditorSceneManager.SaveOpenScenes();

        Debug.Log($"[Migrate] Moved {moved} interactable tiles from Walls → Interactables. Scene saved.");
    }
}

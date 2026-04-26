// Clears wall tiles between Room 2 and Room 3 so the player can walk through.
// Room 2 right wall (tilemap x=28,29) and Room 3 left wall (tilemap x=-30,-29)
// both get a 6-tile-tall opening at y = -8 to -3 (matching Room 3's standard).

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

public static class FixRoom2to3Transition
{
    public static void Execute()
    {
        // Find Room_02 and Room_03 tilemaps
        var room2Walls = GameObject.Find("Room_02/Grid/Walls");
        var room3Walls = GameObject.Find("Room_03/Grid/Walls");

        if (room2Walls == null || room3Walls == null)
        {
            Debug.LogError("[FixTransition] Could not find Room_02/Grid/Walls or Room_03/Grid/Walls");
            return;
        }

        var tm2 = room2Walls.GetComponent<Tilemap>();
        var tm3 = room3Walls.GetComponent<Tilemap>();

        // Clear Room 2 right wall at y = -8 to -3
        for (int y = -8; y <= -3; y++)
        {
            tm2.SetTile(new Vector3Int(28, y, 0), null);
            tm2.SetTile(new Vector3Int(29, y, 0), null);
        }

        // Clear Room 3 left wall at y = -8 to -3 (should already be open, but ensure)
        for (int y = -8; y <= -3; y++)
        {
            tm3.SetTile(new Vector3Int(-30, y, 0), null);
            tm3.SetTile(new Vector3Int(-29, y, 0), null);
        }

        // Refresh colliders
        tm2.RefreshAllTiles();
        tm3.RefreshAllTiles();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[FixTransition] Room 2→3 transition opened at y = -8 to -3.");
    }
}
#endif

// Deletes old Room_01 and Room_02, then repositions Room_03-06 to positions 1-4.
// Room N center = (N-1) * 60.
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class CompressTo4Rooms
{
    public static void Execute()
    {
        // Delete old rooms 1 and 2
        for (int i = 1; i <= 2; i++)
        {
            var go = GameObject.Find($"Room_{i:D2}");
            if (go != null) Undo.DestroyObjectImmediate(go);
        }

        // Remap: Room_03 → Room_01 at x=0, Room_04 → Room_02 at x=60, etc.
        var remap = new (string oldName, string newName, string newId, float newX)[]
        {
            ("Room_03", "Room_01", "ch1-room-01", 0f),
            ("Room_04", "Room_02", "ch1-room-02", 60f),
            ("Room_05", "Room_03", "ch1-room-03", 120f),
            ("Room_06", "Room_04", "ch1-room-04", 180f),
        };

        foreach (var (oldName, newName, newId, newX) in remap)
        {
            var go = GameObject.Find(oldName);
            if (go == null)
            {
                Debug.LogWarning($"[CompressTo4] Could not find {oldName}");
                continue;
            }

            Undo.RecordObject(go.transform, "Reposition room");
            go.transform.position = new Vector3(newX, 0f, 0f);
            go.name = newName;

            var room = go.GetComponent<Room>();
            if (room != null)
            {
                Undo.RecordObject(room, "Update room ID");
                room.Init(newId, new Vector2(60f, 34f), room.SpawnPoint);
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[CompressTo4] Done. 4 rooms at positions 0, 60, 120, 180.");
    }
}
#endif

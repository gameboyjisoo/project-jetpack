#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class DeleteAllRooms
{
    public static void Execute()
    {
        int deleted = 0;
        foreach (var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))
        {
            Undo.DestroyObjectImmediate(room.gameObject);
            deleted++;
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log($"[DeleteAllRooms] Deleted {deleted} rooms.");
    }
}
#endif

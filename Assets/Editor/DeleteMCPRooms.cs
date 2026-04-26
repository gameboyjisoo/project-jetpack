using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class DeleteMCPRooms
{
    public static void Execute()
    {
        string[] roomNames = { "MCP_Room_01", "MCP_Room_02", "MCP_Room_03", "MCP_Room_04" };
        int deleted = 0;

        foreach (var name in roomNames)
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                Undo.DestroyObjectImmediate(go);
                deleted++;
                Debug.Log($"[DeleteMCPRooms] Deleted {name}");
            }
        }

        if (deleted > 0)
        {
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log($"[DeleteMCPRooms] Done. Deleted {deleted} MCP rooms. Scene saved.");
    }
}

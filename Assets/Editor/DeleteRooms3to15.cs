// Deletes all Room GameObjects from Room_03 through Room_15.
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class DeleteRooms3to15
{
    public static void Execute()
    {
        int deleted = 0;
        for (int i = 3; i <= 15; i++)
        {
            string name = $"Room_{i:D2}";
            var go = GameObject.Find(name);
            if (go != null)
            {
                Undo.DestroyObjectImmediate(go);
                deleted++;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[DeleteRooms] Deleted {deleted} rooms.");
    }
}
#endif

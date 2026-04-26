using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class DeleteAerialSlalom
{
    public static void Execute()
    {
        var go = GameObject.Find("ch1-Room-AerialSlalom");
        if (go != null)
        {
            Undo.DestroyObjectImmediate(go);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[DeleteAerialSlalom] Room deleted. Scene saved.");
        }
        else
        {
            Debug.Log("[DeleteAerialSlalom] Room not found.");
        }
    }
}

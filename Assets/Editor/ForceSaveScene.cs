using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class ForceSaveScene
{
    [MenuItem("Project Jetpack/Force Save Scene")]
    public static void Execute()
    {
        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        UnityEngine.Debug.Log($"Force-saved scene: {scene.path}");
    }
}

using UnityEngine;
using UnityEditor;

/// <summary>
/// Places a ClosingPlatform instance in ch1-Room-01 for playtesting.
/// One-shot script — delete after prototype is validated.
/// </summary>
public static class PlaceClosingPlatformTest
{
    public static void Execute()
    {
        // Load the prefab
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Interactables/ClosingPlatform.prefab");
        if (prefab == null)
        {
            Debug.LogError("[PlaceClosingPlatformTest] Prefab not found!");
            return;
        }

        // Find the room to parent under
        var room = GameObject.Find("ch1-Room-01");

        // Instantiate as a vertical barrier (1 wide, 6 tall) in mid-room
        // Room bounds: x=209-270, y=-17 to 17
        // Place at roughly x=240, y=-8 (lower-mid area where player walks)
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "ClosingPlatform_Test";
        instance.transform.position = new Vector3(240f, -8f, 0f);

        if (room != null)
            instance.transform.SetParent(room.transform);

        // Resize to vertical barrier: 1 wide, 6 tall
        var sr = instance.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.size = new Vector2(1f, 6f);

        var box = instance.GetComponent<BoxCollider2D>();
        if (box != null)
            box.size = new Vector2(1f, 6f);

        EditorUtility.SetDirty(instance);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"[PlaceClosingPlatformTest] Placed ClosingPlatform_Test at (240, -8) in ch1-Room-01. 1x6 vertical barrier, 2s closed / 1s open.");
    }
}

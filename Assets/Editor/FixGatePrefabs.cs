using UnityEditor;

public static class FixGatePrefabs
{
    public static void Execute()
    {
        AssetDatabase.DeleteAsset("Assets/Prefabs/Interactables/FuelGate_High.prefab");
        AssetDatabase.DeleteAsset("Assets/Prefabs/Interactables/FuelGate_Mid.prefab");
        AssetDatabase.DeleteAsset("Assets/Prefabs/Interactables/FuelGate_Low.prefab");
        AssetDatabase.Refresh();
        LevelEditorSetup.CreatePrefabs();
    }
}

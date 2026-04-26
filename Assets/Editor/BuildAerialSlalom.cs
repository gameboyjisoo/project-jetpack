using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using UnityEditor.SceneManagement;

/// <summary>
/// Builds "Aerial Slalom" — a room that showcases sustained aerial navigation,
/// mid-air fuel management, and directional jetpack commitment.
/// Designed to feel uniquely "Project Jetpack" rather than Celeste-with-jetpack.
/// </summary>
public static class BuildAerialSlalom
{
    // Room placement: after ch1-Room-01 (which is at some X position)
    // We'll auto-detect the next available X
    const float RoomWidth = 60f;
    const float RoomHeight = 34f;

    public static void Execute()
    {
        // Find next room position
        float nextX = 0f;
        Room[] existing = Object.FindObjectsByType<Room>(FindObjectsSortMode.None);
        foreach (var room in existing)
        {
            float rightEdge = room.transform.position.x + room.RoomSize.x * 0.5f + RoomWidth * 0.5f;
            if (rightEdge > nextX) nextX = rightEdge;
        }
        if (existing.Length == 0) nextX = 0f;

        string roomId = "ch1-Room-AerialSlalom";
        Vector3 roomPos = new Vector3(nextX, 0f, 0f);

        // ─── Room root ───
        GameObject roomGO = new GameObject(roomId);
        roomGO.transform.position = roomPos;
        Room roomComp = roomGO.AddComponent<Room>();

        var so = new SerializedObject(roomComp);
        so.FindProperty("roomSize").vector2Value = new Vector2(RoomWidth, RoomHeight);
        so.FindProperty("roomId").stringValue = roomId;

        // ─── Spawn Point ───
        GameObject spawnGO = new GameObject("SpawnPoint");
        spawnGO.transform.SetParent(roomGO.transform);
        spawnGO.transform.localPosition = new Vector3(-RoomWidth * 0.5f + 3f, -RoomHeight * 0.5f + 3f, 0f);
        so.FindProperty("spawnPoint").objectReferenceValue = spawnGO.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        // ─── Grid ───
        GameObject gridGO = new GameObject("Grid");
        gridGO.transform.SetParent(roomGO.transform);
        gridGO.transform.localPosition = Vector3.zero;
        var grid = gridGO.AddComponent<Grid>();
        grid.cellSize = new Vector3(1, 1, 0);

        // ─── Walls tilemap ───
        GameObject wallsGO = new GameObject("Walls");
        wallsGO.transform.SetParent(gridGO.transform);
        wallsGO.transform.localPosition = Vector3.zero;
        wallsGO.layer = 8;

        var wallsTilemap = wallsGO.AddComponent<Tilemap>();
        wallsGO.AddComponent<TilemapRenderer>();
        var tc = wallsGO.AddComponent<TilemapCollider2D>();
        tc.compositeOperation = Collider2D.CompositeOperation.Merge;
        var rb = wallsGO.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        var cc = wallsGO.AddComponent<CompositeCollider2D>();
        cc.geometryType = CompositeCollider2D.GeometryType.Polygons;

        // ─── Interactables tilemap ───
        GameObject intGO = new GameObject("Interactables");
        intGO.transform.SetParent(gridGO.transform);
        intGO.transform.localPosition = Vector3.zero;
        intGO.AddComponent<Tilemap>();
        var intRenderer = intGO.AddComponent<TilemapRenderer>();
        intRenderer.sortingOrder = 1;
        intGO.AddComponent<SpawnTileManager>();

        // ─── Load tiles ───
        Tile caveTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/PrtCave/PrtCave_0_0.asset");
        if (caveTile == null)
            caveTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/GroundTile.asset");

        SpawnTile hazardTile = AssetDatabase.LoadAssetAtPath<SpawnTile>("Assets/Tiles/Interactables/Hazard.asset");
        SpawnTile fuelPickupTile = AssetDatabase.LoadAssetAtPath<SpawnTile>("Assets/Tiles/Interactables/FuelPickup.asset");
        SpawnTile dashPickupTile = AssetDatabase.LoadAssetAtPath<SpawnTile>("Assets/Tiles/Interactables/DashPickup.asset");
        SpawnTile highGateTile = AssetDatabase.LoadAssetAtPath<SpawnTile>("Assets/Tiles/Interactables/FuelGate_High.asset");
        SpawnTile checkpointTile = AssetDatabase.LoadAssetAtPath<SpawnTile>("Assets/Tiles/Interactables/Checkpoint.asset");

        var walls = wallsGO.GetComponent<Tilemap>();
        var interactables = intGO.GetComponent<Tilemap>();

        int halfW = 30; // 60/2
        int halfH = 17; // 34/2

        // ═══════════════════════════════════════════════════
        // BORDER WALLS
        // ═══════════════════════════════════════════════════
        for (int x = -halfW; x < halfW; x++)
        {
            walls.SetTile(new Vector3Int(x, -halfH, 0), caveTile);     // floor
            walls.SetTile(new Vector3Int(x, halfH - 1, 0), caveTile);  // ceiling
        }
        int openMin = -8, openMax = -3;
        for (int y = -halfH; y < halfH; y++)
        {
            bool leftOpen = y >= openMin && y <= openMax;
            bool rightOpen = y >= openMin && y <= openMax;
            if (!leftOpen) walls.SetTile(new Vector3Int(-halfW, y, 0), caveTile);
            if (!rightOpen) walls.SetTile(new Vector3Int(halfW - 1, y, 0), caveTile);
        }

        // ═══════════════════════════════════════════════════
        // SPAWN PLATFORM (bottom-left, 5 tiles wide)
        // ═══════════════════════════════════════════════════
        for (int x = -halfW + 1; x < -halfW + 6; x++)
            walls.SetTile(new Vector3Int(x, -halfH + 2, 0), caveTile);

        // ═══════════════════════════════════════════════════
        // SPIKE FLOOR (most of the bottom is deadly)
        // ═══════════════════════════════════════════════════
        for (int x = -halfW + 6; x < halfW - 1; x++)
            interactables.SetTile(new Vector3Int(x, -halfH + 1, 0), hazardTile);

        // ═══════════════════════════════════════════════════
        // FLOATING PLATFORMS — ascending left to right
        // Small platforms (2-3 tiles) requiring aerial navigation
        // ═══════════════════════════════════════════════════

        // Phase 1: "Learn to fly between platforms" — close together, easy
        PlacePlatform(walls, caveTile, -20, -10, 3);  // Platform A
        PlacePlatform(walls, caveTile, -14, -6, 3);   // Platform B (6 right, 4 up)
        PlacePlatform(walls, caveTile, -8, -2, 2);    // Platform C (6 right, 4 up)

        // Phase 2: "Commit to a direction" — further apart, need full jetpack
        PlacePlatform(walls, caveTile, -1, 3, 2);     // Platform D (7 right, 5 up)
        PlacePlatform(walls, caveTile, 7, 7, 2);      // Platform E (8 right, 4 up — tight on fuel)

        // ─── HIGH FUEL GATE before Platform F ───
        // Player must arrive with >50% fuel — meaning efficient flying from E
        interactables.SetTile(new Vector3Int(13, 7, 0), highGateTile);
        interactables.SetTile(new Vector3Int(13, 8, 0), highGateTile);
        interactables.SetTile(new Vector3Int(13, 9, 0), highGateTile);

        // Checkpoint before the gate
        interactables.SetTile(new Vector3Int(6, 8, 0), checkpointTile);

        PlacePlatform(walls, caveTile, 14, 7, 3);     // Platform F (after gate)

        // ─── Fuel pickup as reward for passing gate ───
        interactables.SetTile(new Vector3Int(15, 9, 0), fuelPickupTile);

        // Phase 3: "Aerial dodging" — descending with wall spikes
        // Vertical column with spikes on alternating sides
        // Player must jetpack left/right to dodge while descending

        // Right-side column structure
        for (int y = -10; y <= 6; y++)
        {
            walls.SetTile(new Vector3Int(20, y, 0), caveTile);
            walls.SetTile(new Vector3Int(21, y, 0), caveTile);
        }

        // Spikes on left face of column (alternating heights)
        interactables.SetTile(new Vector3Int(19, 5, 0), hazardTile);
        interactables.SetTile(new Vector3Int(19, 4, 0), hazardTile);
        interactables.SetTile(new Vector3Int(19, 1, 0), hazardTile);
        interactables.SetTile(new Vector3Int(19, 0, 0), hazardTile);
        interactables.SetTile(new Vector3Int(19, -3, 0), hazardTile);
        interactables.SetTile(new Vector3Int(19, -4, 0), hazardTile);
        interactables.SetTile(new Vector3Int(19, -7, 0), hazardTile);
        interactables.SetTile(new Vector3Int(19, -8, 0), hazardTile);

        // Opposite wall for the slalom corridor
        for (int y = -10; y <= 6; y++)
        {
            walls.SetTile(new Vector3Int(16, y, 0), caveTile);
        }
        // Spikes on right face of opposite wall (offset from the other side)
        interactables.SetTile(new Vector3Int(17, 3, 0), hazardTile);
        interactables.SetTile(new Vector3Int(17, 2, 0), hazardTile);
        interactables.SetTile(new Vector3Int(17, -1, 0), hazardTile);
        interactables.SetTile(new Vector3Int(17, -2, 0), hazardTile);
        interactables.SetTile(new Vector3Int(17, -5, 0), hazardTile);
        interactables.SetTile(new Vector3Int(17, -6, 0), hazardTile);
        interactables.SetTile(new Vector3Int(17, -9, 0), hazardTile);

        // ─── Dash pickup mid-descent (reward for surviving halfway) ───
        interactables.SetTile(new Vector3Int(18, -1, 0), dashPickupTile);

        // Landing platform at bottom of slalom
        PlacePlatform(walls, caveTile, 17, -12, 4);

        // Checkpoint after slalom
        interactables.SetTile(new Vector3Int(18, -11, 0), checkpointTile);

        // Phase 4: "Horizontal sprint to exit"
        // Long gap requiring jetpack + dash combo to reach exit platform
        PlacePlatform(walls, caveTile, 25, -12, 4);   // Exit platform

        // Exit opening in right wall is at y = -8 to -3
        // Add a ramp/platform near the exit
        for (int x = 25; x < halfW - 1; x++)
            walls.SetTile(new Vector3Int(x, -halfH + 2, 0), caveTile);

        // ═══════════════════════════════════════════════════
        // Register and save
        // ═══════════════════════════════════════════════════
        Undo.RegisterCreatedObjectUndo(roomGO, "Create Aerial Slalom Room");
        Selection.activeGameObject = roomGO;
        EditorUtility.SetDirty(roomGO);
        EditorSceneManager.MarkSceneDirty(roomGO.scene);
        EditorSceneManager.SaveOpenScenes();

        Debug.Log($"[BuildAerialSlalom] Room '{roomId}' created at x={nextX}. Scene saved.");
    }

    static void PlacePlatform(Tilemap tilemap, Tile tile, int x, int y, int width)
    {
        for (int i = 0; i < width; i++)
            tilemap.SetTile(new Vector3Int(x + i, y, 0), tile);
    }
}

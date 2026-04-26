using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

/// <summary>
/// Editor window for quickly creating new room shells with all required components.
/// Also provides utilities for room management.
/// </summary>
public class RoomTools : EditorWindow
{
    private string roomId = "ch1-room-05";
    private Vector2 roomSize = new Vector2(60f, 34f);
    private bool paintBorderWalls = true;
    private bool addLeftOpening = true;
    private bool addRightOpening = true;

    [MenuItem("Project Jetpack/New Room %#r")]  // Ctrl+Shift+R shortcut
    public static void ShowWindow()
    {
        var win = GetWindow<RoomTools>("New Room");
        win.minSize = new Vector2(300, 340);
    }

    private void OnGUI()
    {
        GUILayout.Label("Create New Room", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        roomId = EditorGUILayout.TextField("Room ID", roomId);
        roomSize = EditorGUILayout.Vector2Field("Room Size (units)", roomSize);

        EditorGUILayout.Space(8);
        GUILayout.Label("Options", EditorStyles.boldLabel);
        paintBorderWalls = EditorGUILayout.Toggle("Paint Border Walls", paintBorderWalls);

        if (paintBorderWalls)
        {
            EditorGUI.indentLevel++;
            addLeftOpening = EditorGUILayout.Toggle("Left Opening (6-tile)", addLeftOpening);
            addRightOpening = EditorGUILayout.Toggle("Right Opening (6-tile)", addRightOpening);
            EditorGUI.indentLevel--;
        }

        // Auto-calculate next position
        EditorGUILayout.Space(8);
        float nextX = CalculateNextRoomX();
        EditorGUILayout.HelpBox(
            $"Room will be placed at X = {nextX}\n" +
            $"(after rightmost existing room)",
            MessageType.Info);

        EditorGUILayout.Space(8);

        if (GUILayout.Button("Create Room", GUILayout.Height(32)))
        {
            CreateRoom(new Vector3(nextX, 0f, 0f));
        }

        EditorGUILayout.Space(16);
        GUILayout.Label("Utilities", EditorStyles.boldLabel);

        if (GUILayout.Button("Select All Rooms"))
        {
            var rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
            Selection.objects = System.Array.ConvertAll(rooms, r => (Object)r.gameObject);
        }

        if (GUILayout.Button("Log Room Positions"))
        {
            var rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
            System.Array.Sort(rooms, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            foreach (var r in rooms)
                Debug.Log($"  {r.RoomId} @ x={r.transform.position.x}, size={r.RoomSize}");
        }
    }

    private float CalculateNextRoomX()
    {
        Room[] existing = FindObjectsByType<Room>(FindObjectsSortMode.None);
        float maxRight = 0f;
        foreach (var room in existing)
        {
            float rightEdge = room.transform.position.x + room.RoomSize.x * 0.5f + roomSize.x * 0.5f;
            if (rightEdge > maxRight) maxRight = rightEdge;
        }
        // If no rooms exist, start at 0
        return existing.Length == 0 ? 0f : maxRight;
    }

    private void CreateRoom(Vector3 position)
    {
        // ─── Room root ───
        GameObject roomGO = new GameObject(roomId);
        roomGO.transform.position = position;
        Room room = roomGO.AddComponent<Room>();

        // Set serialized fields via SerializedObject (private fields)
        var so = new SerializedObject(room);
        so.FindProperty("roomSize").vector2Value = roomSize;
        so.FindProperty("roomId").stringValue = roomId;

        // ─── Spawn Point ───
        GameObject spawnGO = new GameObject("SpawnPoint");
        spawnGO.transform.SetParent(roomGO.transform);
        // Default spawn: bottom-left area, above floor
        spawnGO.transform.localPosition = new Vector3(
            -roomSize.x * 0.5f + 4f,
            -roomSize.y * 0.5f + 3f,
            0f);

        so.FindProperty("spawnPoint").objectReferenceValue = spawnGO.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        // ─── Grid > Walls (Tilemap with colliders) ───
        GameObject gridGO = new GameObject("Grid");
        gridGO.transform.SetParent(roomGO.transform);
        gridGO.transform.localPosition = Vector3.zero;
        var grid = gridGO.AddComponent<Grid>();
        grid.cellSize = new Vector3(1, 1, 0);

        GameObject wallsGO = new GameObject("Walls");
        wallsGO.transform.SetParent(gridGO.transform);
        wallsGO.transform.localPosition = Vector3.zero;
        wallsGO.layer = 8; // Ground

        var tilemap = wallsGO.AddComponent<Tilemap>();
        wallsGO.AddComponent<TilemapRenderer>();

        var tilemapCol = wallsGO.AddComponent<TilemapCollider2D>();
        // Unity 6: compositeOperation replaces usedByComposite
        tilemapCol.compositeOperation = Collider2D.CompositeOperation.Merge;

        var rb = wallsGO.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        var composite = wallsGO.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

        // ─── Interactables tilemap (separate layer for SpawnTiles) ───
        GameObject interactablesGO = new GameObject("Interactables");
        interactablesGO.transform.SetParent(gridGO.transform);
        interactablesGO.transform.localPosition = Vector3.zero;

        interactablesGO.AddComponent<Tilemap>();
        var interactablesRenderer = interactablesGO.AddComponent<TilemapRenderer>();
        interactablesRenderer.sortingOrder = 1; // render above walls
        interactablesGO.AddComponent<SpawnTileManager>();

        // ─── Paint border walls if requested ───
        if (paintBorderWalls)
        {
            PaintBorderWalls(tilemap, position);
        }

        // Register undo and select
        Undo.RegisterCreatedObjectUndo(roomGO, $"Create Room {roomId}");
        Selection.activeGameObject = roomGO;
        EditorUtility.SetDirty(roomGO);

        Debug.Log($"[RoomTools] Created room '{roomId}' at ({position.x}, {position.y}), size {roomSize}");

        // Auto-increment room ID for next creation
        IncrementRoomId();
    }

    private void PaintBorderWalls(Tilemap tilemap, Vector3 roomWorldPos)
    {
        // Load the ground tile
        Tile groundTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/GroundTile.asset");
        if (groundTile == null)
        {
            // Fallback: try to find any Cave Story tile
            groundTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/PrtCave/PrtCave_0_0.asset");
        }
        if (groundTile == null)
        {
            Debug.LogWarning("[RoomTools] No ground tile found. Skipping border walls.");
            return;
        }

        // Tilemap positions are in local grid coordinates.
        // Room center is at the Room transform position.
        // Grid is at localPosition (0,0) relative to room.
        // So tile (0,0) in the tilemap = room center.
        // Room spans from -halfW to +halfW-1 in tile coords.
        int halfW = Mathf.FloorToInt(roomSize.x * 0.5f);
        int halfH = Mathf.FloorToInt(roomSize.y * 0.5f);

        // Opening range for room transitions (6-tile gap, y = -8 to -3 relative to center)
        int openingMin = -8;
        int openingMax = -3;

        // Floor and ceiling (full width)
        for (int x = -halfW; x < halfW; x++)
        {
            tilemap.SetTile(new Vector3Int(x, -halfH, 0), groundTile);      // floor
            tilemap.SetTile(new Vector3Int(x, halfH - 1, 0), groundTile);   // ceiling
        }

        // Left wall
        for (int y = -halfH; y < halfH; y++)
        {
            bool isOpening = addLeftOpening && y >= openingMin && y <= openingMax;
            if (!isOpening)
                tilemap.SetTile(new Vector3Int(-halfW, y, 0), groundTile);
        }

        // Right wall
        for (int y = -halfH; y < halfH; y++)
        {
            bool isOpening = addRightOpening && y >= openingMin && y <= openingMax;
            if (!isOpening)
                tilemap.SetTile(new Vector3Int(halfW - 1, y, 0), groundTile);
        }
    }

    private void IncrementRoomId()
    {
        // Try to auto-increment numeric suffix: "ch1-room-05" → "ch1-room-06"
        int lastDash = roomId.LastIndexOf('-');
        if (lastDash >= 0 && lastDash < roomId.Length - 1)
        {
            string suffix = roomId.Substring(lastDash + 1);
            if (int.TryParse(suffix, out int num))
            {
                roomId = roomId.Substring(0, lastDash + 1) + (num + 1).ToString(suffix.Length > 1 ? "D2" : "D1");
            }
        }
    }
}

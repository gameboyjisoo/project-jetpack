// BuildRooms12to15.cs
// Builds tutorial rooms 12-15 for Project Jetpack via Coplay MCP execute_script.
// Implements: design/levels/chapter1-room-designs.md (rooms 12-15)
//
// Coordinate system:
//   Each room is 60x34 units. Room N center = ((N-1)*60, 0).
//   Tile space: x in [-30..29], y in [-17..16].
//   ASCII layout: each character = 2x2 tiles. topPad = (34 - asciiHeight*2) / 2.
//   ASCII row r, col c → tile top-left at (x = -30 + c*2, y = 16 - topPad - r*2).
//
// Design doc: design/levels/chapter1-room-designs.md
// Hazards: layer 10 (Hazard), trigger BoxCollider2D, grouped per contiguous run.
// Pickups: layer 11 (Collectible), trigger CircleCollider2D radius 0.5.
// Walls: layer 8 (Ground), CompositeCollider2D Polygons (CRITICAL — see CLAUDE.md).

using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class BuildRooms12to15
{
    // Cached ground tile — loaded once in Execute, read by all PaintRect calls.
    static TileBase s_groundTile;

    // -------------------------------------------------------------------------
    // Entry point called by Coplay MCP execute_script
    // -------------------------------------------------------------------------
    public static void Execute()
    {
        s_groundTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/GroundTile.asset");
        if (s_groundTile == null)
        {
            Debug.LogError("[BuildRooms12to15] Could not load Assets/Tiles/GroundTile.asset. Aborting.");
            return;
        }

        BuildRoom12();
        BuildRoom13();
        BuildRoom14();
        BuildRoom15();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[BuildRooms12to15] Rooms 12-15 created. Review in scene view before saving.");
    }

    // =========================================================================
    // ROOM 12: "Efficient Flight"
    // Navigate spike obstacle course with limited fuel.
    // High fuel gate (cyan) blocks the right exit — must arrive fuel-rich.
    // Spawn left, exit right through gate.
    //
    // ASCII layout — 30 cols x 13 rows, topPad = 4
    // Row  0: ##############################  ceiling
    // Row  1: #............................#
    // Row  2: #.......XXXXX....XXXXX......#  floating spike strips
    // Row  3: #............................#
    // Row  4: #....XXXXX....XXXXX.........#  lower floating spikes
    // Row  5: #............................#
    // Row  6: #.S..........................#  spawn (col 1)
    // Row  7: #.####.......................#  spawn platform (cols 1-4)
    // Row  8: #....XXXXXXXXXXXXXXXXXX......#  long spike strip on platform
    // Row  9: #....##################......#  platform backing for spikes
    // Row 10: #............................#
    // Row 11: #............................#
    // Row 12: ##############################  floor
    //
    // High gate placed in right wall opening (y -8 to -3).
    // Exit landing platform at x 22-29, y -4 to -3.
    // =========================================================================
    static void BuildRoom12()
    {
        const float cx = 660f;
        const string roomId = "ch1-room-12";
        const int asciiRows = 13;
        const int topPad = (34 - asciiRows * 2) / 2; // 4

        GameObject roomGO = CreateRoomShell(roomId, cx, out Room room,
            out Tilemap tilemap, out Transform spawnTF);

        // Spawn: row 6, col 1 → tile x = -28, y = TileY(4,6) = 16-4-12 = 0 → +1 for above platform
        spawnTF.localPosition = new Vector3(-28f, TileY(topPad, 6) + 1f, 0f);
        room.Init(roomId, new Vector2(60f, 34f), spawnTF);

        // Walls
        PaintFullCeiling(tilemap, topPad);
        PaintFullFloor(tilemap, topPad, asciiRows);
        PaintLeftWall(tilemap);
        PaintRightWallWithOpening(tilemap, -8, -3);

        // Spawn platform: row 7, cols 1-4 → x [-28..-21]
        PaintRect(tilemap, -28, -21, TileY(topPad, 7) - 1, TileY(topPad, 7));

        // Spike platform backing: row 9, cols 4-23 → x [-22..17]
        PaintRect(tilemap, -22, 17, TileY(topPad, 9) - 1, TileY(topPad, 9));

        // Exit landing platform: x [22..29], y [-4..-3]
        PaintRect(tilemap, 22, 29, -4, -3);

        // Hazards
        // Row 2 group A: cols 7-11 → left edge = -30+7*2 = -16, 5 chars = 10 tiles, center x = -16+5 = -11
        CreateHazard(roomGO, "Spikes_12_R2A",
            localX: -11f, localY: TileY(topPad, 2) + 0.5f, width: 10f, height: 1f);

        // Row 2 group B: cols 17-21 → center x = -30+17*2+5 = 9
        CreateHazard(roomGO, "Spikes_12_R2B",
            localX: 9f, localY: TileY(topPad, 2) + 0.5f, width: 10f, height: 1f);

        // Row 4 group A: cols 4-8 → center x = -30+4*2+5 = -17
        CreateHazard(roomGO, "Spikes_12_R4A",
            localX: -17f, localY: TileY(topPad, 4) + 0.5f, width: 10f, height: 1f);

        // Row 4 group B: cols 14-18 → center x = -30+14*2+5 = 3
        CreateHazard(roomGO, "Spikes_12_R4B",
            localX: 3f, localY: TileY(topPad, 4) + 0.5f, width: 10f, height: 1f);

        // Row 8 long spike strip: cols 4-23 → 20 chars = 40 tiles, center x = -30+4*2+20 = -2
        CreateHazard(roomGO, "Spikes_12_Floor",
            localX: -2f, localY: TileY(topPad, 8) + 0.5f, width: 40f, height: 1f);

        // High fuel gate in right wall opening — center at (27, -5.5), 2 wide x 6 tall
        CreateFuelGate(roomGO, "FuelGate_12_High", FuelTier.High,
            localX: 27f, localY: -5.5f, gateWidth: 2f, gateHeight: 6f);

        Undo.RegisterCreatedObjectUndo(roomGO, "Create Room 12");
        Debug.Log("[BuildRooms12to15] Room 12 created.");
    }

    // =========================================================================
    // ROOM 13: "The Fork"
    // The key design moment: fuel state at spawn determines which path is open.
    // High fuel (>50%) → left gate opens (harder route, more obstacles).
    // Low fuel (<20%) → right gate opens (shorter route, harder to reach).
    // Both paths converge at exit platform at top center.
    //
    // ASCII layout — 30 cols x 14 rows, topPad = 3
    // Row  0: ##############################  ceiling
    // Row  1: #.............E..............#  exit center
    // Row  2: #............####............#  exit platform (cols 12-15)
    // Row  3: #............................#
    // Row  4: #..GH....................GL..#  gate tops: High col 2, Low col 25
    // Row  5: #..||....................||..#
    // Row  6: #..||....platformL...||..#  left nav platform (cols 4-7)
    // Row  7: #..||....................||..#
    // Row  8: #..||......####.S......||..#  spawn platform (cols 10-13), spawn col 14
    // Row  9: #..||......####........||..#  spawn platform (continues)
    // Row 10: #..||....................||..#
    // Row 11: #..||..XXXXXXXXXXXXXX..||..#  spike pit (cols 6-21)
    // Row 12: #..||..##############..||..#  spike floor backing
    // Row 13: ##############################  floor
    //
    // Left wall opening at spawn height (row 8 tile y = -3): y range [-5..2].
    // Right wall opening: standard [-8..-3].
    // =========================================================================
    static void BuildRoom13()
    {
        const float cx = 720f;
        const string roomId = "ch1-room-13";
        const int asciiRows = 14;
        const int topPad = (34 - asciiRows * 2) / 2; // 3

        GameObject roomGO = CreateRoomShell(roomId, cx, out Room room,
            out Tilemap tilemap, out Transform spawnTF);

        // Spawn: row 8, col 14 → tile x = -30+14*2 = -2, tile y = TileY(3,8) = 16-3-16 = -3
        spawnTF.localPosition = new Vector3(-2f, TileY(topPad, 8) + 1f, 0f);
        room.Init(roomId, new Vector2(60f, 34f), spawnTF);

        // Left wall opening aligned to spawn platform height.
        // TileY(3,8) = -3. Opening [-5..2] gives 8 tiles (generous for room transition).
        int spawnTileTopY = TileY(topPad, 8); // = -3
        PaintLeftWallWithOpeningCustom(tilemap, spawnTileTopY - 2, spawnTileTopY + 4);
        PaintRightWallWithOpening(tilemap, -8, -3);

        PaintFullCeiling(tilemap, topPad);
        PaintFullFloor(tilemap, topPad, asciiRows);

        // Exit platform: row 2, cols 12-15 → x [-6..1]
        PaintRect(tilemap, -6, 1, TileY(topPad, 2) - 1, TileY(topPad, 2));

        // Spawn/center platform: rows 8-9, cols 10-13 → x [-10..-3]
        PaintRect(tilemap, -10, -3, TileY(topPad, 9) - 1, TileY(topPad, 8));

        // Spike floor backing: row 12, cols 6-21 → x [-18..13]
        PaintRect(tilemap, -18, 13, TileY(topPad, 12) - 1, TileY(topPad, 12));

        // Left path nav platform: row 6, cols 4-7 → x [-22..-15]
        PaintRect(tilemap, -22, -15, TileY(topPad, 6) - 1, TileY(topPad, 6));

        // Right path nav platform: row 6, cols 21-24 → x [12..18]
        PaintRect(tilemap, 12, 18, TileY(topPad, 6) - 1, TileY(topPad, 6));

        // Fuel gates: span rows 4-10 vertically
        // TileY(3,4) = 16-3-8 = 5 (top of gate), TileY(3,10) = 16-3-20 = -7 (bottom)
        float gateTop = TileY(topPad, 4) + 2f;
        float gateBot = TileY(topPad, 10);
        float gateCenterY = (gateTop + gateBot) * 0.5f;
        float gateH = Mathf.Abs(gateTop - gateBot);

        // High gate (cyan): cols 2-3 → tile x = -30+4 = -26, gate center x = -25
        CreateFuelGate(roomGO, "FuelGate_13_High", FuelTier.High,
            localX: -25f, localY: gateCenterY, gateWidth: 2f, gateHeight: gateH);

        // Low gate (red): cols 25-26 → tile x = -30+50 = 20, gate center x = 21
        CreateFuelGate(roomGO, "FuelGate_13_Low", FuelTier.Low,
            localX: 21f, localY: gateCenterY, gateWidth: 2f, gateHeight: gateH);

        // Spike pit hazard: row 11, cols 6-21 → 16 chars = 32 tiles, center x = -30+6*2+16 = -2
        CreateHazard(roomGO, "Spikes_13_Pit",
            localX: -2f, localY: TileY(topPad, 11) + 0.5f, width: 32f, height: 1f);

        // Left path obstacle spike: row 6, cols 4-5 → left edge = -30+8 = -22, 2 chars = 4 tiles, center x = -20
        // Placed so High-gate players still need to navigate, not just fly straight up.
        CreateHazard(roomGO, "Spikes_13_LeftObstacle",
            localX: -20f, localY: TileY(topPad, 6) + 1.5f, width: 4f, height: 1f);

        Undo.RegisterCreatedObjectUndo(roomGO, "Create Room 13");
        Debug.Log("[BuildRooms12to15] Room 13 created.");
    }

    // =========================================================================
    // ROOM 14: "Aim and Fire"
    // Mode swap zone (dash → gun) near spawn. Spike pit requires jetpacking.
    // Shootable target above mid-flight path — aim while managing fuel.
    // Fuel pickup over the pit. Spawn left, exit right.
    //
    // ASCII layout — 30 cols x 13 rows, topPad = 4
    // Row  0: ##############################  ceiling
    // Row  1: #............................#
    // Row  2: #............................#
    // Row  3: #.............T..............#  target (col 13)
    // Row  4: #............................#
    // Row  5: #............................#  fuel pickup here (col 15)
    // Row  6: #.S..........####.........E..#  spawn col 1, center platform cols 12-15
    // Row  7: #.####...................#####  spawn platform cols 1-4; exit platform cols 23-27
    // Row  8: #....XXXXXXXXXXXXXXXX........#  spike pit cols 4-21
    // Row  9: #....##################......#  spike floor backing cols 4-23
    // Row 10: #............................#
    // Row 11: #....W.......................#  mode swap zone (col 2-3)
    // Row 12: ##############################  floor
    //
    // Left wall: full (no left opening; player enters from bottom of room 13's right wall).
    // Right wall: standard opening [-8..-3].
    // =========================================================================
    static void BuildRoom14()
    {
        const float cx = 780f;
        const string roomId = "ch1-room-14";
        const int asciiRows = 13;
        const int topPad = (34 - asciiRows * 2) / 2; // 4

        GameObject roomGO = CreateRoomShell(roomId, cx, out Room room,
            out Tilemap tilemap, out Transform spawnTF);

        // Spawn: row 6, col 1 → tile x = -28, tile y = TileY(4,6) = 0
        spawnTF.localPosition = new Vector3(-28f, TileY(topPad, 6) + 1f, 0f);
        room.Init(roomId, new Vector2(60f, 34f), spawnTF);

        PaintFullCeiling(tilemap, topPad);
        PaintFullFloor(tilemap, topPad, asciiRows);
        PaintLeftWall(tilemap);
        PaintRightWallWithOpening(tilemap, -8, -3);

        // Spawn platform: row 7, cols 1-4 → x [-28..-21]
        PaintRect(tilemap, -28, -21, TileY(topPad, 7) - 1, TileY(topPad, 7));

        // Center landing platform: row 7, cols 12-15 → x [-6..1]
        PaintRect(tilemap, -6, 1, TileY(topPad, 7) - 1, TileY(topPad, 7));

        // Exit platform: row 7, cols 23-27 → x [16..24]
        PaintRect(tilemap, 16, 24, TileY(topPad, 7) - 1, TileY(topPad, 7));

        // Spike floor backing: row 9, cols 4-23 → x [-22..17]
        PaintRect(tilemap, -22, 17, TileY(topPad, 9) - 1, TileY(topPad, 9));

        // Spike pit hazard: row 8, cols 4-21 → 18 chars = 36 tiles, center x = -30+4*2+18 = -4
        CreateHazard(roomGO, "Spikes_14_Pit",
            localX: -4f, localY: TileY(topPad, 8) + 0.5f, width: 36f, height: 1f);

        // Mode swap zone: row 11, cols 2-3 → center x = -30+2*2+2 = -24
        // Green visual trigger — SwapMode logic wired when BoosterSwapZone.cs is implemented.
        CreateModeSwapZone(roomGO, "ModeSwapZone_14",
            localX: -24f, localY: TileY(topPad, 11) + 2f);

        // Shootable target: row 3, col 13 → tile x = -30+13*2 = -4
        // Yellow visual — hit detection wired when interactive target system is designed.
        CreateShootableTarget(roomGO, "Target_14",
            localX: -4f, localY: TileY(topPad, 3) + 1f);

        // Fuel pickup: row 5, col 15 → tile x = -30+15*2 = 0
        // Placed mid-air over spike pit to reward the fuel-efficient flight line.
        CreateFuelPickup(roomGO, "FuelPickup_14",
            localX: 0f, localY: TileY(topPad, 5) + 1f);

        Undo.RegisterCreatedObjectUndo(roomGO, "Create Room 14");
        Debug.Log("[BuildRooms12to15] Room 14 created.");
    }

    // =========================================================================
    // ROOM 15: "Everything Together"
    // Graduation test combining all chapter 1 mechanics:
    //   - Fuel fork (High left gate, Low right gate)
    //   - Shootable target on right path
    //   - Mode swap zone at spawn
    //   - Fuel pickup and dash pickup placed for strategic collection
    //   - Spike pit in middle-left
    //   - Exit at top center
    // No right wall opening (final chapter 1 room).
    //
    // ASCII layout — 30 cols x 14 rows, topPad = 3
    // Row  0: ##############################  ceiling
    // Row  1: #.............E..............#  exit center
    // Row  2: #............####............#  exit platform (cols 12-15)
    // Row  3: #............................#
    // Row  4: #..GH.......T........GL.....#  High gate (col 2), target (col 12), Low gate (col 22)
    // Row  5: #..||................||.....#
    // Row  6: #..||................||.....#
    // Row  7: #..||.....####.......||.....#  inner platform (cols 10-13)
    // Row  8: #..||................||.....#
    // Row  9: #..||..XXXXXXXXXX....||.....#  spike pit (cols 6-15)
    // Row 10: #..||..##########..F.||.....#  spike backing (cols 6-15) + fuel pickup (col 18)
    // Row 11: #.S..........W...D...........#  spawn (col 1), mode swap (col 12), dash pickup (col 16)
    // Row 12: #.####.......................#  spawn platform (cols 1-4)
    // Row 13: ##############################  floor
    //
    // Left wall opening at spawn height (row 11, y = -9): [-11..-4].
    // No right wall opening (Room 15 is the final room).
    // =========================================================================
    static void BuildRoom15()
    {
        const float cx = 840f;
        const string roomId = "ch1-room-15";
        const int asciiRows = 14;
        const int topPad = (34 - asciiRows * 2) / 2; // 3

        GameObject roomGO = CreateRoomShell(roomId, cx, out Room room,
            out Tilemap tilemap, out Transform spawnTF);

        // Spawn: row 11, col 1 → tile x = -28, tile y = TileY(3,11) = 16-3-22 = -9
        spawnTF.localPosition = new Vector3(-28f, TileY(topPad, 11) + 1f, 0f);
        room.Init(roomId, new Vector2(60f, 34f), spawnTF);

        // Left wall opening at spawn height. TileY(3,11) = -9. Opening [-11..-4].
        int spawnTileY15 = TileY(topPad, 11); // = -9
        PaintLeftWallWithOpeningCustom(tilemap, spawnTileY15 - 2, spawnTileY15 + 4);

        // No right wall opening — Room 15 is the final room.
        PaintRightWall(tilemap);

        PaintFullCeiling(tilemap, topPad);
        PaintFullFloor(tilemap, topPad, asciiRows);

        // Exit platform: row 2, cols 12-15 → x [-6..1]
        PaintRect(tilemap, -6, 1, TileY(topPad, 2) - 1, TileY(topPad, 2));

        // Inner platform: row 7, cols 10-13 → x [-10..-3]
        PaintRect(tilemap, -10, -3, TileY(topPad, 7) - 1, TileY(topPad, 7));

        // Spike floor backing: row 10, cols 6-15 → x [-18..1]
        PaintRect(tilemap, -18, 1, TileY(topPad, 10) - 1, TileY(topPad, 10));

        // Spawn platform: row 12, cols 1-4 → x [-28..-21]
        PaintRect(tilemap, -28, -21, TileY(topPad, 12) - 1, TileY(topPad, 12));

        // Fuel gates: span rows 4-10 vertically
        // TileY(3,4) = 16-3-8 = 5, TileY(3,10) = 16-3-20 = -7
        float gateTop15 = TileY(topPad, 4) + 2f;
        float gateBot15 = TileY(topPad, 10);
        float gateCenterY15 = (gateTop15 + gateBot15) * 0.5f;
        float gateH15 = Mathf.Abs(gateTop15 - gateBot15);

        // High gate: col 2 → tile x = -26, gate center x = -25
        CreateFuelGate(roomGO, "FuelGate_15_High", FuelTier.High,
            localX: -25f, localY: gateCenterY15, gateWidth: 2f, gateHeight: gateH15);

        // Low gate: col 22 → tile x = -30+44 = 14, gate center x = 15
        CreateFuelGate(roomGO, "FuelGate_15_Low", FuelTier.Low,
            localX: 15f, localY: gateCenterY15, gateWidth: 2f, gateHeight: gateH15);

        // Spike pit hazard: row 9, cols 6-15 → 10 chars = 20 tiles, center x = -30+6*2+10 = -8
        CreateHazard(roomGO, "Spikes_15_Pit",
            localX: -8f, localY: TileY(topPad, 9) + 0.5f, width: 20f, height: 1f);

        // Mode swap zone: row 11, col 12 → tile x = -30+24 = -6
        CreateModeSwapZone(roomGO, "ModeSwapZone_15",
            localX: -6f, localY: TileY(topPad, 11) + 2f);

        // Shootable target: row 4, col 12 → tile x = -30+24 = -6
        // Placed on right path so gun-mode players aim at it while clearing the pit.
        CreateShootableTarget(roomGO, "Target_15",
            localX: -6f, localY: TileY(topPad, 4) + 1f);

        // Fuel pickup: row 10, col 18 → tile x = -30+36 = 6
        // Placed to reward right-path players who need more fuel for the higher exit.
        CreateFuelPickup(roomGO, "FuelPickup_15",
            localX: 6f, localY: TileY(topPad, 10) + 1f);

        // Dash pickup: row 11, col 16 → tile x = -30+32 = 2
        // Near spawn floor so player can grab it on the way to the gate fork.
        CreateDashPickup(roomGO, "DashPickup_15",
            localX: 2f, localY: TileY(topPad, 11) + 1f);

        Undo.RegisterCreatedObjectUndo(roomGO, "Create Room 15");
        Debug.Log("[BuildRooms12to15] Room 15 created.");
    }

    // =========================================================================
    // ROOM SHELL FACTORY
    // =========================================================================

    /// <summary>
    /// Creates the standard room hierarchy matching existing rooms 1-2:
    ///   Room_[id] (Room component)
    ///     SpawnPoint
    ///     Grid (Grid component)
    ///       Walls (Tilemap, TilemapRenderer, TilemapCollider2D, Rigidbody2D Static,
    ///              CompositeCollider2D Polygons — CRITICAL for dual-raycast ground check)
    /// </summary>
    static GameObject CreateRoomShell(string roomId, float centerX,
        out Room room, out Tilemap tilemap, out Transform spawnTF)
    {
        var roomGO = new GameObject("Room_" + roomId);
        roomGO.transform.position = new Vector3(centerX, 0f, 0f);
        room = roomGO.AddComponent<Room>();

        var spawnGO = new GameObject("SpawnPoint");
        spawnGO.transform.parent = roomGO.transform;
        spawnGO.transform.localPosition = Vector3.zero;
        spawnTF = spawnGO.transform;

        var gridGO = new GameObject("Grid");
        gridGO.transform.parent = roomGO.transform;
        gridGO.transform.localPosition = Vector3.zero;
        var grid = gridGO.AddComponent<Grid>();
        grid.cellSize = Vector3.one;

        var wallsGO = new GameObject("Walls");
        wallsGO.transform.parent = gridGO.transform;
        wallsGO.transform.localPosition = Vector3.zero;
        wallsGO.layer = 8; // Ground layer (must match Physics2D layer mask in PlayerController)

        tilemap = wallsGO.AddComponent<Tilemap>();
        wallsGO.AddComponent<TilemapRenderer>();

        var tmCollider = wallsGO.AddComponent<TilemapCollider2D>();
        tmCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

        // Static Rigidbody2D required for CompositeCollider2D to work.
        var rb = wallsGO.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        // CRITICAL: Polygons geometry prevents edge-collider artifacts that break
        // the dual-raycast ground check (normal.y > 0.7 rejects downward-facing edges).
        var composite = wallsGO.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

        return roomGO;
    }

    // =========================================================================
    // TILEMAP PAINTING HELPERS
    // Cell coordinates are in Tilemap local space (integer grid).
    // Room spans: x in [-30..29], y in [-17..16].
    // =========================================================================

    /// <summary>
    /// Returns the TOP tile y-coordinate for a given ASCII row index.
    /// ASCII row 0 is the ceiling row; rows increase downward.
    /// Formula: 16 - topPad - asciiRow * 2
    /// </summary>
    static int TileY(int topPad, int asciiRow)
    {
        return 16 - topPad - asciiRow * 2;
    }

    static void PaintRect(Tilemap tm, int xMin, int xMax, int yMin, int yMax)
    {
        for (int x = xMin; x <= xMax; x++)
            for (int y = yMin; y <= yMax; y++)
                tm.SetTile(new Vector3Int(x, y, 0), s_groundTile);
    }

    // Ceiling: topPad rows of solid tiles above ASCII row 0, plus ASCII row 0 itself.
    static void PaintFullCeiling(Tilemap tm, int topPad)
    {
        for (int pad = 0; pad < topPad; pad++)
        {
            int rowTop = 16 - pad * 2;
            PaintRect(tm, -30, 29, rowTop - 1, rowTop);
        }
        // ASCII row 0
        PaintRect(tm, -30, 29, TileY(topPad, 0) - 1, TileY(topPad, 0));
    }

    // Floor: bottom ASCII row plus any padding rows below it down to y = -17.
    static void PaintFullFloor(Tilemap tm, int topPad, int asciiRows)
    {
        int lastRow = asciiRows - 1;
        PaintRect(tm, -30, 29, TileY(topPad, lastRow) - 1, TileY(topPad, lastRow));
        int bottomEdge = TileY(topPad, lastRow) - 2;
        for (int y = bottomEdge; y >= -17; y--)
            PaintRect(tm, -30, 29, y, y);
    }

    // Full left wall: x = [-30..-29], all y.
    static void PaintLeftWall(Tilemap tm)
    {
        PaintRect(tm, -30, -29, -17, 16);
    }

    // Left wall with a custom gap [yLow..yHigh] cleared for room transition.
    static void PaintLeftWallWithOpeningCustom(Tilemap tm, int yLow, int yHigh)
    {
        for (int y = -17; y <= 16; y++)
        {
            if (y >= yLow && y <= yHigh) continue;
            PaintRect(tm, -30, -29, y, y);
        }
    }

    // Full right wall: x = [28..29], all y. Used for Room 15 (no exit).
    static void PaintRightWall(Tilemap tm)
    {
        PaintRect(tm, 28, 29, -17, 16);
    }

    // Right wall with a 6-tile opening [yLow..yHigh] for room transitions.
    static void PaintRightWallWithOpening(Tilemap tm, int yLow, int yHigh)
    {
        for (int y = -17; y <= 16; y++)
        {
            if (y >= yLow && y <= yHigh) continue;
            PaintRect(tm, 28, 29, y, y);
        }
    }

    // =========================================================================
    // SPRITE HELPERS
    // =========================================================================

    /// <summary>
    /// Creates a programmatic sprite matching worldW x worldH at 16 PPU.
    /// Used for hazard, gate, and zone visuals.
    /// </summary>
    static Sprite CreateSizedSprite(float worldW, float worldH, Color color)
    {
        int pixW = Mathf.Max(1, Mathf.RoundToInt(worldW * 16f));
        int pixH = Mathf.Max(1, Mathf.RoundToInt(worldH * 16f));
        var tex = new Texture2D(pixW, pixH);
        var pixels = new Color[pixW * pixH];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, pixW, pixH),
            new Vector2(0.5f, 0.5f), 16f);
    }

    /// <summary>
    /// Creates a 16x16 diamond-shaped sprite for pickups (matching existing pickup style).
    /// </summary>
    static Sprite CreateDiamondSprite(Color color)
    {
        var tex = new Texture2D(16, 16);
        tex.alphaIsTransparency = true;
        var pixels = new Color[256];
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                pixels[y * 16 + x] = (Mathf.Abs(x - 8) + Mathf.Abs(y - 8) <= 6) ? color : Color.clear;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
    }

    // =========================================================================
    // GAMEPLAY OBJECT FACTORIES
    // All localX/localY are in room-local space (parent = roomGO).
    // =========================================================================

    /// <summary>
    /// Creates a grouped spike hazard: red sprite, trigger BoxCollider2D, Hazard component.
    /// Group adjacent ASCII X tiles into a single call to keep the hierarchy clean.
    /// </summary>
    static void CreateHazard(GameObject roomGO, string name,
        float localX, float localY, float width, float height)
    {
        var go = new GameObject(name);
        go.transform.parent = roomGO.transform;
        go.transform.localPosition = new Vector3(localX, localY, 0f);
        go.layer = 10; // Hazard

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSizedSprite(width, height, Color.red);
        sr.color = Color.red;
        sr.sortingOrder = 5;

        var bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(width, height);

        go.AddComponent<Hazard>();
    }

    /// <summary>
    /// Creates a fuel gate barrier. Solid (not trigger) until FuelGate.cs opens it.
    /// Color matches exhaust gradient: cyan = High, orange = Mid, red = Low.
    /// </summary>
    static void CreateFuelGate(GameObject roomGO, string name, FuelTier tier,
        float localX, float localY, float gateWidth, float gateHeight)
    {
        var go = new GameObject(name);
        go.transform.parent = roomGO.transform;
        go.transform.localPosition = new Vector3(localX, localY, 0f);

        Color tierColor = tier switch
        {
            FuelTier.High => new Color(0f, 0.9f, 1f, 1f),    // cyan — matches High exhaust
            FuelTier.Mid  => new Color(1f, 0.6f, 0f, 1f),    // orange — matches Mid exhaust
            FuelTier.Low  => new Color(1f, 0.15f, 0.15f, 1f), // red — matches Low exhaust
            _             => Color.white
        };

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSizedSprite(gateWidth, gateHeight, Color.white);
        sr.color = tierColor;
        sr.sortingOrder = 5;

        var bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = false; // Solid barrier — FuelGate.cs disables this when condition is met
        bc.size = new Vector2(gateWidth, gateHeight);

        var fg = go.AddComponent<FuelGate>();
        fg.Init(tier);
    }

    /// <summary>
    /// Creates a fuel recharge pickup: cyan diamond, CircleCollider2D trigger, GasRechargePickup.
    /// Respawns when player lands (handled by GasRechargePickup.cs).
    /// </summary>
    static void CreateFuelPickup(GameObject roomGO, string name, float localX, float localY)
    {
        var go = new GameObject(name);
        go.transform.parent = roomGO.transform;
        go.transform.localPosition = new Vector3(localX, localY, 0f);
        go.layer = 11; // Collectible
        go.tag = "GasRecharge";

        var sr = go.AddComponent<SpriteRenderer>();
        var cyanColor = new Color(0f, 0.9f, 1f, 1f);
        sr.sprite = CreateDiamondSprite(cyanColor);
        sr.color = cyanColor;
        sr.sortingOrder = 5;

        var cc = go.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.5f;

        go.AddComponent<GasRechargePickup>();
    }

    /// <summary>
    /// Creates a dash recharge pickup: magenta diamond (rotated 45°), DashRechargePickup.
    /// Respawns when player lands (handled by DashRechargePickup.cs).
    /// </summary>
    static void CreateDashPickup(GameObject roomGO, string name, float localX, float localY)
    {
        var go = new GameObject(name);
        go.transform.parent = roomGO.transform;
        go.transform.localPosition = new Vector3(localX, localY, 0f);
        go.layer = 11; // Collectible

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateDiamondSprite(Color.magenta);
        sr.color = Color.magenta;
        sr.sortingOrder = 5;

        // 45° rotation gives the classic diamond orientation matching existing pickups
        go.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        var cc = go.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.5f;

        go.AddComponent<DashRechargePickup>();
    }

    /// <summary>
    /// Creates a mode swap zone visual indicator (green semi-transparent box).
    /// Actual SecondaryBooster.SwapMode(SecondaryMode.Gun) logic is deferred —
    /// wire it when BoosterSwapZone.cs is implemented.
    /// </summary>
    static void CreateModeSwapZone(GameObject roomGO, string name, float localX, float localY)
    {
        var go = new GameObject(name);
        go.transform.parent = roomGO.transform;
        go.transform.localPosition = new Vector3(localX, localY, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSizedSprite(4f, 4f, Color.white);
        sr.color = new Color(0f, 1f, 0f, 0.4f); // semi-transparent green
        sr.sortingOrder = 3;

        var bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(4f, 4f);

        Debug.Log($"[BuildRooms12to15] {name} created. TODO: wire SecondaryBooster.SwapMode(SecondaryMode.Gun) " +
                  "when BoosterSwapZone.cs is implemented.");
    }

    /// <summary>
    /// Creates a shootable target visual indicator (yellow circle).
    /// Actual hit detection is deferred — wire it when the interactive target system is designed.
    /// </summary>
    static void CreateShootableTarget(GameObject roomGO, string name, float localX, float localY)
    {
        var go = new GameObject(name);
        go.transform.parent = roomGO.transform;
        go.transform.localPosition = new Vector3(localX, localY, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSizedSprite(2f, 2f, Color.white);
        sr.color = Color.yellow;
        sr.sortingOrder = 5;

        var cc = go.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 1f;

        Debug.Log($"[BuildRooms12to15] {name} created. TODO: add hit detection component " +
                  "when interactive target system is designed.");
    }
}

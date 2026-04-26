// BuildRooms3to7.cs
// Implements design/levels/chapter1-room-designs.md — Rooms 3-7 (Tutorial sequence).
// Execute via Coplay MCP execute_script tool: BuildRooms3to7.Execute()
//
// Coordinate system:
//   Room N center X = (N-1) * 60, Y = 0.
//   Room bounds: x [-30..29], y [-17..16] relative to center (60×34 units).
//   ASCII grid scales 2× — each char → 2×2 tile block.
//   Standard wall opening (left/right): y = -8 to -3 (6 tiles tall).

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class BuildRooms3to7
{
    // ──────────────────────────────────────────────────────────────
    // Room ASCII layouts
    // '#' = solid tile, everything else = air.
    // S = spawn marker (air tile), E = exit marker (air tile),
    // X = hazard center (air tile, hazard GameObject placed separately),
    // F = fuel pickup (air tile, pickup GameObject placed separately),
    // D = dash pickup (air tile, pickup GameObject placed separately).
    // Spaces inside rows are also treated as air.
    // ──────────────────────────────────────────────────────────────

    // 30 cols × 14 rows
    private static readonly string[] Room3Layout = new string[]
    {
        "##############################",
        "#............................#",
        "#............................#",
        "#............................#",
        "#............................#",
        "#.S..........................#",
        "#.####.......####.......##E..#",
        "#......XXXXX......XXXXX......#",
        "#......#####......#####......#",
        "#............................#",
        "#............................#",
        "#............................#",
        "#............................#",
        "##############################",
    };

    // Room 4: "Liftoff" — must use upward jetpack boost to reach exit platform.
    // Exit sits on a high platform at row 3-4.
    // A connecting bridge (solid tiles) runs from exit platform to the right wall
    // so the standard right-wall opening leads somewhere walkable.
    private static readonly string[] Room4Layout = new string[]
    {
        "##############################",
        "#............................#",
        "#............................#",
        "#.............E..............#",
        "#............####............#",
        "#............................#",
        "#............................#",
        "#............................#",
        "#............................#",
        "#............................#",
        "#....####################....#",
        "#............................#",
        "#.S..........................#",
        "#............................#",   // row 13 is bottom border row
        "##############################",
    };
    // Note: row 13 above is intentionally kept as interior + solid bottom border.
    // The layout is 15 rows here — we handle height mismatch in the painter.

    // Room 5: "Four Directions"
    private static readonly string[] Room5Layout = new string[]
    {
        "##############################",
        "#............................#",
        "#.####.......................#",
        "#..........####..............#",
        "#....................####....#",
        "#............................#",
        "#............................#",
        "#....................####.E..#",
        "#.S..........................#",
        "#.####.......####............#",
        "#............................#",
        "#............................#",
        "#............................#",
        "##############################",
    };

    // Room 6: "Fuel Awareness"
    // Long spike pit. Mid-platform for landing and refueling.
    // F marks a fuel pickup above the midpoint platform (row 6, col 15).
    private static readonly string[] Room6Layout = new string[]
    {
        "##############################",
        "#............................#",
        "#............................#",
        "#............................#",
        "#............................#",
        "#............................#",
        "#....F.......................#",
        "#.S..............##.......E..#",
        "#.####...............####.###",
        "#....XXXXXXXXXXXXXXXX........#",
        "#....##################......#",
        "#............................#",
        "#............................#",
        "##############################",
    };

    // Room 7: "Quick Burst" — dash intro.
    // Three small platforms over 3-tile spike pits.
    // D marks a dash pickup above the second platform.
    private static readonly string[] Room7Layout = new string[]
    {
        "##############################",
        "#............................#",
        "#............................#",
        "#............................#",
        "#............................#",
        "#............................#",
        "#............................#",
        "#.S.......D..........D.....E.#",
        "#.####.###...###...###...####",
        "#..........XXX...XXX...XXX...#",
        "#..........###...###...###...#",
        "#............................#",
        "#............................#",
        "##############################",
    };

    // Standard wall opening range (y in tilemap space, relative to room center).
    // Covers 6 tiles: y = -8, -7, -6, -5, -4, -3.
    private const int OpeningYMin = -8;
    private const int OpeningYMax = -3;

    // ──────────────────────────────────────────────────────────────
    // Entry point
    // ──────────────────────────────────────────────────────────────
    public static void Execute()
    {
        var tile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/GroundTile.asset");
        if (tile == null)
        {
            Debug.LogError("[BuildRooms3to7] Could not load Assets/Tiles/GroundTile.asset. Aborting.");
            return;
        }

        BuildRoom(3, Room3Layout,  tile, spawnCol: 2,  spawnRow: 5,  pickups: null, hazards: BuildRoom3Hazards());
        BuildRoom(4, Room4Layout,  tile, spawnCol: 2,  spawnRow: 12, pickups: null, hazards: null,            room4Bridge: true);
        BuildRoom(5, Room5Layout,  tile, spawnCol: 2,  spawnRow: 8,  pickups: null, hazards: null);
        BuildRoom(6, Room6Layout,  tile, spawnCol: 2,  spawnRow: 7,  pickups: BuildRoom6Pickups(), hazards: BuildRoom6Hazards());
        BuildRoom(7, Room7Layout,  tile, spawnCol: 2,  spawnRow: 7,  pickups: BuildRoom7Pickups(), hazards: BuildRoom7Hazards());

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[BuildRooms3to7] Done. Rooms 3-7 created. Save the scene when ready.");
    }

    // ──────────────────────────────────────────────────────────────
    // Pickup / hazard descriptor structs
    // ──────────────────────────────────────────────────────────────

    private struct HazardDesc
    {
        public float LocalX, LocalY;   // room-local position (center of hazard)
        public float Width, Height;
        public string Name;
    }

    private struct PickupDesc
    {
        public float LocalX, LocalY;
        public string Name;
        public bool IsDash;            // false = fuel, true = dash
    }

    // ──────────────────────────────────────────────────────────────
    // Per-room hazard / pickup descriptors
    // ──────────────────────────────────────────────────────────────

    // Room 3: two 5-tile spike strips at row 7 (scaled: y = 16 - topPad - 7*2).
    // ASCII rows 14 tall → topPad = (34 - 14*2) / 2 = 3.
    // Row 7, scaled Y top = 16 - 3 - 14 = -1; bottom = -2. Center Y = -1.5.
    // Spike strips start at scaled col 6 (x = -30 + 6*2 = -18), width 5 chars = 10 tiles.
    // Second strip: col 18 (x = -30 + 18*2 = 6), same width.
    private static HazardDesc[] BuildRoom3Hazards() => new HazardDesc[]
    {
        new HazardDesc { Name = "Spikes_1", LocalX = -13f, LocalY = -1.5f, Width = 10f, Height = 1f },
        new HazardDesc { Name = "Spikes_2", LocalX =   11f, LocalY = -1.5f, Width = 10f, Height = 1f },
    };

    // Room 6: wide spike pit at rows 9-10 (scaled).
    // topPad = (34 - 14*2) / 2 = 3.
    // Row 9 scaled top Y: 16 - 3 - 18 = -5; row 10 bottom Y: 16 - 3 - 22 = -9.
    // Pit x range: col 4 to 23 = x = -30+8 = -22 to -30+46 = 16. Width = 38, center x = -3.
    private static HazardDesc[] BuildRoom6Hazards() => new HazardDesc[]
    {
        new HazardDesc { Name = "Spikes_1", LocalX = -3f, LocalY = -7f, Width = 38f, Height = 4f },
    };

    // Room 7: three 3-tile spike pits at row 9 (scaled).
    // topPad = (34 - 14*2)/2 = 3.
    // Row 9 scaled top Y: 16 - 3 - 18 = -5; bottom: 16 - 3 - 20 = -7. Center Y = -6.
    // Pit 1: cols 10-12 → x = -30+20 = -10, width 6.
    // Pit 2: cols 16-18 → x = -30+32 = 2, width 6.
    // Pit 3: cols 22-24 → x = -30+44 = 14, width 6.
    private static HazardDesc[] BuildRoom7Hazards() => new HazardDesc[]
    {
        new HazardDesc { Name = "Spikes_1", LocalX = -7f,  LocalY = -6f, Width = 6f, Height = 2f },
        new HazardDesc { Name = "Spikes_2", LocalX =  5f,  LocalY = -6f, Width = 6f, Height = 2f },
        new HazardDesc { Name = "Spikes_3", LocalX = 17f,  LocalY = -6f, Width = 6f, Height = 2f },
    };

    // Room 6: fuel pickup above midpoint platform.
    // F is at ASCII row 6, col 6 → scaled x = -30+12 = -18, topPad=3 → y = 16-3-12 = 1.
    private static PickupDesc[] BuildRoom6Pickups() => new PickupDesc[]
    {
        new PickupDesc { Name = "FuelPickup_1", LocalX = -18f, LocalY = 1f, IsDash = false },
    };

    // Room 7: dash pickup above second small platform.
    // D at row 7, col 10 → x = -30+20 = -10, topPad=3 → y = 16-3-14 = -1.
    // Second D at row 7, col 21 → x = -30+42 = 12, same y.
    private static PickupDesc[] BuildRoom7Pickups() => new PickupDesc[]
    {
        new PickupDesc { Name = "DashPickup_1", LocalX = -10f, LocalY = -1f, IsDash = true },
        new PickupDesc { Name = "DashPickup_2", LocalX = 12f,  LocalY = -1f, IsDash = true },
    };

    // ──────────────────────────────────────────────────────────────
    // Core room builder
    // ──────────────────────────────────────────────────────────────

    private static void BuildRoom(
        int roomNumber,
        string[] ascii,
        TileBase tile,
        int spawnCol,
        int spawnRow,
        PickupDesc[] pickups,
        HazardDesc[] hazards,
        bool room4Bridge = false)
    {
        string goName = $"Room_0{roomNumber}";
        float centerX = (roomNumber - 1) * 60f;

        // Guard: destroy existing room with same name to avoid ghost duplicates.
        var existing = GameObject.Find(goName);
        if (existing != null)
        {
            Debug.LogWarning($"[BuildRooms3to7] '{goName}' already exists — destroying and rebuilding.");
            Undo.DestroyObjectImmediate(existing);
        }

        // ── Room root ──
        var roomGO = new GameObject(goName);
        Undo.RegisterCreatedObjectUndo(roomGO, $"Create {goName}");
        roomGO.transform.position = new Vector3(centerX, 0f, 0f);

        var room = roomGO.AddComponent<Room>();

        // ── Spawn point ──
        var spawnGO = new GameObject("SpawnPoint");
        spawnGO.transform.parent = roomGO.transform;

        // Convert ASCII (spawnCol, spawnRow) to room-local position.
        int asciiH = ascii.Length;
        int topPad = (34 - asciiH * 2) / 2;
        float spawnLocalX = -30f + spawnCol * 2f + 1f;   // +1 to center in the 2-tile block
        float spawnLocalY = 16f - topPad - spawnRow * 2f - 1f;
        spawnGO.transform.localPosition = new Vector3(spawnLocalX, spawnLocalY, 0f);

        // ── Room component init ──
        room.Init($"ch1-room-0{roomNumber}", new Vector2(60f, 34f), spawnGO.transform);

        // ── Grid ──
        var gridGO = new GameObject("Grid");
        gridGO.transform.parent = roomGO.transform;
        gridGO.transform.localPosition = Vector3.zero;
        var gridComp = gridGO.AddComponent<Grid>();
        gridComp.cellSize = Vector3.one;

        // ── Walls (Tilemap) ──
        var wallsGO = new GameObject("Walls");
        wallsGO.transform.parent = gridGO.transform;
        wallsGO.transform.localPosition = Vector3.zero;
        wallsGO.layer = 8; // Ground

        var tilemap = wallsGO.AddComponent<Tilemap>();
        wallsGO.AddComponent<TilemapRenderer>();

        var tmCollider = wallsGO.AddComponent<TilemapCollider2D>();
        tmCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

        var rb = wallsGO.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        var composite = wallsGO.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons; // CRITICAL

        // ── Paint tiles ──
        PaintRoom(tilemap, tile, ascii, topPad, room4Bridge);

        // ── Hazards ──
        if (hazards != null)
        {
            foreach (var h in hazards)
                CreateHazard(roomGO, h);
        }

        // ── Pickups ──
        if (pickups != null)
        {
            foreach (var p in pickups)
                CreatePickup(roomGO, p);
        }

        Debug.Log($"[BuildRooms3to7] Built {goName} at x={centerX}.");
    }

    // ──────────────────────────────────────────────────────────────
    // Tile painter
    // ──────────────────────────────────────────────────────────────

    private static void PaintRoom(Tilemap tilemap, TileBase tile, string[] ascii, int topPad, bool room4Bridge)
    {
        int asciiH = ascii.Length;

        // Paint top padding rows (solid walls above ASCII grid).
        for (int padRow = 0; padRow < topPad; padRow++)
        {
            int tileY = 16 - padRow;
            for (int tx = -30; tx <= 29; tx++)
                tilemap.SetTile(new Vector3Int(tx, tileY, 0), tile);
        }

        // Paint bottom padding rows (solid walls below ASCII grid).
        int bottomStart = 16 - topPad - asciiH * 2;
        for (int tileY = bottomStart; tileY >= -17; tileY--)
        {
            for (int tx = -30; tx <= 29; tx++)
                tilemap.SetTile(new Vector3Int(tx, tileY, 0), tile);
        }

        // Paint ASCII rows (each char → 2×2 block).
        for (int r = 0; r < asciiH; r++)
        {
            string row = ascii[r];
            int rowLen = row.Length;

            for (int c = 0; c < 30; c++)
            {
                char ch = (c < rowLen) ? row[c] : '.';
                bool isSolid = (ch == '#');

                if (!isSolid) continue;

                int tileX = -30 + c * 2;
                int tileY = 16 - topPad - r * 2;

                tilemap.SetTile(new Vector3Int(tileX,     tileY,     0), tile);
                tilemap.SetTile(new Vector3Int(tileX + 1, tileY,     0), tile);
                tilemap.SetTile(new Vector3Int(tileX,     tileY - 1, 0), tile);
                tilemap.SetTile(new Vector3Int(tileX + 1, tileY - 1, 0), tile);
            }
        }

        // Room 4 extra: connecting bridge from exit platform to the right wall.
        // Room 4 layout has 15 rows → topPad = (34-30)/2 = 2.
        // Exit platform ("####") is at ASCII row 4, cols 12-15.
        //   tileY (top tile) = 16 - 2 - 4*2 = 6.  Player walks on y=6 surface.
        //   Scaled right edge of platform: tileX = -30 + 16*2 = 2.
        // Bridge: two tile rows (y=6 and y=5) from tileX=2 to tileX=27 (right wall interior).
        if (room4Bridge)
        {
            int bridgeY = 16 - topPad - 4 * 2; // = 6 for Room 4's topPad=2
            for (int tx = 2; tx <= 27; tx++)
            {
                tilemap.SetTile(new Vector3Int(tx, bridgeY,     0), tile);
                tilemap.SetTile(new Vector3Int(tx, bridgeY - 1, 0), tile);
            }
        }

        // Carve wall openings (both left and right walls).
        ClearOpening(tilemap, leftWall: true);
        ClearOpening(tilemap, leftWall: false);
    }

    // Clears a 2-tile-wide, 6-tile-tall opening in the left or right border wall.
    // Y range: OpeningYMin to OpeningYMax (inclusive).
    private static void ClearOpening(Tilemap tilemap, bool leftWall)
    {
        for (int ty = OpeningYMin; ty <= OpeningYMax; ty++)
        {
            if (leftWall)
            {
                tilemap.SetTile(new Vector3Int(-30, ty, 0), null);
                tilemap.SetTile(new Vector3Int(-29, ty, 0), null);
            }
            else
            {
                tilemap.SetTile(new Vector3Int(28, ty, 0), null);
                tilemap.SetTile(new Vector3Int(29, ty, 0), null);
            }
        }
    }

    // ──────────────────────────────────────────────────────────────
    // Hazard factory
    // ──────────────────────────────────────────────────────────────

    private static void CreateHazard(GameObject roomGO, HazardDesc desc)
    {
        var go = new GameObject(desc.Name);
        go.transform.parent = roomGO.transform;
        go.transform.localPosition = new Vector3(desc.LocalX, desc.LocalY, 0f);
        go.layer = 10; // Hazard

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSquareSprite();
        sr.color = Color.red;
        sr.sortingOrder = 5;

        var bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(desc.Width, desc.Height);

        go.AddComponent<Hazard>();
    }

    // ──────────────────────────────────────────────────────────────
    // Pickup factory
    // ──────────────────────────────────────────────────────────────

    private static void CreatePickup(GameObject roomGO, PickupDesc desc)
    {
        var go = new GameObject(desc.Name);
        go.transform.parent = roomGO.transform;
        go.transform.localPosition = new Vector3(desc.LocalX, desc.LocalY, 0f);
        go.layer = 11; // Collectible

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSquareSprite();
        sr.color = desc.IsDash ? Color.magenta : new Color(0f, 0.9f, 1f);
        sr.sortingOrder = 5;

        // Diamond rotation (45 degrees) — matches existing pickups in rooms 1-2.
        go.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        var cc = go.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.5f;

        if (desc.IsDash)
            go.AddComponent<DashRechargePickup>();
        else
            go.AddComponent<GasRechargePickup>();
    }

    // ──────────────────────────────────────────────────────────────
    // Shared sprite utility — 16×16 white square, Point filter, 16 PPU.
    // ──────────────────────────────────────────────────────────────

    private static Sprite MakeSquareSprite()
    {
        var tex = new Texture2D(16, 16);
        var pixels = new Color[256];
        for (int i = 0; i < 256; i++)
            pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
    }
}
#endif

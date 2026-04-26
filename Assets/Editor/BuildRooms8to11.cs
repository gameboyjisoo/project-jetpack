// BuildRooms8to11.cs
// Implements: design/levels/chapter1-room-designs.md — Rooms 8-11
// Rooms 8: "Dash + Jetpack", 9: "Red Gate", 10: "Drain and Dodge", 11: "Blue Gate"
//
// Execute via Coplay MCP execute_script tool.
// Entry point: BuildRooms8to11.Execute()

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class BuildRooms8to11
{
    // -------------------------------------------------------------------------
    // ASCII layouts — 30 cols wide x 14 rows tall
    // Each character scales 2x to fill the 60x34 room.
    // Legend: # = wall, . = air, S = spawn, E = exit, X = hazard,
    //         F = fuel pickup, D = dash pickup,
    //         GL = low fuel gate (red),   GH = high fuel gate (cyan)
    //         | = gate body column (paired with GL/GH row above)
    // -------------------------------------------------------------------------

    // Room 8 — "Dash + Jetpack"
    // Wide spike gap; narrow landing platform with right-side spike wall.
    // Dash pickup floats mid-air above the spike gap.
    private static readonly string[] kRoom8 = new string[]
    {
        "##############################",
        "#............................#",
        "#............................#",
        "#............................#",
        "#............................#",
        "#.S..........................#",
        "#.####...................##E.#",
        "#.....XXXXXXXXXXXXXXXX..X....#",
        "#.....##################X....#",
        "#.......................X....#",
        "#............................#",
        "#............................#",
        "#............................#",
        "##############################",
    };

    // Room 9 — "Red Gate"
    // Flat room, no hazards. Red/Low gate blocks the middle path.
    // GL marker row sets the gate header; | rows below are the gate body.
    private static readonly string[] kRoom9 = new string[]
    {
        "##############################",
        "#............................#",
        "#............................#",
        "#............................#",
        "#............................#",
        "#..........GL................#",
        "#..........||................#",
        "#.S........||.............E..#",
        "#.####.....||.............####",
        "#..........||................#",
        "#..........||................#",
        "#............................#",
        "#............................#",
        "##############################",
    };

    // Room 10 — "Drain and Dodge"
    // Spike clusters scattered before a red gate. Maneuvering IS fuel spending.
    private static readonly string[] kRoom10 = new string[]
    {
        "##############################",
        "#............................#",
        "#.....XXXXX..................#",
        "#............................#",
        "#..............XXXXX.........#",
        "#............................#",
        "#.S.......GL.............E..#",
        "#.####....||..............####",
        "#....XXXXX||.................#",
        "#....#####||.................#",
        "#..........XXXXX.............#",
        "#............................#",
        "#............................#",
        "##############################",
    };

    // Room 11 — "Blue Gate"
    // Spike floor forces flight. High/Cyan gate — must conserve fuel (stay above 50%).
    private static readonly string[] kRoom11 = new string[]
    {
        "##############################",
        "#............................#",
        "#............................#",
        "#............................#",
        "#.............GH.............#",
        "#.............||.............#",
        "#.S...........||..........E..#",
        "#.####........||..........####",
        "#.............||.............#",
        "#XXXXXXXXXXXXXXXXXXXXXX......#",
        "#######################......#",
        "#............................#",
        "#............................#",
        "##############################",
    };

    // -------------------------------------------------------------------------
    // Entry point
    // -------------------------------------------------------------------------
    public static void Execute()
    {
        var tile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/GroundTile.asset");
        if (tile == null)
        {
            Debug.LogError("[BuildRooms8to11] Could not load Assets/Tiles/GroundTile.asset. Aborting.");
            return;
        }

        BuildRoom(8,  420, kRoom8,  "ch1-room-08", tile);
        BuildRoom(9,  480, kRoom9,  "ch1-room-09", tile);
        BuildRoom(10, 540, kRoom10, "ch1-room-10", tile);
        BuildRoom(11, 600, kRoom11, "ch1-room-11", tile);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[BuildRooms8to11] Done. Rooms 8-11 created. Save the scene manually (Ctrl+S).");
    }

    // -------------------------------------------------------------------------
    // Per-room builder
    // -------------------------------------------------------------------------
    private static void BuildRoom(int roomNumber, float centerX, string[] ascii, string roomId, TileBase tile)
    {
        // Guard: skip if a room with this ID already exists
        var existing = Object.FindObjectsByType<Room>(FindObjectsSortMode.None);
        foreach (var r in existing)
        {
            if (r.RoomId == roomId)
            {
                Debug.LogWarning($"[BuildRooms8to11] Room '{roomId}' already exists — skipping.");
                return;
            }
        }

        // ---- Root Room GameObject ------------------------------------------
        var roomGO = new GameObject($"Room_{roomNumber:D2}");
        roomGO.transform.position = new Vector3(centerX, 0f, 0f);
        Undo.RegisterCreatedObjectUndo(roomGO, $"Create {roomId}");

        var room = roomGO.AddComponent<Room>();

        // ---- Spawn point ------------------------------------------------------
        // Find the 'S' character in ASCII and compute world position
        Vector3 spawnLocal = FindSpecialCharLocal(ascii, 'S', centerX);
        var spawnGO = new GameObject("SpawnPoint");
        spawnGO.transform.parent = roomGO.transform;
        spawnGO.transform.position = new Vector3(centerX + spawnLocal.x, spawnLocal.y, 0f);

        room.Init(roomId, new Vector2(60f, 34f), spawnGO.transform);

        // ---- Grid + Tilemap --------------------------------------------------
        var gridGO = new GameObject("Grid");
        gridGO.transform.parent = roomGO.transform;
        gridGO.transform.localPosition = Vector3.zero;
        gridGO.AddComponent<Grid>();

        var wallsGO = new GameObject("Walls");
        wallsGO.transform.parent = gridGO.transform;
        wallsGO.transform.localPosition = Vector3.zero;
        wallsGO.layer = 8; // Ground

        var tilemap = wallsGO.AddComponent<Tilemap>();
        wallsGO.AddComponent<TilemapRenderer>();

        var tc2d = wallsGO.AddComponent<TilemapCollider2D>();
        tc2d.compositeOperation = Collider2D.CompositeOperation.Merge;

        var rb2d = wallsGO.AddComponent<Rigidbody2D>();
        rb2d.bodyType = RigidbodyType2D.Static;

        var cc2d = wallsGO.AddComponent<CompositeCollider2D>();
        cc2d.geometryType = CompositeCollider2D.GeometryType.Polygons; // CRITICAL — Outlines break ground check

        // ---- Paint tiles from ASCII ------------------------------------------
        PaintTiles(tilemap, tile, ascii, centerX);

        // ---- Carve exit openings (6 tiles tall, y = -8 to -3) ---------------
        ClearExitOpenings(tilemap);

        // ---- Place interactables from ASCII ----------------------------------
        PlaceInteractables(roomGO, ascii, centerX, roomNumber);

        Debug.Log($"[BuildRooms8to11] Built {roomId} at centerX={centerX}.");
    }

    // -------------------------------------------------------------------------
    // Tile painting
    // ASCII: 30 cols x 14 rows  →  60x28 tiles (2x scale), centered in 60x34
    // topPad = (34 - 14*2) / 2 = 3 rows of solid wall at top and bottom
    // ASCII row r, col c → tile at local (x=-30+c*2, y=13-r*2) for the top-left
    //   of the 2×2 block: also (x+1,y), (x,y-1), (x+1,y-1)
    // -------------------------------------------------------------------------
    private const int kAsciiCols   = 30;
    private const int kAsciiRows   = 14;
    private const int kRoomWidth   = 60;
    private const int kRoomHeight  = 34;
    private const int kTopPad      = (kRoomHeight - kAsciiRows * 2) / 2; // = 3

    // Tile-space: x in [-30, 29], y in [-17, 16]
    // The Tilemap origin is at the room center, so tile cell (cx, cy) maps to
    // local position (cx + 0.5, cy + 0.5). We paint in cell coordinates.

    private static void PaintTiles(Tilemap tilemap, TileBase tile, string[] ascii, float centerX)
    {
        int roomHalfH = kRoomHeight / 2; // 17

        // Top solid pad: rows y = 16 down to (16 - kTopPad + 1) = 14
        for (int ty = roomHalfH - 1; ty >= roomHalfH - kTopPad; ty--)
            for (int tx = -kRoomWidth / 2; tx < kRoomWidth / 2; tx++)
                tilemap.SetTile(new Vector3Int(tx, ty, 0), tile);

        // Bottom solid pad: rows y = -roomHalfH to (-roomHalfH + kTopPad - 1) = -17 to -15
        for (int ty = -roomHalfH; ty < -roomHalfH + kTopPad; ty++)
            for (int tx = -kRoomWidth / 2; tx < kRoomWidth / 2; tx++)
                tilemap.SetTile(new Vector3Int(tx, ty, 0), tile);

        // ASCII interior (rows 0..13, cols 0..29)
        for (int r = 0; r < kAsciiRows; r++)
        {
            if (r >= ascii.Length) break;
            string row = ascii[r];

            for (int c = 0; c < kAsciiCols; c++)
            {
                if (c >= row.Length) break;
                char ch = row[c];

                // Only '#' characters become solid tiles.
                // 'X', 'S', 'E', 'F', 'D', 'G', '|', '.' are handled separately or ignored.
                bool isSolid = (ch == '#');

                // The GL/GH marker cells and | gate-body cells should NOT become tiles
                // (the gate is a separate GameObject). Treat them as air here.

                if (!isSolid) continue;

                // Compute the top-left tile of the 2×2 block
                int tx = -30 + c * 2;
                int ty = 13 - r * 2; // top tile of the block

                tilemap.SetTile(new Vector3Int(tx,     ty,     0), tile);
                tilemap.SetTile(new Vector3Int(tx + 1, ty,     0), tile);
                tilemap.SetTile(new Vector3Int(tx,     ty - 1, 0), tile);
                tilemap.SetTile(new Vector3Int(tx + 1, ty - 1, 0), tile);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Carve 6-tile-tall exit openings in left and right walls
    // Opening y range in tile coords: -8 to -3 (inclusive)
    // Left wall tiles: x = -30, -29
    // Right wall tiles: x = 28, 29
    // -------------------------------------------------------------------------
    private static void ClearExitOpenings(Tilemap tilemap)
    {
        for (int ty = -8; ty <= -3; ty++)
        {
            // Left opening
            tilemap.SetTile(new Vector3Int(-30, ty, 0), null);
            tilemap.SetTile(new Vector3Int(-29, ty, 0), null);
            // Right opening
            tilemap.SetTile(new Vector3Int(28, ty, 0), null);
            tilemap.SetTile(new Vector3Int(29, ty, 0), null);
        }
    }

    // -------------------------------------------------------------------------
    // Interactables: hazards (X), fuel pickups (F), dash pickups (D),
    //                fuel gates (GL/GH)
    // -------------------------------------------------------------------------
    private static void PlaceInteractables(GameObject roomGO, string[] ascii, float centerX, int roomNumber)
    {
        // We scan the ASCII for:
        //   - Contiguous runs of 'X' on the same row → grouped hazard
        //   - 'F' → fuel pickup
        //   - 'D' → dash pickup
        //   - "GL" or "GH" starting a gate header (next rows with '|' = body extent)

        int hazardIndex  = 0;
        int fuelIndex    = 0;
        int dashIndex    = 0;
        int gateIndex    = 0;

        // ----- Hazards (row-grouped runs of 'X') -----
        for (int r = 0; r < kAsciiRows && r < ascii.Length; r++)
        {
            string row = ascii[r];
            int c = 0;
            while (c < row.Length)
            {
                if (row[c] == 'X')
                {
                    // Find extent of this contiguous X run
                    int start = c;
                    while (c < row.Length && row[c] == 'X') c++;
                    int end = c - 1; // inclusive

                    // Local center of the hazard (in 2x-scaled tile space, relative to room center)
                    float localX = AsciiColToLocal(start, end);
                    float localY = AsciiRowToLocal(r);
                    float width  = (end - start + 1) * 2f; // each ASCII col = 2 tiles wide
                    float height = 2f;                       // one ASCII row = 2 tiles tall

                    SpawnHazard(roomGO, hazardIndex++, localX, localY, width, height);
                }
                else
                {
                    c++;
                }
            }
        }

        // ----- Fuel pickups ('F') -----
        for (int r = 0; r < kAsciiRows && r < ascii.Length; r++)
        {
            string row = ascii[r];
            for (int c = 0; c < row.Length; c++)
            {
                if (row[c] == 'F')
                {
                    float localX = AsciiColToLocal(c, c);
                    float localY = AsciiRowToLocal(r);
                    SpawnFuelPickup(roomGO, fuelIndex++, localX, localY);
                }
            }
        }

        // ----- Dash pickups ('D') -----
        for (int r = 0; r < kAsciiRows && r < ascii.Length; r++)
        {
            string row = ascii[r];
            for (int c = 0; c < row.Length; c++)
            {
                if (row[c] == 'D')
                {
                    float localX = AsciiColToLocal(c, c);
                    float localY = AsciiRowToLocal(r);
                    SpawnDashPickup(roomGO, dashIndex++, localX, localY);
                }
            }
        }

        // ----- Fuel gates (GL / GH) -----
        // Scan for the two-char tokens "GL" or "GH" in the ASCII.
        // The column where "GL"/"GH" starts defines the gate's horizontal center.
        // Count consecutive rows of '|' below to determine gate body height.
        for (int r = 0; r < kAsciiRows && r < ascii.Length; r++)
        {
            string row = ascii[r];
            for (int c = 0; c < row.Length - 1; c++)
            {
                if ((row[c] == 'G') && (row[c + 1] == 'L' || row[c + 1] == 'H'))
                {
                    FuelTier tier = (row[c + 1] == 'H') ? FuelTier.High : FuelTier.Low;

                    // Gate header col c is the 'G'; the gate spans cols c and c+1 (= "GL" / "GH")
                    // Gate horizontal center = AsciiColToLocal(c, c+1)
                    float gateLocalX = AsciiColToLocal(c, c + 1);

                    // Count '|' rows below to determine vertical extent
                    int pipeRows = 0;
                    for (int pr = r + 1; pr < kAsciiRows && pr < ascii.Length; pr++)
                    {
                        if (pr < ascii[pr].Length && ascii[pr].Length > c && ascii[pr][c] == '|')
                            pipeRows++;
                        else
                            break;
                    }

                    // Gate body spans from row r+1 to r+pipeRows (inclusive)
                    // Vertical center of body
                    int bodyTopRow    = r + 1;
                    int bodyBottomRow = r + pipeRows;
                    // clamp
                    if (pipeRows == 0) { bodyTopRow = r; bodyBottomRow = r; }

                    float gateBodyTopLocal    = AsciiRowToLocal(bodyTopRow) + 1f; // +1 = top of top tile
                    float gateBodyBottomLocal = AsciiRowToLocal(bodyBottomRow) - 1f; // -1 = bottom of bottom tile
                    float gateCenterY         = (gateBodyTopLocal + gateBodyBottomLocal) * 0.5f;
                    float gateHeight          = gateBodyTopLocal - gateBodyBottomLocal;
                    // minimum height of 4 tiles if pipeRows was 0
                    if (gateHeight < 4f) gateHeight = 4f;

                    // Width: 2 ASCII cols = 4 tiles
                    float gateWidth = 4f;

                    SpawnFuelGate(roomGO, gateIndex++, gateLocalX, gateCenterY, gateWidth, gateHeight, tier);

                    c++; // skip the 'L'/'H' character
                }
            }
        }

        // ----- Room 8 special: mid-air dash pickup over the spike gap -----
        // The ASCII for Room 8 has no explicit 'D' marker; add it programmatically.
        // Spike gap starts at col 5, ends at col 24 (approx). Place pickup at col 14 (center), row 6.
        if (roomNumber == 8)
        {
            // Row 6 in ASCII = "S.####...................##E."
            // The spike gap is roughly cols 5-23. Mid-point col = 14.
            // Float it 1 ASCII row above the spikes (row 6 in ASCII = gap row, place at row 5 above it)
            // Row 5 = "#.S.........................#"  — row 5 is the spawn row.
            // Better: row 5, col 14 = open air above the gap.
            float pickupLocalX = AsciiColToLocal(14, 14);
            float pickupLocalY = AsciiRowToLocal(5);
            SpawnDashPickup(roomGO, dashIndex++, pickupLocalX, pickupLocalY);
        }
    }

    // -------------------------------------------------------------------------
    // Coordinate helpers
    // ASCII col range [startCol..endCol] (inclusive) → local X center (relative to room center)
    // Each ASCII col = 2 tile units wide; room x starts at -30
    // -------------------------------------------------------------------------
    private static float AsciiColToLocal(int startCol, int endCol)
    {
        float leftEdge  = -30f + startCol * 2f;
        float rightEdge = -30f + (endCol + 1) * 2f;
        return (leftEdge + rightEdge) * 0.5f;
    }

    // ASCII row r → local Y center of the 2×2 block
    // topPad = 3 rows of solid wall. Block top tile y = 13 - r*2; bottom = 12 - r*2
    // Center Y = 13 - r*2 - 0.5 = 12.5 - r*2
    private static float AsciiRowToLocal(int row)
    {
        return 12.5f - row * 2f;
    }

    // Find first occurrence of 'ch' in ASCII, return local offset from room center
    private static Vector3 FindSpecialCharLocal(string[] ascii, char ch, float centerX)
    {
        for (int r = 0; r < ascii.Length; r++)
        {
            for (int c = 0; c < ascii[r].Length; c++)
            {
                if (ascii[r][c] == ch)
                {
                    float localX = AsciiColToLocal(c, c);
                    float localY = AsciiRowToLocal(r);
                    return new Vector3(localX, localY, 0f);
                }
            }
        }
        return Vector3.zero;
    }

    // -------------------------------------------------------------------------
    // Shared sprite factory — 16x16 white square (matches other rooms)
    // -------------------------------------------------------------------------
    private static Sprite MakeSquareSprite()
    {
        var tex = new Texture2D(16, 16);
        var pixels = new Color[256];
        for (int i = 0; i < 256; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
    }

    // -------------------------------------------------------------------------
    // Hazard spawner — one BoxCollider2D covering the full run
    // -------------------------------------------------------------------------
    private static void SpawnHazard(GameObject roomGO, int index, float localX, float localY,
                                    float width, float height)
    {
        var go = new GameObject($"Spikes_{index}");
        go.transform.parent = roomGO.transform;
        go.transform.localPosition = new Vector3(localX, localY, 0f);
        go.layer = 10; // Hazard

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSquareSprite();
        sr.color  = Color.red;
        sr.size   = new Vector2(width, height);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.sortingOrder = 5;

        var bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(width, height);

        go.AddComponent<Hazard>();
    }

    // -------------------------------------------------------------------------
    // Fuel pickup spawner
    // -------------------------------------------------------------------------
    private static void SpawnFuelPickup(GameObject roomGO, int index, float localX, float localY)
    {
        var go = new GameObject($"FuelPickup_{index}");
        go.transform.parent = roomGO.transform;
        go.transform.localPosition = new Vector3(localX, localY, 0f);
        go.layer = 11; // Collectible

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSquareSprite();
        sr.color  = new Color(0f, 0.9f, 1f); // Cyan — matches High exhaust color
        sr.sortingOrder = 5;

        var cc = go.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.5f;

        go.AddComponent<GasRechargePickup>();
    }

    // -------------------------------------------------------------------------
    // Dash pickup spawner
    // -------------------------------------------------------------------------
    private static void SpawnDashPickup(GameObject roomGO, int index, float localX, float localY)
    {
        var go = new GameObject($"DashPickup_{index}");
        go.transform.parent = roomGO.transform;
        go.transform.localPosition = new Vector3(localX, localY, 0f);
        go.layer = 11; // Collectible

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSquareSprite();
        sr.color  = Color.magenta;
        sr.sortingOrder = 5;

        var cc = go.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.5f;

        go.AddComponent<DashRechargePickup>();
    }

    // -------------------------------------------------------------------------
    // Fuel gate spawner
    // FuelGate requires SpriteRenderer + Collider2D ([RequireComponent]).
    // The gate's Start() will set the sprite color from the tier, so we just
    // provide a white square sprite here. Init() sets the tier field so Start()
    // reads it correctly.
    // -------------------------------------------------------------------------
    private static void SpawnFuelGate(GameObject roomGO, int index, float localX, float localY,
                                      float width, float height, FuelTier tier)
    {
        var go = new GameObject($"FuelGate_{TierName(tier)}_{index}");
        go.transform.parent = roomGO.transform;
        go.transform.localPosition = new Vector3(localX, localY, 0f);

        // SpriteRenderer required by FuelGate
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSquareSprite();
        sr.color  = TierColor(tier); // preview color; FuelGate.Start() will confirm
        sr.size   = new Vector2(width, height);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.sortingOrder = 5;

        // Collider required by FuelGate — solid, NOT a trigger (gates block until opened)
        var bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = false;
        bc.size = new Vector2(width, height);

        // FuelGate component — must add after SpriteRenderer and Collider2D
        var fg = go.AddComponent<FuelGate>();
        fg.Init(tier);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------
    private static string TierName(FuelTier tier)
    {
        return tier switch
        {
            FuelTier.High => "High",
            FuelTier.Mid  => "Mid",
            FuelTier.Low  => "Low",
            _             => "Unknown"
        };
    }

    private static Color TierColor(FuelTier tier)
    {
        return tier switch
        {
            FuelTier.High => new Color(0f, 0.9f, 1f),          // Cyan
            FuelTier.Mid  => new Color(1f, 0.6f, 0f),          // Orange
            FuelTier.Low  => new Color(1f, 0.15f, 0.15f, 1f),  // Red
            _             => Color.white
        };
    }
}
#endif

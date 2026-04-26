// BuildTutorialRooms.cs
// Implements the revised 4-room tutorial sequence (Rooms 3-6) replacing the old 13-room layout.
// Implements: design/levels/chapter1-room-designs.md (2026-04-25 revision)
// Execute via Coplay MCP execute_script: BuildTutorialRooms.Execute()
//
// Coordinate system:
//   Room N center X = (N-1) * 60, Y = 0.
//   Room 3 = (120,0), Room 4 = (180,0), Room 5 = (240,0), Room 6 = (300,0).
//   Room bounds: x [-30..29], y [-17..16] relative to center (60x34 units).
//   ASCII grid scales 2x — each char -> 2x2 tile block.
//   All ASCII grids are 14 rows -> topPad = (34 - 14*2) / 2 = 3.
//   Tile for ASCII (r, c):
//     tileX = -30 + c*2         (two tiles wide: tileX and tileX+1)
//     tileY = 16 - topPad - r*2 = 13 - r*2   (two tiles tall: tileY and tileY-1)
//
// Wall openings (left + right): y = -8 to -3, clearing x cols {-30,-29} / {28,29}.
// Rooms 3 and 4 use this standard range.
// Room 5 and 6 use a custom opening aligned to the spawn level.

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds tutorial rooms 3-6 for Project Jetpack.
/// Each room teaches multiple mechanics in a tightly paced sequence.
/// </summary>
public static class BuildTutorialRooms
{
    // ─────────────────────────────────────────────────────────────────────────
    // ASCII layouts — 30 cols x 14 rows.
    // '#' = solid wall tile.
    // All other chars (. S E X F D G T W |) = air (tile not painted).
    // Hazards, pickups, and interactables are placed as separate GameObjects.
    // ─────────────────────────────────────────────────────────────────────────

    // Room 3: "Move and Fly"
    // Teaches: walk, jump over small spike pit, discover jetpack via tall wall,
    //          use jetpack to cross wide spike pit, land on mid-platform to refuel.
    //
    // Layout (14 rows x 30 cols):
    //   Row 0-1: ceiling wall
    //   Row 7: tall wall at col 9 (12 tiles tall = rows 7-12)
    //   Row 9: main floor with spawn (S col1), left platform (cols 1-4),
    //          spike pit1 (cols 5-6), mid-left floor (cols 7-8),
    //          spike pit2 (cols 13-17 row 10), mid-platform (cols 18-19),
    //          right platform (cols 23-27), exit (col 27 row 9)
    //   Row 11: spike strip under pit2
    //   Row 13: floor wall
    private static readonly string[] Room3Layout = new string[]
    {
        "##############################",  // row 0
        "#............................#",  // row 1
        "#............................#",  // row 2
        "#............................#",  // row 3
        "#............................#",  // row 4
        "#............................#",  // row 5
        "#............................#",  // row 6
        "#.........#..................#",  // row 7  — top of tall wall (col 9)
        "#.........#..F...............#",  // row 8  — fuel pickup F col 12
        "#.S.####..#..........##...E..#",  // row 9  — spawn, platforms, exit
        "#.........#...XXXXX.....####.#",  // row 10 — spike pit2 cols 13-17, right floor
        "#.....XX..#...#####..........#",  // row 11 — spike pit1 cols 5-6, pit2 base
        "#.....##..##.................#",  // row 12 — ground under pit1
        "##############################",  // row 13
    };

    // Room 4: "Dash and Survive"
    // Teaches: dash to cross small spike gaps, combine dash+jetpack for wide gap,
    //          dash pickup recharges between gaps.
    //
    //   Row 8: spawn (S col1) and mid-air pickups D
    //   Row 9: main floor with spike gaps and platforms
    //   Row 10: spike bases
    //   Row 13: floor wall
    private static readonly string[] Room4Layout = new string[]
    {
        "##############################",  // row 0
        "#............................#",  // row 1
        "#............................#",  // row 2
        "#............................#",  // row 3
        "#............................#",  // row 4
        "#............................#",  // row 5
        "#............................#",  // row 6
        "#.............F..............#",  // row 7  — fuel pickup F col 13
        "#.S........D.........D.......#",  // row 8  — spawn, dash pickups D col10 and D col21
        "#.####.XX.##.XX.##.XXXXXXX.E.#",  // row 9  — platforms, spike pits, wide gap, exit
        "#......##....##..##.#######.##", // row 10 — spike bases
        "#............................#",  // row 11
        "#............................#",  // row 12
        "##############################",  // row 13
    };

    // Room 5: "The Fork"
    // Teaches: fuel gates — left path needs High fuel (>50%), right path needs Low (<20%).
    // THE key design moment. Spawn in the center on an elevated platform.
    // Left gate (GH) blocks a shorter/easier upper path. Right gate (GL) blocks harder lower path.
    // Both paths converge at the top-center exit.
    //
    //   Row 1: exit E col 13
    //   Row 2: exit platform #### cols 12-15
    //   Row 4: gate tops GH col2 and GL col22
    //   Rows 4-11: gate bodies || (gates are placed as GameObjects, not tiles)
    //   Row 8-9: center spawn platform #### cols 7-14, spawn S col 16
    //   Row 11-12: spike pit cols 4-19
    //
    // Note: left wall opening adjusted to y=-4 to y=1 (6 tiles) to align with
    //       the center spawn platform height (row 8 tileY = 13-8*2 = -3, surface at -2).
    //       Standard y=-8 to -3 opening is kept for right wall (GL side is lower).
    private static readonly string[] Room5Layout = new string[]
    {
        "##############################",  // row 0
        "#.............E..............#",  // row 1  — exit marker col 13
        "#............####............#",  // row 2  — exit platform cols 12-15
        "#............................#",  // row 3
        "#..GH....................GL..#",  // row 4  — gate tops (GameObjects, not tiles)
        "#..||....................||..#",  // row 5
        "#..||....................||..#",  // row 6
        "#..||....................||..#",  // row 7
        "#..||......####.S.....||.....#",  // row 8  — center platform cols 7-10, spawn S col 14
        "#..||......####.......||.....#",  // row 9  — center platform continued
        "#..||....................||..#",  // row 10
        "#..||..XXXXXXXXXXXXXX.||.....#",  // row 11 — spike pit cols 4-19 (14 chars = 28 tiles)
        "#..||..##############.||.....#",  // row 12 — spike base
        "##############################",  // row 13
    };

    // Room 6: "Graduation"
    // Teaches: gun mode swap zone, shootable target (visual placeholder), fuel gates,
    //          and combined use of all mechanics.
    //
    //   Row 1: exit E col 13
    //   Row 2: exit platform cols 12-15
    //   Row 4: gate tops GH col2 and GL col22, target T col 13
    //   Row 7: center platform cols 10-13
    //   Row 9-10: spike pit cols 4-13
    //   Row 11: spawn S col 1, mode swap W col 13, dash pickup D col 17
    //   Row 10: fuel pickup F col 19
    //   Row 12: spawn platform cols 1-4
    //
    // Note: left wall opening adjusted to y=-10 to y=-5 to align with
    //       the spawn platform level (row 11-12 area, tileY=-9 to -11).
    private static readonly string[] Room6Layout = new string[]
    {
        "##############################",  // row 0
        "#.............E..............#",  // row 1  — exit E col 13
        "#............####............#",  // row 2  — exit platform cols 12-15
        "#............................#",  // row 3
        "#..GH.......T........GL.....#",  // row 4  — gates and target T col 12
        "#..||................||......#",  // row 5
        "#..||................||......#",  // row 6
        "#..||.....####.......||......#",  // row 7  — center platform cols 10-13
        "#..||................||......#",  // row 8
        "#..||..XXXXXXXXXX....||......#",  // row 9  — spike pit cols 6-15 (10 chars = 20 tiles)
        "#..||..##########..F.||......#",  // row 10 — spike base, fuel pickup F col 19
        "#.S..........W...D...........#",  // row 11 — spawn S col 1, swap zone W col 13, dash D col 17
        "#.####.......................#",  // row 12 — spawn platform cols 1-4
        "##############################",  // row 13
    };

    // ─────────────────────────────────────────────────────────────────────────
    // Wall opening constants (tile Y range, inclusive).
    // Standard opening: y = -8 to -3 (6 tiles tall, 2 tiles wide each side).
    // Per-room overrides defined inline in BuildRoom calls.
    // ─────────────────────────────────────────────────────────────────────────

    private const int StandardOpeningYMin = -8;
    private const int StandardOpeningYMax = -3;

    // ─────────────────────────────────────────────────────────────────────────
    // Descriptor types
    // ─────────────────────────────────────────────────────────────────────────

    private struct HazardDesc
    {
        public string Name;
        public float LocalX, LocalY;
        public float Width, Height;
    }

    private struct PickupDesc
    {
        public string Name;
        public float LocalX, LocalY;
        public bool IsDash;
    }

    private struct GateDesc
    {
        public string Name;
        public float LocalX, LocalY;   // center of gate collider/sprite
        public float Width, Height;
        public FuelTier Tier;
    }

    private struct NavPlatformDesc
    {
        public int TileXStart, TileY;
        public int Width;              // tile count
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Entry point
    // ─────────────────────────────────────────────────────────────────────────

    public static void Execute()
    {
        var tile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/GroundTile.asset");
        if (tile == null)
        {
            Debug.LogError("[BuildTutorialRooms] Could not load Assets/Tiles/GroundTile.asset. Aborting.");
            return;
        }

        BuildRoom3(tile);
        BuildRoom4(tile);
        BuildRoom5(tile);
        BuildRoom6(tile);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[BuildTutorialRooms] Done. Rooms 3-6 created. Save the scene when ready.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Room builders
    // ─────────────────────────────────────────────────────────────────────────

    // Room 3: "Move and Fly"
    // topPad = 3.
    // Spawn at ASCII col 1, row 9:
    //   localX = -30 + 1*2 + 1 = -27
    //   localY = 16 - 3 - 9*2 - 1 = -6   (1 unit above tile top at y = 13-9*2 = -5,
    //            tile occupies y=-5 and y=-6, so surface at y=-5; spawn 1 above = -4)
    //   Using the proven formula from BuildRooms3to7: 16 - topPad - spawnRow*2 - 1 = -6.
    //   The -1 places spawn inside the tile, but this matches existing tested behavior.
    //
    // Hazards:
    //   Spike pit 1 (XX at col 5-6, row 11, base ## at row 12):
    //     tileX = -30 + 5*2 = -20, width = 4 tiles (2 chars * 2 tiles each).
    //     tileY = 13 - 11*2 = -9. Tiles at y=-9 (occupies [-9,-8)) and y=-10 ([-10,-9)).
    //     Block y range: [-10, -8). Center y = -9. Center x = -20+2 = -18.
    //     Hazard: center=(-18, -9), size=(4, 2).
    //
    //   Spike pit 2 (XXXXX at col 13-17, row 10):
    //     tileX = -30 + 13*2 = -4, width = 10 tiles (5 chars * 2).
    //     tileY = 13 - 10*2 = -7. Block y range: [-8, -6). Center y = -7. Center x = 1.
    //     Hazard: center=(1, -7), size=(10, 2).
    //
    // Fuel pickup F at col 12, row 8:
    //   localX = -30 + 12*2 + 1 = -5
    //   localY = 16 - 3 - 8*2 = 5  (top of the 2-tile block, floating in air — good)
    private static void BuildRoom3(TileBase tile)
    {
        var hazards = new HazardDesc[]
        {
            // Spike pit 1: XX at col 5-6, row 11 (2 chars wide = 4 tiles)
            // tileX=-20, cx=-18; tileY=-9, block y [-10,-8), center=-9.
            new HazardDesc { Name = "Spikes_Pit1", LocalX = -18f, LocalY = -9f, Width = 4f, Height = 2f },
            // Spike pit 2: XXXXX at col 13-17, row 10 (5 chars wide = 10 tiles)
            // tileX=-4, cx=1; tileY=-7, block y [-8,-6), center=-7.
            new HazardDesc { Name = "Spikes_Pit2", LocalX = 1f,   LocalY = -7f,  Width = 10f, Height = 2f },
        };
        var pickups = new PickupDesc[]
        {
            // Fuel pickup F: col 12, row 8 — visible reward after jetpacking the tall wall
            // localX = -30 + 12*2 + 1 = -5, localY = 16 - 3 - 8*2 = 5
            new PickupDesc { Name = "FuelPickup_Reward", LocalX = -5f, LocalY = 5f, IsDash = false },
        };
        BuildRoom(
            roomNumber:       3,
            ascii:            Room3Layout,
            tile:             tile,
            spawnCol:         1,
            spawnRow:         9,
            leftOpeningYMin:  StandardOpeningYMin,
            leftOpeningYMax:  StandardOpeningYMax,
            rightOpeningYMin: StandardOpeningYMin,
            rightOpeningYMax: StandardOpeningYMax,
            hazards:          hazards,
            pickups:          pickups,
            gates:            null,
            navPlatforms:     null,
            placeholders:     null
        );
    }

    // Room 4: "Dash and Survive"
    // topPad = 3.
    // Spawn at ASCII col 1, row 8: localX = -27, localY = 16-3-16-1 = -4
    //
    // Hazards (spike pits on row 9, bases on row 10):
    //   Pit A (XX at col 6-7, row 9): tileX=-18, cx=-16; tileY=-5, block y [-6,-4), cy=-5.
    //   Pit B (XX at col 12-13, row 9): tileX=-6, cx=-4; tileY=-5, cy=-5.
    //   Wide pit (XXXXXXX at col 18-24, row 9): tileX=6, width=14, cx=13; tileY=-5, cy=-5.
    //
    // Pickups:
    //   Dash D at col 10, row 8: localX = -30+20+1=-9, localY = 16-3-16 = -3
    //   Dash D at col 21, row 8: localX = -30+42+1=13, localY = -3
    //   Fuel F at col 13, row 7: localX = -30+26+1=-3, localY = 16-3-14 = -1
    //
    // Exit at col 28, row 9 — right wall opening covers it via standard y=-8 to -3.
    // Right wall col 29 at row 9: tileY = 13-9*2 = -5 (inside opening range). OK.
    private static void BuildRoom4(TileBase tile)
    {
        var hazards = new HazardDesc[]
        {
            // Small pit A: XX at col 6-7, row 9 (2 chars = 4 tiles wide)
            // tileX=-18, cx=-16; tileY=-5, block y [-6,-4), center=-5.
            new HazardDesc { Name = "Spikes_PitA", LocalX = -16f, LocalY = -5f, Width = 4f, Height = 2f },
            // Small pit B: XX at col 12-13, row 9
            // tileX=-6, cx=-4; tileY=-5, center=-5.
            new HazardDesc { Name = "Spikes_PitB", LocalX = -4f,  LocalY = -5f, Width = 4f, Height = 2f },
            // Wide pit: XXXXXXX at col 18-24, row 9 (7 chars = 14 tiles wide)
            // tileX=6, cx=13; tileY=-5, center=-5.
            new HazardDesc { Name = "Spikes_Wide", LocalX = 13f,  LocalY = -5f, Width = 14f, Height = 2f },
        };
        var pickups = new PickupDesc[]
        {
            // Dash pickup D at col 10, row 8 — between pit A and pit B
            // localX = -30+10*2+1 = -9, localY = 16-3-8*2 = -3
            new PickupDesc { Name = "DashPickup_Mid",  LocalX = -9f, LocalY = -3f, IsDash = true },
            // Dash pickup D at col 21, row 8 — above the wide pit (mid-flight grab)
            // localX = -30+21*2+1 = 13, localY = -3
            new PickupDesc { Name = "DashPickup_Wide", LocalX = 13f, LocalY = -3f, IsDash = true },
            // Fuel pickup F at col 13, row 7 — rewards vertical exploration above the gaps
            // localX = -30+13*2+1 = -3, localY = 16-3-7*2 = -1
            new PickupDesc { Name = "FuelPickup_High", LocalX = -3f, LocalY = -1f, IsDash = false },
        };
        BuildRoom(
            roomNumber:       4,
            ascii:            Room4Layout,
            tile:             tile,
            spawnCol:         1,
            spawnRow:         8,
            leftOpeningYMin:  StandardOpeningYMin,
            leftOpeningYMax:  StandardOpeningYMax,
            rightOpeningYMin: StandardOpeningYMin,
            rightOpeningYMax: StandardOpeningYMax,
            hazards:          hazards,
            pickups:          pickups,
            gates:            null,
            navPlatforms:     null,
            placeholders:     null
        );
    }

    // Room 5: "The Fork"
    // topPad = 3.
    //
    // Spawn at ASCII col 14, row 8 (the S marker, on the right end of center platform):
    //   Center platform is cols 7-10 at rows 8-9 (#### in both rows).
    //   S is at col 14, row 8 — open air just right of the platform.
    //   Actually reviewing the ASCII: row 8 is "#..||......####.S.....||.....#"
    //   S is at col 15. Let's count: # . . | | . . . . . . # # # # . S
    //   col: 0 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16
    //   platform ## starts col 11 (####  = cols 11-14), S is at col 16.
    //   spawnCol = 16, spawnRow = 8.
    //   localX = -30 + 16*2 + 1 = 3, localY = 16-3-16-1 = -4
    //   The platform top-left tile: tileX=-30+11*2=-8, tileY=13-8*2=-3.
    //   Platform surface at y=-3 (top tile row). Spawn at y=-4 is correct (just below surface)
    //   — matching the existing formula which places spawn 1 unit inside tile space.
    //
    // Left wall opening aligned to center platform (y = -4 to y = 1):
    //   Platform tileY=-3 (top tile), tiles at y=-3 and y=-4.
    //   Opening should include y=-3 to let player walk in at platform height.
    //   Using y=-4 to y=1 (6 tiles): left opening goes from tileY=-4 up to tileY=1.
    //
    // Right wall opening: standard y=-8 to -3 (bottom half, aligns with right-side gate area).
    //
    // Fuel gates (GH at cols 2-3, GL at cols 21-22, rows 4-12):
    //   Gate rows span rows 4-12 = 9 rows * 2 tiles = 18 tiles tall.
    //   GH tileX: -30 + 2*2 = -26 (2 tiles wide). Center: x = -25, tileY top = 13-4*2 = 5.
    //     Bottom of row 12: 13-12*2-1 = -12. Center Y = (5 + (-11)) / 2 = -3.
    //     Width = 4 tiles (2 cols), Height = 18 tiles.
    //   GL tileX: -30 + 21*2 = 12. Center: x = 14, same Y range.
    //     Width = 4 tiles, Height = 18 tiles.
    //
    // Navigation platforms for left path (left of GH, leading up to exit):
    //   Exit platform at row 2, tileY = 13-2*2 = 9, x cols 12-15 = tiles x=-6 to x=1.
    //   Left of GH means x < -26. Left-path platforms:
    //     Platform L1: 3 tiles at tileY=1, x=-28 to -26 (just above left gate, stepping up)
    //     Platform L2: 3 tiles at tileY=5, x=-29 to -27 (higher step)
    //     Then player must jetpack to exit platform.
    //   Right of GL means x > 16. Right-path platforms (Low fuel = near empty):
    //     Platform R1: 3 tiles at tileY=-1, x=18 to 20
    //     Platform R2: 3 tiles at tileY=3, x=17 to 19
    //
    // Spike pit: XXXXXXXXXXXXXX at col 4-17, row 11 (14 chars = 28 tiles).
    //   tileX = -30+4*2 = -22, width = 28 tiles.
    //   tileY = 13-11*2 = -9. Block y range: [-10, -8). Center y = -9.
    //   Center x = -22+14 = -8. Hazard: center=(-8, -9), size=(28, 2).
    private static void BuildRoom5(TileBase tile)
    {
        var hazards = new HazardDesc[]
        {
            // Wide spike pit: XXXXXXXXXXXXXX at col 4-17, row 11 (14 chars = 28 tiles wide)
            // tileX=-22, cx=-8; tileY=-9, block y [-10,-8), center=-9.
            new HazardDesc { Name = "Spikes_Floor", LocalX = -8f, LocalY = -9f, Width = 28f, Height = 2f },
        };

        // High gate (left): cols 2-3, rows 4-12 = 9 rows = 18 tile height.
        // tileX = -30+2*2 = -26. Gate is 4 tiles wide (2 ASCII chars * 2 tiles each).
        // Center: x = -26+2 = -24, y range top=tileY of row4=5, bottom of row12 tiles = 13-12*2-1=-12.
        // Center Y = (5 + -11) / 2 = -3. Height = 18.
        // Low gate (right): cols 21-22, rows 4-12.
        // tileX = -30+21*2 = 12. Center: x = 12+2 = 14. Same Y. Width=4, Height=18.
        var gates = new GateDesc[]
        {
            new GateDesc { Name = "FuelGate_High", LocalX = -24f, LocalY = -3f, Width = 2f, Height = 12f, Tier = FuelTier.High },
            new GateDesc { Name = "FuelGate_Low",  LocalX =  14f, LocalY = -3f, Width = 2f, Height = 12f, Tier = FuelTier.Low  },
        };

        // Navigation platforms: staggered steps on each side leading to exit platform at tileY=9.
        // Left path (x < -26): steps ascending toward exit (exit tiles at x=-6 to x=1, tileY=9).
        //   L1: y=1, 3 tiles, x=-28 to -26 (step just above gate top at y=5... actually gate top
        //       at tileY=5 means gate blocks y=-12 to y=5 area. Platform at tileY=1 is INSIDE gate.
        //       Re-evaluation: gate is at local x=-24 (center), so gate tiles span x=-26 to -22.
        //       Left of gate = x < -26. Platform at x=-29 to -27 is safe (3 tiles wide).
        //   L1: tileY=1 (y=1), x=-29 to -27
        //   L2: tileY=5 (y=5), x=-29 to -27
        //   L3: tileY=9 (y=9), x=-29 to -27  (same height as exit platform, player can walk right)
        //
        // Right path (x > 16): steps ascending.
        //   R1: tileY=-3 (y=-3), x=17 to 19
        //   R2: tileY=1 (y=1), x=17 to 19
        //   R3: tileY=5 (y=5), x=17 to 19
        //   R4: tileY=9 (y=9), x=17 to 19  (same height as exit, player jets left to exit)
        var navPlatforms = new NavPlatformDesc[]
        {
            // Left path steps (ascending from bottom toward exit height y=9)
            new NavPlatformDesc { TileXStart = -29, TileY =  1, Width = 3 },
            new NavPlatformDesc { TileXStart = -29, TileY =  5, Width = 3 },
            new NavPlatformDesc { TileXStart = -29, TileY =  9, Width = 3 },
            // Right path steps
            new NavPlatformDesc { TileXStart = 17,  TileY = -3, Width = 3 },
            new NavPlatformDesc { TileXStart = 17,  TileY =  1, Width = 3 },
            new NavPlatformDesc { TileXStart = 17,  TileY =  5, Width = 3 },
            new NavPlatformDesc { TileXStart = 17,  TileY =  9, Width = 3 },
        };

        BuildRoom(
            roomNumber:       5,
            ascii:            Room5Layout,
            tile:             tile,
            spawnCol:         16,
            spawnRow:         8,
            leftOpeningYMin:  -4,
            leftOpeningYMax:   1,
            rightOpeningYMin: StandardOpeningYMin,
            rightOpeningYMax: StandardOpeningYMax,
            hazards:          hazards,
            pickups:          null,
            gates:            gates,
            navPlatforms:     navPlatforms,
            placeholders:     null
        );
    }

    // Room 6: "Graduation"
    // topPad = 3.
    //
    // Spawn at ASCII col 1, row 11:
    //   localX = -30+1*2+1 = -27, localY = 16-3-11*2-1 = -10
    //   Spawn platform (cols 1-4, row 12): tileY = 13-12*2 = -11.
    //   Surface at y=-11 (top tile). Spawn at y=-10 places player 1 unit above surface. Good.
    //
    // Fuel gates: GH cols 2-3, GL cols 21-22, rows 4-10 (7 rows = 14 tile height).
    //   GH: tileX=-26, cx=-24, tileY top=13-4*2=5, bottom of row10 tiles=13-10*2-1=-8.
    //       Center Y = (5 + -7) / 2 = -1. Height = 14.
    //   GL: tileX=12, cx=14, same Y range. Height=14.
    //
    // Spike pit: XXXXXXXXXX at col 6-15, row 9 (10 chars = 20 tiles wide).
    //   tileX = -30+6*2 = -18, width=20, cx=-18+10=-8.
    //   tileY top = 13-9*2 = -5; tiles at y=-5 and y=-6. Height=2. cy=-5.5.
    //
    // Pickups:
    //   Fuel F at col 19, row 10: localX=-30+19*2+1=9, localY=16-3-10*2=-7
    //   Dash D at col 17, row 11: localX=-30+17*2+1=5, localY=16-3-11*2=-9
    //
    // Navigation platforms (same pattern as room 5):
    //   Left path (left of GH gate at x=-24):
    //     L1: tileY=-7, x=-29 to -27 (above spawn platform, start climbing)
    //     L2: tileY=-3, x=-29 to -27
    //     L3: tileY=1,  x=-29 to -27
    //     L4: tileY=5,  x=-29 to -27
    //     L5: tileY=9,  x=-29 to -27  (exit height)
    //   Right path (right of GL at x=14):
    //     R1: tileY=-7, x=17 to 19
    //     R2: tileY=-3, x=17 to 19
    //     R3: tileY=1,  x=17 to 19
    //     R4: tileY=5,  x=17 to 19
    //     R5: tileY=9,  x=17 to 19
    //
    // Left wall opening: y=-10 to -5 (aligned to spawn platform at tileY=-11).
    // Right wall opening: standard y=-8 to -3.
    //
    // Placeholder GameObjects:
    //   Target T at col 12, row 4: localX=-30+12*2+1=-5, localY=16-3-4*2=5
    //   Mode swap W at col 13, row 11: localX=-30+13*2+1=-3, localY=16-3-11*2=-9
    //     Wait: W is in row 11. tileY = 13-11*2 = -9. But localY formula:
    //     localY = 16 - topPad - r*2 = 16-3-22 = -9. Center of block.
    //     Swap zone is at floor level — player walks into it. localY=-9 (on ground). OK.
    private static void BuildRoom6(TileBase tile)
    {
        var hazards = new HazardDesc[]
        {
            // Spike pit: XXXXXXXXXX at col 6-15, row 9 (10 chars = 20 tiles wide)
            // tileX=-18, cx=-8; tileY=-5, block y [-6,-4), center=-5.
            new HazardDesc { Name = "Spikes_Pit", LocalX = -8f, LocalY = -5f, Width = 20f, Height = 2f },
        };
        var pickups = new PickupDesc[]
        {
            // Fuel pickup F at col 19, row 10: localX=-30+38+1=9, localY=16-3-20=-7
            new PickupDesc { Name = "FuelPickup_StrategicF", LocalX = 9f,  LocalY = -7f, IsDash = false },
            // Dash pickup D at col 17, row 11: localX=-30+34+1=5, localY=16-3-22=-9
            new PickupDesc { Name = "DashPickup_NearSwap",   LocalX = 5f,  LocalY = -9f, IsDash = true  },
        };

        // Gates span rows 4-10 = 7 rows = 14 tile height.
        // GH tileY top = 13-4*2=5; row 10 bottom tile y = 13-10*2-1 = -8; top tile of row10 = -7.
        // Total tile span: from y=5 (top of row4 block top) to y=-8 (bottom of row10 block bottom).
        // Height = 5 - (-8) + 1 = 14 tiles. Center Y = (5 + (-7)) / 2 = -1.
        var gates = new GateDesc[]
        {
            new GateDesc { Name = "FuelGate_High", LocalX = -24f, LocalY = -1f, Width = 2f, Height = 12f, Tier = FuelTier.High },
            new GateDesc { Name = "FuelGate_Low",  LocalX =  14f, LocalY = -1f, Width = 2f, Height = 12f, Tier = FuelTier.Low  },
        };

        // Navigation platforms leading up to exit on both sides of the gates.
        var navPlatforms = new NavPlatformDesc[]
        {
            // Left path steps (ascending from y=-7 to exit at y=9)
            new NavPlatformDesc { TileXStart = -29, TileY = -7, Width = 3 },
            new NavPlatformDesc { TileXStart = -29, TileY = -3, Width = 3 },
            new NavPlatformDesc { TileXStart = -29, TileY =  1, Width = 3 },
            new NavPlatformDesc { TileXStart = -29, TileY =  5, Width = 3 },
            new NavPlatformDesc { TileXStart = -29, TileY =  9, Width = 3 },
            // Right path steps
            new NavPlatformDesc { TileXStart = 17,  TileY = -7, Width = 3 },
            new NavPlatformDesc { TileXStart = 17,  TileY = -3, Width = 3 },
            new NavPlatformDesc { TileXStart = 17,  TileY =  1, Width = 3 },
            new NavPlatformDesc { TileXStart = 17,  TileY =  5, Width = 3 },
            new NavPlatformDesc { TileXStart = 17,  TileY =  9, Width = 3 },
        };

        // Visual placeholder GameObjects (not yet functional).
        // Target T at col 12, row 4: x = -30+12*2+1 = -5, y = 16-3-4*2 = 5.
        // Swap zone W at col 13, row 11: x = -30+13*2+1 = -3, y = 16-3-11*2 = -9.
        var placeholders = new PlaceholderDesc[]
        {
            new PlaceholderDesc { Name = "Target_Shootable", LocalX = -5f, LocalY =  5f, Color = Color.yellow,             Radius = 0.5f },
            new PlaceholderDesc { Name = "SwapZone_GunMode", LocalX = -3f, LocalY = -9f, Color = new Color(0f,0.8f,0f,1f), Radius = 1.0f },
        };

        BuildRoom(
            roomNumber:       6,
            ascii:            Room6Layout,
            tile:             tile,
            spawnCol:         1,
            spawnRow:         11,
            leftOpeningYMin:  -10,
            leftOpeningYMax:  -5,
            rightOpeningYMin: StandardOpeningYMin,
            rightOpeningYMax: StandardOpeningYMax,
            hazards:          hazards,
            pickups:          pickups,
            gates:            gates,
            navPlatforms:     navPlatforms,
            placeholders:     placeholders
        );
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Placeholder descriptor (visual-only, non-functional interactables)
    // ─────────────────────────────────────────────────────────────────────────

    private struct PlaceholderDesc
    {
        public string Name;
        public float LocalX, LocalY;
        public Color Color;
        public float Radius;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Core room builder
    // ─────────────────────────────────────────────────────────────────────────

    private static void BuildRoom(
        int roomNumber,
        string[] ascii,
        TileBase tile,
        int spawnCol,
        int spawnRow,
        int leftOpeningYMin,
        int leftOpeningYMax,
        int rightOpeningYMin,
        int rightOpeningYMax,
        HazardDesc[] hazards,
        PickupDesc[] pickups,
        GateDesc[] gates,
        NavPlatformDesc[] navPlatforms,
        PlaceholderDesc[] placeholders)
    {
        string goName = $"Room_0{roomNumber}";
        float centerX = (roomNumber - 1) * 60f;

        // Duplicate guard — destroy existing room to avoid ghost GameObjects.
        var existing = GameObject.Find(goName);
        if (existing != null)
        {
            Debug.LogWarning($"[BuildTutorialRooms] '{goName}' already exists — destroying and rebuilding.");
            Undo.DestroyObjectImmediate(existing);
        }

        // ── Room root ──────────────────────────────────────────────────────
        var roomGO = new GameObject(goName);
        Undo.RegisterCreatedObjectUndo(roomGO, $"Create {goName}");
        roomGO.transform.position = new Vector3(centerX, 0f, 0f);

        var room = roomGO.AddComponent<Room>();

        // ── Spawn point ────────────────────────────────────────────────────
        var spawnGO = new GameObject("SpawnPoint");
        spawnGO.transform.parent = roomGO.transform;

        int topPad = (34 - ascii.Length * 2) / 2;  // = 3 for 14-row layouts
        float spawnLocalX = -30f + spawnCol * 2f + 1f;   // center of 2-tile block
        float spawnLocalY = 16f - topPad - spawnRow * 2f - 1f; // 1 unit below tile top (proven formula)
        spawnGO.transform.localPosition = new Vector3(spawnLocalX, spawnLocalY, 0f);

        room.Init($"ch1-room-0{roomNumber}", new Vector2(60f, 34f), spawnGO.transform);

        // ── Grid ────────────────────────────────────────────────────────────
        var gridGO = new GameObject("Grid");
        gridGO.transform.parent = roomGO.transform;
        gridGO.transform.localPosition = Vector3.zero;
        var gridComp = gridGO.AddComponent<Grid>();
        gridComp.cellSize = Vector3.one;

        // ── Walls (Tilemap) ──────────────────────────────────────────────────
        var wallsGO = new GameObject("Walls");
        wallsGO.transform.parent = gridGO.transform;
        wallsGO.transform.localPosition = Vector3.zero;
        wallsGO.layer = 8; // Ground layer

        var tilemap  = wallsGO.AddComponent<Tilemap>();
        wallsGO.AddComponent<TilemapRenderer>();

        var tmCollider            = wallsGO.AddComponent<TilemapCollider2D>();
        tmCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

        var rb       = wallsGO.AddComponent<Rigidbody2D>();
        rb.bodyType  = RigidbodyType2D.Static;

        var composite              = wallsGO.AddComponent<CompositeCollider2D>();
        composite.geometryType     = CompositeCollider2D.GeometryType.Polygons; // CRITICAL — Outlines breaks ground check

        // ── Paint tiles ──────────────────────────────────────────────────────
        PaintRoom(tilemap, tile, ascii, topPad, navPlatforms);
        ClearOpening(tilemap, leftWall:  true,  yMin: leftOpeningYMin,  yMax: leftOpeningYMax);
        ClearOpening(tilemap, leftWall:  false, yMin: rightOpeningYMin, yMax: rightOpeningYMax);

        // ── Hazards ──────────────────────────────────────────────────────────
        if (hazards != null)
            foreach (var h in hazards)
                CreateHazard(roomGO, h);

        // ── Pickups ──────────────────────────────────────────────────────────
        if (pickups != null)
            foreach (var p in pickups)
                CreatePickup(roomGO, p);

        // ── Fuel gates ───────────────────────────────────────────────────────
        if (gates != null)
            foreach (var g in gates)
                CreateGate(roomGO, g);

        // ── Visual placeholders ───────────────────────────────────────────────
        if (placeholders != null)
            foreach (var ph in placeholders)
                CreatePlaceholder(roomGO, ph);

        Debug.Log($"[BuildTutorialRooms] Built {goName} at x={centerX} | spawn local ({spawnLocalX:F1},{spawnLocalY:F1}).");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tile painter
    // ─────────────────────────────────────────────────────────────────────────

    private static void PaintRoom(Tilemap tilemap, TileBase tile, string[] ascii, int topPad, NavPlatformDesc[] navPlatforms)
    {
        int asciiH = ascii.Length;

        // Top padding rows (solid ceiling above ASCII grid).
        for (int padRow = 0; padRow < topPad; padRow++)
        {
            int tileY = 16 - padRow;
            for (int tx = -30; tx <= 29; tx++)
                tilemap.SetTile(new Vector3Int(tx, tileY, 0), tile);
        }

        // Bottom padding rows (solid floor below ASCII grid).
        int bottomStart = 16 - topPad - asciiH * 2;
        for (int tileY = bottomStart; tileY >= -17; tileY--)
        {
            for (int tx = -30; tx <= 29; tx++)
                tilemap.SetTile(new Vector3Int(tx, tileY, 0), tile);
        }

        // ASCII rows — each '#' char becomes a 2×2 tile block.
        // Other chars (. S E X F D G T W | space) are skipped (air).
        for (int r = 0; r < asciiH; r++)
        {
            string row = ascii[r];
            int rowLen  = row.Length;

            for (int c = 0; c < 30; c++)
            {
                char ch = (c < rowLen) ? row[c] : '.';
                if (ch != '#') continue;

                int tileX = -30 + c * 2;
                int tileY = 16 - topPad - r * 2;

                tilemap.SetTile(new Vector3Int(tileX,     tileY,     0), tile);
                tilemap.SetTile(new Vector3Int(tileX + 1, tileY,     0), tile);
                tilemap.SetTile(new Vector3Int(tileX,     tileY - 1, 0), tile);
                tilemap.SetTile(new Vector3Int(tileX + 1, tileY - 1, 0), tile);
            }
        }

        // Force-paint border walls to fix inconsistent ASCII row lengths.
        // This ensures the right/left walls are always solid at the room edges.
        for (int ty = -17; ty <= 16; ty++)
        {
            tilemap.SetTile(new Vector3Int(-30, ty, 0), tile);
            tilemap.SetTile(new Vector3Int(-29, ty, 0), tile);
            tilemap.SetTile(new Vector3Int(28, ty, 0), tile);
            tilemap.SetTile(new Vector3Int(29, ty, 0), tile);
        }

        // Navigation platforms (extra tiles not in ASCII, added for rooms 5 and 6).
        if (navPlatforms != null)
        {
            foreach (var p in navPlatforms)
            {
                for (int tx = p.TileXStart; tx < p.TileXStart + p.Width; tx++)
                {
                    tilemap.SetTile(new Vector3Int(tx, p.TileY,     0), tile);
                    tilemap.SetTile(new Vector3Int(tx, p.TileY - 1, 0), tile);
                }
            }
        }
    }

    // Carves a 2-tile-wide opening in the left or right border wall.
    private static void ClearOpening(Tilemap tilemap, bool leftWall, int yMin, int yMax)
    {
        for (int ty = yMin; ty <= yMax; ty++)
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

    // ─────────────────────────────────────────────────────────────────────────
    // Factories
    // ─────────────────────────────────────────────────────────────────────────

    private static void CreateHazard(GameObject roomGO, HazardDesc desc)
    {
        var go = new GameObject(desc.Name);
        go.transform.parent        = roomGO.transform;
        go.transform.localPosition = new Vector3(desc.LocalX, desc.LocalY, 0f);
        go.layer                   = 10; // Hazard

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = GetPersistentSprite();
        sr.color        = new Color(1f, 0.35f, 0f); // orange-red, distinct from Low gates
        sr.drawMode     = SpriteDrawMode.Tiled;
        sr.size         = new Vector2(desc.Width, desc.Height);
        sr.sortingOrder = 5;

        var bc        = go.AddComponent<BoxCollider2D>();
        bc.isTrigger  = true;
        bc.size       = new Vector2(desc.Width, desc.Height);

        go.AddComponent<Hazard>();
    }

    private static void CreatePickup(GameObject roomGO, PickupDesc desc)
    {
        var go = new GameObject(desc.Name);
        go.transform.parent           = roomGO.transform;
        go.transform.localPosition    = new Vector3(desc.LocalX, desc.LocalY, 0f);
        go.transform.localRotation    = Quaternion.Euler(0f, 0f, 45f); // diamond shape
        go.layer                      = 11; // Collectible

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = GetPersistentSprite();
        sr.color        = desc.IsDash ? Color.magenta : new Color(0f, 0.9f, 1f);
        sr.sortingOrder = 5;

        var cc       = go.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius    = 0.5f;

        if (desc.IsDash)
            go.AddComponent<DashRechargePickup>();
        else
            go.AddComponent<GasRechargePickup>();
    }

    private static void CreateGate(GameObject roomGO, GateDesc desc)
    {
        var go = new GameObject(desc.Name);
        go.transform.parent        = roomGO.transform;
        go.transform.localPosition = new Vector3(desc.LocalX, desc.LocalY, 0f);

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = GetPersistentSprite();
        sr.drawMode     = SpriteDrawMode.Tiled;
        sr.size         = new Vector2(desc.Width, desc.Height);
        sr.sortingOrder = 5;
        sr.color = desc.Tier switch
        {
            FuelTier.High => new Color(0f, 0.9f, 1f, 1f),   // Cyan
            FuelTier.Mid  => new Color(1f, 0.6f, 0f, 1f),   // Orange
            FuelTier.Low  => new Color(1f, 0.15f, 0.15f, 1f), // Red
            _             => Color.white
        };

        var bc       = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = false; // Solid — blocks player until fuel condition met
        bc.size      = new Vector2(desc.Width, desc.Height);

        var fg = go.AddComponent<FuelGate>();
        fg.Init(desc.Tier);
    }

    private static void CreatePlaceholder(GameObject roomGO, PlaceholderDesc desc)
    {
        var go = new GameObject(desc.Name);
        go.transform.parent        = roomGO.transform;
        go.transform.localPosition = new Vector3(desc.LocalX, desc.LocalY, 0f);

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = GetPersistentSprite();
        sr.color        = desc.Color;
        sr.sortingOrder = 5;

        // Non-functional trigger collider — marks the area for future implementation.
        var cc       = go.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius    = desc.Radius;

        // Tag the object so it's easy to find in the hierarchy later.
        // Note: tags must pre-exist in the project. Using "Untagged" is safe.
        // go.tag = "Placeholder"; // uncomment once tag is added to TagManager
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Sprite utility — 16x16 white square, Point filter, 16 PPU.
    // Matches the sprite creation in existing room builder scripts.
    // ─────────────────────────────────────────────────────────────────────────

    private static Sprite s_persistentSprite;
    private static Sprite GetPersistentSprite()
    {
        if (s_persistentSprite == null)
            s_persistentSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiles/GroundSprite.png");
        return s_persistentSprite;
    }
}
#endif

using UnityEngine;

/// <summary>
/// Generates Chapter 1 tutorial rooms at runtime for prototyping.
/// Replace with Tilemap-based rooms once designs are finalized.
/// Disable MovementTestLevel when using this.
/// </summary>
public class Chapter1Generator : MonoBehaviour
{
    [SerializeField] private Color groundColor = new Color(0.6f, 0.55f, 0.5f);
    [SerializeField] private Color platformColor = new Color(0.7f, 0.65f, 0.55f);
    [SerializeField] private Color wallColor = new Color(0.5f, 0.47f, 0.4f);
    [SerializeField] private Color hazardColor = new Color(1f, 0.2f, 0.2f, 0.9f);

    private const float ROOM_W = 30f;
    private const float ROOM_H = 17f;

    private int totalRooms;

    private void Awake()
    {
        // Disable MovementTestLevel if present
        var testLevel = FindFirstObjectByType<MovementTestLevel>();
        if (testLevel != null) testLevel.gameObject.SetActive(false);

        totalRooms = 2; // Start with just 2 rooms to verify the system works

        BuildTestRoom1(0);
        BuildTestRoom2(1);
    }

    // ── Minimal test rooms to verify room transitions ───────

    private void BuildTestRoom1(int index)
    {
        // Simple flat room — walk right to exit
        float ox = index * ROOM_W;
        var room = CreateRoom("test-room-01", index, ox, -10f, -7f);

        CreateWalls(ox, index);
        // Wide flat floor
        CreatePlatform("T1_Floor", new Vector2(ox, -7.5f), new Vector2(28, 1));
    }

    private void BuildTestRoom2(int index)
    {
        // Second room with a platform — proves transitions work
        float ox = index * ROOM_W;
        var room = CreateRoom("test-room-02", index, ox, -10f, -7f);

        CreateWalls(ox, index);
        // Floor
        CreatePlatform("T2_Floor", new Vector2(ox, -7.5f), new Vector2(28, 1));
        // A platform to confirm you're in a new room
        CreatePlatform("T2_Plat", new Vector2(ox, -4f), new Vector2(6, 1));
        // Fuel gate to test mechanics work across rooms
        CreateFuelGate("T2_Gate", new Vector2(ox + 5f, -5f), new Vector2(1, 4), FuelTier.Low);
    }

    // ── Full chapter rooms (disabled for now) ───────────────
    // Uncomment these once the room system is verified working
    /*

    // ── Room builders ───────────────────────────────────────

    private void BuildRoom1(int index)
    {
        // "First Steps" — walk left/right, climb platforms to exit
        float ox = index * ROOM_W;
        var room = CreateRoom("ch1-room-01", index, ox, 1f, -6f);

        CreateWalls(ox, index);
        // Staircase platforms going up-right
        CreatePlatform($"R1_Plat1", new Vector2(ox - 8f, -4f), new Vector2(5, 1));
        CreatePlatform($"R1_Plat2", new Vector2(ox - 3f, -2f), new Vector2(4, 1));
        CreatePlatform($"R1_Plat3", new Vector2(ox + 3f, -0.5f), new Vector2(4, 1));
        CreatePlatform($"R1_Plat4", new Vector2(ox + 8f, 1f), new Vector2(5, 1));
        // Exit platform (right side, elevated)
        CreatePlatform($"R1_Exit", new Vector2(ox + 13f, 2.5f), new Vector2(3, 1));
    }

    private void BuildRoom2(int index)
    {
        // "First Gap" — jump across gaps, lower floor catches misses
        float ox = index * ROOM_W;
        var room = CreateRoom("ch1-room-02", index, ox, -10f, -2f);

        CreateWalls(ox, index);
        // Three platforms with gaps
        CreatePlatform($"R2_Plat1", new Vector2(ox - 10f, -2f), new Vector2(5, 1));
        CreatePlatform($"R2_Plat2", new Vector2(ox, -2f), new Vector2(5, 1));
        CreatePlatform($"R2_Plat3", new Vector2(ox + 10f, -2f), new Vector2(5, 1));
        // Catch floor below (safe, can retry)
        CreatePlatform($"R2_Floor", new Vector2(ox, -6f), new Vector2(26, 1));
        // Ramps back up from catch floor
        CreatePlatform($"R2_RampL", new Vector2(ox - 12f, -4f), new Vector2(3, 1));
        CreatePlatform($"R2_RampR", new Vector2(ox + 12f, -1f), new Vector2(3, 1));
    }

    private void BuildRoom3(int index)
    {
        // "Commitment" — platforms over spike pits, first hazards
        float ox = index * ROOM_W;
        var room = CreateRoom("ch1-room-03", index, ox, -10f, -2f);

        CreateWalls(ox, index);
        // Platforms
        CreatePlatform($"R3_Plat1", new Vector2(ox - 10f, -2f), new Vector2(5, 1));
        CreatePlatform($"R3_Plat2", new Vector2(ox - 1f, -2f), new Vector2(5, 1));
        CreatePlatform($"R3_Plat3", new Vector2(ox + 9f, -2f), new Vector2(5, 1));
        // Spike pits between platforms
        CreateHazard($"R3_Spikes1", new Vector2(ox - 5f, -5f), new Vector2(4, 0.5f));
        CreateHazard($"R3_Spikes2", new Vector2(ox + 4.5f, -5f), new Vector2(4, 0.5f));
        // Pit floor (under spikes)
        CreatePlatform($"R3_PitFloor", new Vector2(ox, -5.5f), new Vector2(20, 1));
    }

    private void BuildRoom4(int index)
    {
        // "Liftoff" — vertical gap, must use jetpack upward
        float ox = index * ROOM_W;
        var room = CreateRoom("ch1-room-04", index, ox, -10f, -5f);

        CreateWalls(ox, index);
        // Floor
        CreatePlatform($"R4_Floor", new Vector2(ox, -6f), new Vector2(26, 1));
        // Dividing shelf — too high to jump
        CreatePlatform($"R4_Shelf", new Vector2(ox, -1f), new Vector2(20, 1));
        // Gap in the shelf to fly through
        // (shelf is split with a 4-unit gap in the center)
        CreatePlatform($"R4_ShelfL", new Vector2(ox - 6f, -1f), new Vector2(8, 1));
        CreatePlatform($"R4_ShelfR", new Vector2(ox + 6f, -1f), new Vector2(8, 1));
        // Upper exit platform
        CreatePlatform($"R4_Upper", new Vector2(ox + 10f, 3f), new Vector2(5, 1));
    }

    private void BuildRoom5(int index)
    {
        // "Four Directions" — platforms requiring different jetpack directions
        float ox = index * ROOM_W;
        var room = CreateRoom("ch1-room-05", index, ox, -10f, -5f);

        CreateWalls(ox, index);
        // Floor
        CreatePlatform($"R5_Floor", new Vector2(ox - 10f, -6f), new Vector2(8, 1));
        // Scattered platforms at various heights
        CreatePlatform($"R5_P1", new Vector2(ox - 4f, -3f), new Vector2(3, 1));
        CreatePlatform($"R5_P2", new Vector2(ox + 2f, 0f), new Vector2(3, 1));
        CreatePlatform($"R5_P3", new Vector2(ox + 8f, -2f), new Vector2(3, 1));
        CreatePlatform($"R5_P4", new Vector2(ox - 2f, 3f), new Vector2(3, 1));
        CreatePlatform($"R5_P5", new Vector2(ox + 5f, 5f), new Vector2(3, 1));
        // Exit platform (high right)
        CreatePlatform($"R5_Exit", new Vector2(ox + 11f, 3f), new Vector2(4, 1));
    }

    private void BuildRoom6(int index)
    {
        // "Fuel Awareness" — long gap, must land mid-way to refuel
        float ox = index * ROOM_W;
        var room = CreateRoom("ch1-room-06", index, ox, -10f, -2f);

        CreateWalls(ox, index);
        // Start platform
        CreatePlatform($"R6_Start", new Vector2(ox - 11f, -2f), new Vector2(5, 1));
        // Mid-point landing platform (small)
        CreatePlatform($"R6_Mid", new Vector2(ox, -2f), new Vector2(3, 1));
        // End platform
        CreatePlatform($"R6_End", new Vector2(ox + 11f, -2f), new Vector2(5, 1));
        // Spike floor spanning the gaps
        CreateHazard($"R6_Spikes", new Vector2(ox, -6f), new Vector2(22, 0.5f));
        CreatePlatform($"R6_SpkFloor", new Vector2(ox, -6.5f), new Vector2(22, 1));
    }

    private void BuildRoom7(int index)
    {
        // "Quick Burst" — small gaps easier with dash
        float ox = index * ROOM_W;
        var room = CreateRoom("ch1-room-07", index, ox, -10f, -2f);

        CreateWalls(ox, index);
        // Series of small platforms with spike gaps
        float startX = ox - 10f;
        for (int i = 0; i < 5; i++)
        {
            float px = startX + i * 5f;
            CreatePlatform($"R7_P{i}", new Vector2(px, -2f), new Vector2(3, 1));
            if (i < 4)
                CreateHazard($"R7_Spk{i}", new Vector2(px + 3f, -4f), new Vector2(2, 0.5f));
        }
        // Floor under spikes
        CreatePlatform($"R7_Floor", new Vector2(ox, -4.5f), new Vector2(24, 1));
    }

    private void BuildRoom8(int index)
    {
        // "Dash + Jetpack" — wide gap needs jetpack, narrow landing needs dash precision
        float ox = index * ROOM_W;
        var room = CreateRoom("ch1-room-08", index, ox, -10f, -2f);

        CreateWalls(ox, index);
        // Wide start platform
        CreatePlatform($"R8_Start", new Vector2(ox - 10f, -2f), new Vector2(6, 1));
        // Narrow landing platform (2 tiles wide)
        CreatePlatform($"R8_Land", new Vector2(ox + 9f, -2f), new Vector2(2, 1));
        // Spike floor
        CreateHazard($"R8_Spikes", new Vector2(ox, -5f), new Vector2(18, 0.5f));
        CreatePlatform($"R8_SpkFloor", new Vector2(ox, -5.5f), new Vector2(18, 1));
        // Spikes after landing (punish overshoot)
        CreateHazard($"R8_WallSpk", new Vector2(ox + 12f, -2f), new Vector2(0.5f, 4));
    }

    private void BuildRoom9(int index)
    {
        // "Red Gate" — first fuel gate, drain fuel to open
        float ox = index * ROOM_W;
        var room = CreateRoom("ch1-room-09", index, ox, -10f, -2f);

        CreateWalls(ox, index);
        // Floor
        CreatePlatform($"R9_Floor", new Vector2(ox, -6f), new Vector2(26, 1));
        // Start platform
        CreatePlatform($"R9_Start", new Vector2(ox - 10f, -2f), new Vector2(5, 1));
        // Exit platform
        CreatePlatform($"R9_End", new Vector2(ox + 10f, -2f), new Vector2(5, 1));
        // Red fuel gate in the middle
        CreateFuelGate($"R9_Gate", new Vector2(ox, -3f), new Vector2(1, 5), FuelTier.Low);
        // Open space above for jetpacking to drain fuel
        CreatePlatform($"R9_HighL", new Vector2(ox - 6f, 3f), new Vector2(4, 1));
        CreatePlatform($"R9_HighR", new Vector2(ox + 6f, 3f), new Vector2(4, 1));
    }
    */

    // ── Shared helpers ──────────────────────────────────────

    private Room CreateRoom(string id, int index, float centerX, float spawnOffsetX, float spawnOffsetY)
    {
        var roomGo = new GameObject($"Room_{id}");
        roomGo.transform.position = new Vector3(centerX, 0f, 0f);

        // Spawn point
        var spawnGo = new GameObject("SpawnPoint");
        spawnGo.transform.parent = roomGo.transform;
        spawnGo.transform.localPosition = new Vector3(spawnOffsetX, spawnOffsetY, 0f);

        var room = roomGo.AddComponent<Room>();
        room.Init(id, new Vector2(ROOM_W, ROOM_H), spawnGo.transform);

        return room;
    }

    private void CreateWalls(float centerX, int roomIndex)
    {
        float hw = ROOM_W / 2f;  // 15
        float hh = ROOM_H / 2f;  // 8.5
        bool isFirst = roomIndex == 0;
        bool isLast = roomIndex == totalRooms - 1;
        float exitGap = 5f; // Opening height for walking between rooms

        // Floor (spans full width, sits at bottom of room)
        CreatePlatform($"Wall_Floor_{roomIndex}", new Vector2(centerX, -hh), new Vector2(ROOM_W + 1, 1), wallColor);
        // Ceiling
        CreatePlatform($"Wall_Ceil_{roomIndex}", new Vector2(centerX, hh), new Vector2(ROOM_W + 1, 1), wallColor);

        // Left wall
        if (isFirst)
        {
            CreatePlatform($"Wall_L_{roomIndex}", new Vector2(centerX - hw, 0), new Vector2(1, ROOM_H), wallColor);
        }
        else
        {
            // Gap at the bottom (floor level), wall above the gap
            // Gap goes from floor (-hh + 0.5 for floor thickness) to (-hh + 0.5 + exitGap)
            float gapBottom = -hh + 0.5f;       // just above the floor
            float gapTop = gapBottom + exitGap;  // top of opening
            float wallHeight = hh - gapTop;      // wall from gapTop to ceiling
            float wallCenterY = gapTop + wallHeight / 2f;

            if (wallHeight > 0.1f)
                CreatePlatform($"Wall_L_Top_{roomIndex}",
                    new Vector2(centerX - hw, wallCenterY),
                    new Vector2(1, wallHeight), wallColor);
        }

        // Right wall
        if (isLast)
        {
            CreatePlatform($"Wall_R_{roomIndex}", new Vector2(centerX + hw, 0), new Vector2(1, ROOM_H), wallColor);
        }
        else
        {
            float gapBottom = -hh + 0.5f;
            float gapTop = gapBottom + exitGap;
            float wallHeight = hh - gapTop;
            float wallCenterY = gapTop + wallHeight / 2f;

            if (wallHeight > 0.1f)
                CreatePlatform($"Wall_R_Top_{roomIndex}",
                    new Vector2(centerX + hw, wallCenterY),
                    new Vector2(1, wallHeight), wallColor);
        }
    }

    private void CreatePlatform(string name, Vector2 position, Vector2 size)
    {
        CreatePlatform(name, position, size, platformColor);
    }

    private void CreatePlatform(string name, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.layer = 8;
        go.transform.position = new Vector3(position.x, position.y, 0);
        go.transform.localScale = new Vector3(size.x, size.y, 1);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSquareSprite();
        sr.color = color;

        go.AddComponent<BoxCollider2D>();
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
    }

    private void CreateHazard(string name, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name);
        go.layer = 10;
        go.transform.position = new Vector3(position.x, position.y, 0);
        go.transform.localScale = new Vector3(size.x, size.y, 1);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSquareSprite();
        sr.color = hazardColor;
        sr.sortingOrder = 1;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        go.AddComponent<Hazard>();
    }

    private void CreateFuelGate(string name, Vector2 position, Vector2 size, FuelTier tier)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(position.x, position.y, 0);
        go.transform.localScale = new Vector3(size.x, size.y, 1);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSquareSprite();
        sr.sortingOrder = 1;

        go.AddComponent<BoxCollider2D>();

        var gate = go.AddComponent<FuelGate>();
        gate.Init(tier);
    }

    private static Sprite cachedSprite;
    private static Sprite GetSquareSprite()
    {
        if (cachedSprite != null) return cachedSprite;
        var tex = new Texture2D(16, 16);
        tex.filterMode = FilterMode.Point;
        var pixels = new Color[256];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        cachedSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        return cachedSprite;
    }
}

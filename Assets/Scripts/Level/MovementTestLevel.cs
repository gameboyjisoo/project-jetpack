using UnityEngine;

/// <summary>
/// Spawns a large test level at runtime for movement testing.
/// Attach to an empty GameObject in the scene.
/// Disable or remove once real levels exist.
/// </summary>
public class MovementTestLevel : MonoBehaviour
{
    [SerializeField] private Color groundColor = new Color(0.4f, 0.35f, 0.3f);
    [SerializeField] private Color platformColor = new Color(0.5f, 0.45f, 0.35f);
    [SerializeField] private Color wallColor = new Color(0.35f, 0.3f, 0.25f);

    private void Awake()
    {
        // Disable existing ground/platforms from the scene so they don't overlap
        DisableExistingPlatforms();

        // Floor
        CreatePlatform("Floor", new Vector2(0, -10), new Vector2(120, 2), groundColor);

        // Ceiling
        CreatePlatform("Ceiling", new Vector2(0, 30), new Vector2(120, 2), wallColor);

        // Walls
        CreatePlatform("Wall_Left", new Vector2(-60, 10), new Vector2(2, 42), wallColor);
        CreatePlatform("Wall_Right", new Vector2(60, 10), new Vector2(2, 42), wallColor);

        // Ground-level platforms (staircase left)
        CreatePlatform("Step_1", new Vector2(-40, -7), new Vector2(8, 1), platformColor);
        CreatePlatform("Step_2", new Vector2(-32, -4), new Vector2(6, 1), platformColor);
        CreatePlatform("Step_3", new Vector2(-24, -1), new Vector2(6, 1), platformColor);

        // Mid-height platforms (scattered)
        CreatePlatform("Plat_Mid_1", new Vector2(-10, 0), new Vector2(5, 1), platformColor);
        CreatePlatform("Plat_Mid_2", new Vector2(5, 3), new Vector2(5, 1), platformColor);
        CreatePlatform("Plat_Mid_3", new Vector2(20, 1), new Vector2(5, 1), platformColor);
        CreatePlatform("Plat_Mid_4", new Vector2(35, 4), new Vector2(5, 1), platformColor);

        // High platforms (jetpack targets)
        CreatePlatform("Plat_High_1", new Vector2(-15, 10), new Vector2(4, 1), platformColor);
        CreatePlatform("Plat_High_2", new Vector2(0, 14), new Vector2(4, 1), platformColor);
        CreatePlatform("Plat_High_3", new Vector2(15, 10), new Vector2(4, 1), platformColor);
        CreatePlatform("Plat_High_4", new Vector2(30, 13), new Vector2(4, 1), platformColor);

        // Top platforms (near ceiling)
        CreatePlatform("Plat_Top_1", new Vector2(-25, 20), new Vector2(6, 1), platformColor);
        CreatePlatform("Plat_Top_2", new Vector2(10, 22), new Vector2(6, 1), platformColor);
        CreatePlatform("Plat_Top_3", new Vector2(40, 20), new Vector2(6, 1), platformColor);

        // Vertical corridor (tight jetpack navigation)
        CreatePlatform("Corridor_L", new Vector2(48, 5), new Vector2(1, 20), wallColor);
        CreatePlatform("Corridor_R", new Vector2(54, 5), new Vector2(1, 20), wallColor);
        CreatePlatform("Corridor_Mid", new Vector2(51, 8), new Vector2(4, 1), platformColor);
        CreatePlatform("Corridor_Top", new Vector2(51, 16), new Vector2(4, 1), platformColor);

        // Fuel gates for testing
        // Red gate (needs low fuel) — blocks path between mid platforms
        CreateFuelGate("FuelGate_Red", new Vector2(12, 1), new Vector2(1, 4), FuelTier.Low);

        // Cyan gate (needs high fuel) — blocks access to high platform area
        CreateFuelGate("FuelGate_Cyan", new Vector2(-5, 8), new Vector2(1, 4), FuelTier.High);

        // Orange gate (needs mid fuel) — blocks corridor entrance
        CreateFuelGate("FuelGate_Orange", new Vector2(46, 0), new Vector2(1, 6), FuelTier.Mid);

        // Hazards (spikes along sections of the floor)
        CreateHazard("Spikes_1", new Vector2(-20, -8.5f), new Vector2(10, 0.5f));
        CreateHazard("Spikes_2", new Vector2(25, -8.5f), new Vector2(8, 0.5f));
        CreateHazard("Spikes_3", new Vector2(51, -8.5f), new Vector2(4, 0.5f));

        // Fuel pickups (mid-air, recharge jetpack gas)
        CreateFuelPickup("FuelPickup_1", new Vector2(-5, 5));
        CreateFuelPickup("FuelPickup_2", new Vector2(25, 8));
        CreateFuelPickup("FuelPickup_3", new Vector2(40, 12));

        // Dash pickups (mid-air, recharge dash ammo)
        CreateDashPickup("DashPickup_1", new Vector2(10, 7));
        CreateDashPickup("DashPickup_2", new Vector2(35, 5));
    }

    private void CreateFuelGate(string name, Vector2 position, Vector2 size, FuelTier tier)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(position.x, position.y, 0);
        go.transform.localScale = new Vector3(size.x, size.y, 1);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.sortingOrder = 1;

        var col = go.AddComponent<BoxCollider2D>();

        var gate = go.AddComponent<FuelGate>();
        gate.Init(tier);
    }

    private void CreateHazard(string name, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name);
        go.layer = 10; // Hazard layer
        go.transform.position = new Vector3(position.x, position.y, 0);
        go.transform.localScale = new Vector3(size.x, size.y, 1);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = new Color(1f, 0.2f, 0.2f, 0.8f); // Red-ish
        sr.sortingOrder = 1;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        go.AddComponent<Hazard>();
    }

    private void CreateFuelPickup(string name, Vector2 position)
    {
        var go = new GameObject(name);
        go.layer = 11; // Collectible layer
        go.transform.position = new Vector3(position.x, position.y, 0);
        go.transform.localScale = new Vector3(0.8f, 0.8f, 1);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = new Color(0f, 0.9f, 1f, 0.9f); // Cyan — matches full fuel color
        sr.sortingOrder = 2;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        go.AddComponent<GasRechargePickup>();
    }

    private void CreateDashPickup(string name, Vector2 position)
    {
        var go = new GameObject(name);
        go.layer = 11; // Collectible layer
        go.transform.position = new Vector3(position.x, position.y, 0);
        go.transform.localScale = new Vector3(0.8f, 0.8f, 1);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = new Color(1f, 0.5f, 1f, 0.9f); // Pink/magenta — distinct from fuel
        sr.sortingOrder = 2;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        go.AddComponent<DashRechargePickup>();
    }

    private void CreatePlatform(string name, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.layer = 8; // Ground layer
        go.transform.position = new Vector3(position.x, position.y, 0);
        go.transform.localScale = new Vector3(size.x, size.y, 1);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = color;

        var col = go.AddComponent<BoxCollider2D>();

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
    }

    private static Sprite cachedSprite;
    private static Sprite CreateSquareSprite()
    {
        if (cachedSprite != null) return cachedSprite;

        var tex = new Texture2D(16, 16);
        tex.filterMode = FilterMode.Point;
        var pixels = new Color[16 * 16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();

        cachedSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        return cachedSprite;
    }

    private void DisableExistingPlatforms()
    {
        string[] names = { "Ground", "Platform_Left", "Platform_Right", "Platform_Top" };
        foreach (var name in names)
        {
            var go = GameObject.Find(name);
            if (go != null)
                go.SetActive(false);
        }
    }
}

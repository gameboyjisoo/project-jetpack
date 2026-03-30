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

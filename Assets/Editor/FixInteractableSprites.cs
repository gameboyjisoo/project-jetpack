// Fixes all interactable objects (hazards, gates, pickups) that lost their
// runtime-created sprites on scene save. Assigns a persistent sprite asset
// and uses Tiled draw mode so the visual matches the collider area.
// Also fixes oversized gate colliders.
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class FixInteractableSprites
{
    public static void Execute()
    {
        // Load the persistent white square sprite (16x16, 16 PPU)
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiles/GroundSprite.png");
        if (sprite == null)
        {
            Debug.LogError("[FixSprites] Could not load Assets/Tiles/GroundSprite.png");
            return;
        }

        int fixed_count = 0;

        // Fix all Hazard objects
        foreach (var hazard in Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None))
        {
            var sr = hazard.GetComponent<SpriteRenderer>();
            var bc = hazard.GetComponent<BoxCollider2D>();
            if (sr != null && bc != null)
            {
                Undo.RecordObject(sr, "Fix hazard sprite");
                sr.sprite = sprite;
                sr.color = Color.red;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = bc.size; // match visual to collider
                sr.sortingOrder = 5;
                fixed_count++;
            }
        }

        // Fix all FuelGate objects
        foreach (var gate in Object.FindObjectsByType<FuelGate>(FindObjectsSortMode.None))
        {
            var sr = gate.GetComponent<SpriteRenderer>();
            var bc = gate.GetComponent<BoxCollider2D>();
            if (sr != null && bc != null)
            {
                Undo.RecordObject(bc, "Fix gate collider");
                // Gates should be 2 wide x 10 tall (not 4x18)
                bc.size = new Vector2(2f, 10f);

                Undo.RecordObject(sr, "Fix gate sprite");
                sr.sprite = sprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = bc.size;
                sr.sortingOrder = 5;
                // Color is set by FuelGate.Start() at runtime, but set it here too for editor visibility
                // Read the requiredTier from the component
                var so = new SerializedObject(gate);
                var tierProp = so.FindProperty("requiredTier");
                if (tierProp != null)
                {
                    // FuelTier enum: 0=High, 1=Mid, 2=Low
                    switch (tierProp.enumValueIndex)
                    {
                        case 0: sr.color = new Color(0f, 0.9f, 1f); break; // cyan
                        case 1: sr.color = new Color(1f, 0.6f, 0f); break; // orange
                        case 2: sr.color = new Color(1f, 0.15f, 0.15f); break; // red
                    }
                }
                fixed_count++;
            }
        }

        // Fix all GasRechargePickup objects
        foreach (var pickup in Object.FindObjectsByType<GasRechargePickup>(FindObjectsSortMode.None))
        {
            var sr = pickup.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Undo.RecordObject(sr, "Fix fuel pickup sprite");
                sr.sprite = sprite;
                sr.color = new Color(0f, 0.9f, 1f); // cyan
                sr.sortingOrder = 5;
                fixed_count++;
            }
        }

        // Fix all DashRechargePickup objects
        foreach (var pickup in Object.FindObjectsByType<DashRechargePickup>(FindObjectsSortMode.None))
        {
            var sr = pickup.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Undo.RecordObject(sr, "Fix dash pickup sprite");
                sr.sprite = sprite;
                sr.color = Color.magenta;
                sr.sortingOrder = 5;
                fixed_count++;
            }
        }

        // Fix placeholder objects (targets, swap zones) — find by name pattern
        foreach (var go in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (go.sprite != null) continue; // already has a sprite
            if (go.name.Contains("Target") || go.name.Contains("SwapZone"))
            {
                Undo.RecordObject(go, "Fix placeholder sprite");
                go.sprite = sprite;
                go.sortingOrder = 5;
                // Keep existing color (yellow for targets, green for swap zones)
                fixed_count++;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[FixSprites] Fixed {fixed_count} objects with persistent sprite.");
    }
}
#endif

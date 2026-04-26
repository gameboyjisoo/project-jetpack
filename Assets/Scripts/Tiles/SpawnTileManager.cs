using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// Attach to any Tilemap that contains SpawnTiles.
/// At runtime: spawns prefabs at each SpawnTile position, then clears all tile visuals.
/// This is the single source of truth — SpawnTile itself has no runtime logic.
/// </summary>
[RequireComponent(typeof(Tilemap))]
public class SpawnTileManager : MonoBehaviour
{
    private void Awake()
    {
        var tilemap = GetComponent<Tilemap>();
        BoundsInt bounds = tilemap.cellBounds;

        // Collect all SpawnTiles first, then process
        // (avoid modifying tilemap while iterating)
        var toSpawn = new List<(Vector3Int pos, SpawnTile tile)>();

        foreach (var pos in bounds.allPositionsWithin)
        {
            var tile = tilemap.GetTile(pos) as SpawnTile;
            if (tile != null)
                toSpawn.Add((pos, tile));
        }

        // Spawn prefabs and clear tiles
        foreach (var (pos, tile) in toSpawn)
        {
            if (tile.prefab != null)
            {
                Vector3 worldPos = tilemap.CellToWorld(pos) + tilemap.cellSize * 0.5f;
                var instance = Instantiate(tile.prefab, worldPos, Quaternion.identity, transform.parent);
                instance.name = $"{tile.prefab.name}_{pos.x}_{pos.y}";
            }

            // Clear the placeholder tile visual
            tilemap.SetTile(pos, null);
        }
    }
}

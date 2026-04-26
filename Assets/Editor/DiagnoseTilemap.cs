using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public static class DiagnoseTilemap
{
    public static void Execute()
    {
        // Scan ALL tilemaps in the scene
        var tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);

        foreach (var tilemap in tilemaps)
        {
            string path = GetPath(tilemap.transform);
            BoundsInt bounds = tilemap.cellBounds;

            var tileCounts = new Dictionary<string, int>();
            int total = 0;

            foreach (var pos in bounds.allPositionsWithin)
            {
                var tile = tilemap.GetTile(pos);
                if (tile != null)
                {
                    string key = $"{tile.GetType().Name}:{tile.name}";
                    if (!tileCounts.ContainsKey(key)) tileCounts[key] = 0;
                    tileCounts[key]++;
                    total++;
                }
            }

            if (total == 0) continue;

            Debug.Log($"[Diagnose] === {path} === ({total} tiles)");
            foreach (var kv in tileCounts)
                Debug.Log($"  {kv.Key} × {kv.Value}");
        }
    }

    static string GetPath(Transform t)
    {
        var parts = new List<string>();
        while (t != null) { parts.Insert(0, t.name); t = t.parent; }
        return string.Join("/", parts);
    }
}

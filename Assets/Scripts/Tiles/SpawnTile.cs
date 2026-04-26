using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// A tile that represents an interactable object in the editor.
/// The tile is just a colored placeholder — SpawnTileManager handles
/// instantiating the prefab and clearing the tile visual at runtime.
/// </summary>
[CreateAssetMenu(menuName = "Project Jetpack/Tiles/Spawn Tile")]
public class SpawnTile : TileBase
{
    [Header("Editor Visual (placeholder)")]
    [Tooltip("Placeholder sprite shown in the tile palette and editor. Will be replaced with real art later.")]
    public Sprite editorSprite;
    public Color editorColor = Color.white;

    [Header("Runtime")]
    [Tooltip("The prefab to instantiate at this tile's position when the game starts.")]
    public GameObject prefab;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = editorSprite;
        tileData.color = editorColor;
        tileData.colliderType = Tile.ColliderType.None;
    }
}

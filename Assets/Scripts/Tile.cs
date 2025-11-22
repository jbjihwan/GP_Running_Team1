using UnityEngine;

/// <summary>
/// [Tile]
/// - Calculates the actual Z-length of this tile so that TileManager
///   can place tiles correctly in a continuous loop.
/// - Automatically detects the size from all Renderers inside the prefab,
///   so even if the scale or mesh changes, the tile length stays accurate.
/// </summary>
public class Tile : MonoBehaviour
{
    [Tooltip("Actual Z-length of this tile (auto-calculated)")]
    public float tileLength = 10f;

    private void Awake()
    {
        // Find all Renderers under this object (including children)
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            // Start with the first renderer bounds
            Bounds bounds = renderers[0].bounds;

            // Combine bounds of all child renderers
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            // Final Z size becomes the tile length
            tileLength = bounds.size.z;
        }
    }

    /// <summary>
    /// Returns the calculated Z-length of this tile.
    /// </summary>
    public float GetLength()
    {
        return tileLength;
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [TileManager]
/// - Creates an infinite map illusion by spawning tiles in sequence
///   and recycling tiles that move behind the player.
/// - Uses tilePrefabs in order (0¡æ1¡æ2¡æ3¡æ0¡æ1...).
/// - Each tile prefab must contain a Tile script that provides GetLength().
/// 
/// Usage:
///   1. Create an empty GameObject and attach TileManager.
///   2. Assign the Player Transform.
///   3. Add tilePrefabs in the order they should appear.
///   4. Set startTileCount = tilePrefabs.Length for a full initial set.
/// </summary>
public class TileManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Player Transform used for determining when to recycle tiles")]
    public Transform player;

    [Header("Tile Settings")]
    [Tooltip("Tile prefabs that will repeat in order (0¡æ1¡æ2¡æ...¡æ0)")]
    public Tile[] tilePrefabs;

    [Tooltip("Number of tiles to spawn at the beginning")]
    public int startTileCount = 4;

    // Length of each tile (Z direction). Determined from the first tile¡¯s GetLength().
    private float tileLength = 10f;

    [Header("Movement Settings")]
    [Tooltip("Speed of tile movement toward negative Z")]
    public float moveSpeed = 5f;

    [Tooltip("If a tile moves this far behind the player, it will be recycled")]
    public float recycleDistance = 10f;

    // Currently active tiles placed in the scene
    private readonly List<Tile> activeTiles = new List<Tile>();

    // Index of the next tile to spawn (loops)
    private int nextIndex = 0;

    private void Start()
    {
        // Basic validation
        if (player == null)
        {
            Debug.LogError("TileManager: Player reference is missing!");
            return;
        }

        if (tilePrefabs == null || tilePrefabs.Length == 0)
        {
            Debug.LogError("TileManager: tilePrefabs is empty!");
            return;
        }

        // Initial Z position for spawning tiles
        float spawnZ = player.position.z;

        // ---------------------------------
        // 1) Spawn first tile
        // ---------------------------------
        Tile firstTile = CreateTile(tilePrefabs[0], spawnZ);

        // Read tile length from Tile script
        tileLength = firstTile.GetLength();

        activeTiles.Add(firstTile);
        spawnZ += tileLength;

        // Next tile index starts at 1
        nextIndex = 1;

        // ---------------------------------
        // 2) Spawn remaining tiles
        // ---------------------------------
        for (int i = 1; i < startTileCount; i++)
        {
            Tile newTile = CreateTile(tilePrefabs[nextIndex], spawnZ);

            activeTiles.Add(newTile);
            spawnZ += tileLength;

            // Loop index
            nextIndex = (nextIndex + 1) % tilePrefabs.Length;
        }

        // If recycleDistance is not set, assign tile length
        if (recycleDistance <= 0f)
            recycleDistance = tileLength;
    }

    private void Update()
    {
        MoveTiles();
        RecycleTilesIfNeeded();
    }

    /// <summary>
    /// Instantiates a tile prefab at the given Z position (X,Y fixed at 0).
    /// </summary>
    private Tile CreateTile(Tile prefab, float zPos)
    {
        Vector3 pos = new Vector3(0f, 0f, zPos);
        return Instantiate(prefab, pos, Quaternion.identity, transform);
    }

    /// <summary>
    /// Moves all active tiles toward negative Z.
    /// This creates the illusion that the player is moving forward.
    /// </summary>
    private void MoveTiles()
    {
        Vector3 move = Vector3.back * moveSpeed * Time.deltaTime;

        for (int i = 0; i < activeTiles.Count; i++)
        {
            activeTiles[i].transform.position += move;
        }
    }

    /// <summary>
    /// Recycles a tile that has moved behind the player
    /// and repositions it in front to form an infinite loop.
    /// </summary>
    private void RecycleTilesIfNeeded()
    {
        if (activeTiles.Count == 0) return;

        Tile firstTile = activeTiles[0];
        Tile lastTile = activeTiles[activeTiles.Count - 1];

        // Z position threshold for recycling
        float recycleZ = player.position.z - recycleDistance;

        // If the first tile has passed behind the threshold
        if (firstTile.transform.position.z < recycleZ)
        {
            // Move this tile to the end of the line
            float newZ = lastTile.transform.position.z + tileLength;

            Vector3 pos = firstTile.transform.position;
            pos.z = newZ;
            firstTile.transform.position = pos;

            // Update list order (front ¡æ back)
            activeTiles.RemoveAt(0);
            activeTiles.Add(firstTile);
        }
    }
}

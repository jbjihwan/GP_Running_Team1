using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [SpawnManager]
/// - Assumes the player stays still while the world (tiles/cubes) moves.
/// - Spawns a set of 3 AnswerCubes every given interval,
///   based on this object's transform.position.
/// - Actual problem generation and assignment (multiplication, correct/incorrect)
///   is handled by QuestionManager.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    [Header("Prefab Reference")]
    [Tooltip("AnswerCube prefab with the text component attached")]
    public AnswerCube answerCubePrefab;

    [Header("Lane / Position Settings")]
    [Tooltip("X offset for left/center/right lanes.\nExample: offset 2 -> -2, 0, 2")]
    public float laneOffset = 2f;

    [Tooltip("Y position where cubes will be spawned (match ground height)")]
    public float spawnY = 1f;

    [Header("Spawn Interval Settings")]
    [Tooltip("Interval (seconds) between problem spawns")]
    public float spawnInterval = 20f;

    [Tooltip("If true, spawn immediately once at game start")]
    public bool spawnOnStart = true;

    // Internal timer
    private float timer = 0f;

    private void Start()
    {
        // Spawn one question immediately when the game starts
        if (spawnOnStart)
        {
            SpawnQuestionSetAtCurrentPosition();
        }

        timer = 0f;
    }

    private void Update()
    {
        // Accumulate time
        timer += Time.deltaTime;

        // When enough time has passed, spawn a new set
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnQuestionSetAtCurrentPosition();
        }
    }

    /// <summary>
    /// Spawns 3 AnswerCubes at the current Z position of this object.
    /// Then sends them to QuestionManager to assign numbers and correctness.
    /// </summary>
    private void SpawnQuestionSetAtCurrentPosition()
    {
        if (answerCubePrefab == null)
        {
            Debug.LogError("[SpawnManager] answerCubePrefab is null!");
            return;
        }

        if (QuestionManager.instance == null)
        {
            Debug.LogError("[SpawnManager] QuestionManager instance not found!");
            return;
        }

        // 1) Use this object's Z position as spawn base
        float spawnZ = transform.position.z;

        // 2) Prepare list to collect 3 spawned AnswerCubes
        List<AnswerCube> cubes = new List<AnswerCube>();

        // 3) Lane X positions: -laneOffset, 0, +laneOffset
        float[] laneX = { -laneOffset, 0f, laneOffset };

        for (int i = 0; i < 3; i++)
        {
            Vector3 pos = new Vector3(laneX[i], spawnY, spawnZ);

            // Instantiate AnswerCube
            AnswerCube cube = Instantiate(answerCubePrefab, pos, Quaternion.identity);

            cubes.Add(cube);
        }

        // 4) Pass the set of cubes to QuestionManager
        //    so it can assign values, correctness, and text.
        QuestionManager.instance.SetupQuestion(cubes);
    }
}

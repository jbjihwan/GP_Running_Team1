using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// [StageManager]
/// - Controls sequential stage transitions based on time.
/// - Each stage corresponds to a root GameObject (StageRoot).
/// - When changing stage:
///     1) Disable previous stage root
///     2) Enable the new stage root
///     3) Play stage BGM through SoundManager
///
/// - Example configuration:
///     Stage 0: 90 seconds
///     Stage 1: 120 seconds
///     Stage 2: 120 seconds ¡æ Game Clear
///
/// - Usage:
///     1) Create an empty GameObject in the scene and attach this script
///     2) Assign stageRoots in order (0, 1, 2...)
///     3) Set stageDurations (seconds)
///     4) Assign onGameClear event (UI popup, scene change, etc.)
/// </summary>
public class StageManager : MonoBehaviour
{
    // Singleton instance
    public static StageManager Instance;

    [Header("Stage Root Objects")]
    [Tooltip("Root GameObject for each stage.\nExample: 0 = Grass, 1 = Dungeon IN, 2 = Dungeon OUT")]
    public GameObject[] stageRoots;

    [Header("Stage Durations (in seconds)")]
    [Tooltip("Duration (seconds) for each stage.\nRecommended: Same length as stageRoots.")]
    public float[] stageDurations = { 90f, 120f, 120f };

    [Header("Game Clear Event")]
    [Tooltip("Event called when all stages are completed.\nExample: Show Game Clear UI, go to menu, etc.")]
    public UnityEvent onGameClear;

    // Current stage index (0,1,2...)
    private int currentStageIndex = 0;

    // Time elapsed in the current stage
    private float stageTimer = 0f;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (stageRoots == null || stageRoots.Length == 0)
        {
            Debug.LogError("[StageManager] stageRoots is empty. Assign stage root objects.");
            return;
        }

        if (stageDurations == null || stageDurations.Length == 0)
        {
            Debug.LogWarning("[StageManager] stageDurations is empty. Default durations recommended.");
        }

        // Start at stage 0
        currentStageIndex = 0;
        stageTimer = 0f;

        // Activate stage 0, deactivate the rest
        SetStage(currentStageIndex);
    }

    private void Update()
    {
        if (stageRoots == null || stageRoots.Length == 0)
            return;

        // Increase timer
        stageTimer += Time.deltaTime;

        // Get target duration
        float duration = GetCurrentStageDuration();

        // If time is up, move to next stage
        if (stageTimer >= duration)
        {
            GoToNextStage();
        }
    }

    /// <summary>
    /// Returns duration (seconds) for the current stage.
    /// If stageDurations is shorter than stage count, last value is reused.
    /// </summary>
    private float GetCurrentStageDuration()
    {
        if (stageDurations == null || stageDurations.Length == 0)
            return 99999f; // Fail-safe: effectively infinite stage

        if (currentStageIndex < stageDurations.Length)
            return stageDurations[currentStageIndex];

        // Reuse last value if array is shorter
        return stageDurations[stageDurations.Length - 1];
    }

    /// <summary>
    /// Activates the given stage index:
    ///   - Enables that stage root
    ///   - Disables all others
    ///   - Plays the stage BGM
    /// </summary>
    private void SetStage(int index)
    {
        for (int i = 0; i < stageRoots.Length; i++)
        {
            if (stageRoots[i] != null)
                stageRoots[i].SetActive(i == index);
        }

        Debug.Log("[StageManager] Stage " + index + " started");

        // Play BGM for this stage
        if (SoundManager.instance != null)
            SoundManager.instance.PlayStageBgm(index);
    }

    /// <summary>
    /// Moves to the next stage.
    /// If no more stages remain ¡æ game clear.
    /// </summary>
    private void GoToNextStage()
    {
        int nextStage = currentStageIndex + 1;

        if (nextStage < stageRoots.Length)
        {
            currentStageIndex = nextStage;
            stageTimer = 0f; // Reset timer
            SetStage(currentStageIndex);
        }
        else
        {
            Debug.Log("[StageManager] All stages completed! Game Clear!");

            if (SoundManager.instance != null)
            {
                SoundManager.instance.StopBgm();
                // Optional: play game clear BGM here
            }

            if (onGameClear != null)
                onGameClear.Invoke();

            // Stop running Update()
            enabled = false;
        }
    }

    // --------------------------------------------------------
    // Public helper functions (for UI, debugging, etc.)
    // --------------------------------------------------------

    /// <summary>
    /// Returns current stage index (0,1,2...)
    /// </summary>
    public int GetCurrentStageIndex()
    {
        return currentStageIndex;
    }

    /// <summary>
    /// Returns elapsed time of the current stage.
    /// </summary>
    public float GetCurrentStageElapsedTime()
    {
        return stageTimer;
    }

    /// <summary>
    /// Returns remaining time of the current stage.
    /// </summary>
    public float GetCurrentStageRemainTime()
    {
        float duration = GetCurrentStageDuration();
        return Mathf.Max(0f, duration - stageTimer);
    }
}

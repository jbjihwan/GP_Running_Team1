using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// QuestionManager:
/// - Manages multiplication questions presented to the player.
/// - Does not spawn AnswerCubes itself. Instead, it receives already-spawned cubes
///   from SpawnManager and assigns values (correct/incorrect).
/// - Handles:
///     * Generating multiplication problems
///     * Creating correct and incorrect answer options
///     * Assigning answer values to cubes
///     * Showing question UI text
///     * Checking answers when player collides with a cube
///     * Applying damage on wrong answers
///     * Cleaning up cubes and UI after question is finished
/// </summary>
public class QuestionManager : MonoBehaviour
{
    public static QuestionManager instance;

    // ---------------------------------------------------------
    // Difficulty Settings
    // ---------------------------------------------------------
    [Header("Question Difficulty Settings")]
    [Tooltip("Minimum multiplication number (example: 2 means 2x table)")]
    public int minNumber = 2;

    [Tooltip("Maximum multiplication number")]
    public int maxNumber = 9;

    // ---------------------------------------------------------
    // UI Settings
    // ---------------------------------------------------------
    [Header("UI Settings")]
    [Tooltip("Text UI that displays the current math question")]
    public TextMeshProUGUI questionText;

    [Tooltip("How long the question text should stay visible (seconds)")]
    public float questionShowTime = 5f;

    // ---------------------------------------------------------
    // Damage Settings
    // ---------------------------------------------------------
    [Header("Wrong Answer Damage Settings")]
    [Tooltip("Damage applied when player selects an incorrect answer")]
    public int wrongDamage = 2;

    // ---------------------------------------------------------
    // Internal State
    // ---------------------------------------------------------
    private int currentA;
    private int currentB;
    private int currentAnswer;

    // Stores currently active cubes (the three choices)
    private readonly List<AnswerCube> activeCubes = new List<AnswerCube>();

    private Coroutine questionUICoroutine;

    // Ensures one question runs at a time
    private bool questionActive = false;

    private void Awake()
    {
        // Simple Singleton setup
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ---------------------------------------------------------
    // PUBLIC API - Called by SpawnManager
    // ---------------------------------------------------------

    /// <summary>
    /// Assigns a new multiplication question to the provided AnswerCubes.
    /// Called by SpawnManager after it spawns a set of 3 cubes.
    /// 
    /// Steps:
    ///  1. Generate random multiplication problem
    ///  2. Create 1 correct and 2 incorrect answer numbers
    ///  3. Shuffle answers and assign to cubes
    ///  4. Display question UI
    /// </summary>
    public void SetupQuestion(IList<AnswerCube> cubes)
    {
        if (cubes == null || cubes.Count == 0)
        {
            Debug.LogWarning("[QuestionManager] SetupQuestion called with an empty cube list.");
            return;
        }

        if (questionActive)
        {
            Debug.Log("[QuestionManager] A question is already active. New question skipped.");
            return;
        }

        questionActive = true;
        activeCubes.Clear();

        // Step 1: Generate random question
        GenerateQuestion();

        // Step 2: Generate correct + incorrect answers
        int[] answers = GenerateAnswerOptions(currentAnswer);

        // Step 3: Shuffle answers
        ShuffleArray(answers);

        // Step 4: Assign numbers to the existing cubes
        int length = Mathf.Min(answers.Length, cubes.Count);

        for (int i = 0; i < length; i++)
        {
            AnswerCube cube = cubes[i];
            if (cube == null) continue;

            int value = answers[i];
            bool isCorrect = (value == currentAnswer);

            cube.Setup(value, isCorrect);
            activeCubes.Add(cube);
        }

        // Step 5: Show the question UI text
        ShowQuestionUI();
    }

    // ---------------------------------------------------------
    // QUESTION GENERATION
    // ---------------------------------------------------------

    /// <summary>
    /// Picks two random integers (A and B) and computes A * B.
    /// </summary>
    private void GenerateQuestion()
    {
        currentA = Random.Range(minNumber, maxNumber + 1);
        currentB = Random.Range(minNumber, maxNumber + 1);
        currentAnswer = currentA * currentB;
    }

    /// <summary>
    /// Generates an array of answer options:
    ///     [0] = correct answer
    ///     [1,2] = incorrect values near the correct one
    /// Ensures:
    ///     - No duplicates
    ///     - No zero / negative values
    /// </summary>
    private int[] GenerateAnswerOptions(int correct)
    {
        int[] values = new int[3];
        values[0] = correct;

        int index = 1;

        while (index < 3)
        {
            // Generate candidate near correct answer (range -5 to +5)
            int offset = Random.Range(-5, 6);
            int candidate = correct + offset;

            if (candidate <= 0) continue;
            if (candidate == correct) continue;

            // Check duplicates
            bool duplicate = false;
            for (int i = 0; i < index; i++)
            {
                if (values[i] == candidate)
                {
                    duplicate = true;
                    break;
                }
            }
            if (duplicate) continue;

            values[index] = candidate;
            index++;
        }

        return values;
    }

    /// <summary>
    /// Randomly shuffles an integer array using Fisher-Yates algorithm.
    /// </summary>
    private void ShuffleArray(int[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            int r = Random.Range(i, array.Length);
            int tmp = array[i];
            array[i] = array[r];
            array[r] = tmp;
        }
    }

    // ---------------------------------------------------------
    // UI DISPLAY
    // ---------------------------------------------------------

    /// <summary>
    /// Shows the multiplication problem at the top of the screen.
    /// </summary>
    private void ShowQuestionUI()
    {
        if (questionText == null) return;

        questionText.text = currentA + " x " + currentB + " = ?";
        questionText.gameObject.SetActive(true);

        if (questionUICoroutine != null)
            StopCoroutine(questionUICoroutine);

        questionUICoroutine = StartCoroutine(Co_HideQuestionAfterDelay());
    }

    /// <summary>
    /// Hides the problem text after a set amount of time.
    /// </summary>
    private IEnumerator Co_HideQuestionAfterDelay()
    {
        yield return new WaitForSeconds(questionShowTime);

        if (questionText != null)
            questionText.gameObject.SetActive(false);
    }

    // ---------------------------------------------------------
    // ANSWER RESOLUTION
    // ---------------------------------------------------------

    /// <summary>
    /// Called by AnswerCube when the player collides with a cube.
    /// Handles:
    ///   - Correct/incorrect answer check
    ///   - Applying damage on wrong answer
    ///   - Cleaning up question and cubes
    /// </summary>
    public void ResolveAnswer(AnswerCube cube, PlayerEvent playerEvent)
    {
        if (!questionActive) return;

        bool correct = cube.isCorrect;

        if (correct)
        {
            Debug.Log("[QuestionManager] Correct answer!");
            // TODO: Add correct SFX, score system, etc.
        }
        else
        {
            Debug.Log("[QuestionManager] Wrong answer!");
            if (playerEvent != null)
                playerEvent.OnWrongAnswer(wrongDamage);
        }

        EndQuestion();
    }

    /// <summary>
    /// Ends the current question:
    ///   - Hides UI text
    ///   - Destroys remaining AnswerCubes (they may already be destroyed)
    ///   - Resets internal state for the next problem
    /// </summary>
    private void EndQuestion()
    {
        questionActive = false;

        if (questionText != null)
            questionText.gameObject.SetActive(false);

        foreach (var cube in activeCubes)
        {
            if (cube != null)
                Destroy(cube.gameObject);
        }

        activeCubes.Clear();
    }
}

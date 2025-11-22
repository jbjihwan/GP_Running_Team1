using UnityEngine;
using TMPro;

public class AnswerCube : MonoBehaviour
{
    [Header("UI Text")]
    public TextMeshPro text;

    [Header("Value / Correctness")]
    public int answerValue;
    public bool isCorrect;

    [Header("Movement Settings")]
    public float moveSpeed = 8f;     // Speed of cube moving toward the player (-Z)
    public float destroyZ = -10f;    // Automatically destroy when passing behind camera

    private bool used = false;

    private void Update()
    {
        // Move the cube forward (toward negative Z direction)
        transform.position += Vector3.back * moveSpeed * Time.deltaTime;

        // Destroy the cube if it goes too far behind
        if (transform.position.z < destroyZ)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called by QuestionManager to assign the value and correctness.
    /// </summary>
    public void Setup(int value, bool isCorrect)
    {
        this.answerValue = value;
        this.isCorrect = isCorrect;

        // Display the value on the text UI
        if (text != null)
            text.text = value.ToString();
    }

    /// <summary>
    /// Handles player collision to determine correct/incorrect answer.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (used) return;
        if (!other.CompareTag("Player")) return;

        used = true;

        // Get PlayerEvent component
        PlayerEvent playerEvent = other.GetComponent<PlayerEvent>();
        if (playerEvent == null)
            playerEvent = other.GetComponentInParent<PlayerEvent>();

        // Send result to QuestionManager
        if (QuestionManager.instance != null && playerEvent != null)
        {
            QuestionManager.instance.ResolveAnswer(this, playerEvent);
        }

        // Remove the cube immediately after interaction
        Destroy(gameObject);
    }
}

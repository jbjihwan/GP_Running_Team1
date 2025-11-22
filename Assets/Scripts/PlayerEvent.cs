using UnityEngine;

/// <summary>
/// [PlayerEvent]
/// - Handles all player HP and damage-related events.
/// - Any damage source (traps, wrong answer, etc.) should call these functions.
/// </summary>
public class PlayerEvent : MonoBehaviour
{
    [Header("HP Settings")]
    [Tooltip("Player maximum HP")]
    public int maxHP = 5;

    [Tooltip("Current HP (initialized to maxHP on start)")]
    public int currentHP;

    // Flag to indicate whether the player is dead
    public bool isDead { get; private set; } = false;

    private void Awake()
    {
        currentHP = maxHP;
    }

    /// <summary>
    /// General damage processing method.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        int finalDamage = Mathf.Max(1, damage); // minimum 1 damage

        currentHP -= finalDamage;
        Debug.Log($"[PlayerEvent] Took {finalDamage} damage ¡æ Current HP: {currentHP}");

        // If HP falls to 0 or below, trigger death
        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }

        // TODO: Update HP bar UI here (Slider or TMP)
    }

    /// <summary>
    /// Called when hit by a trap.
    /// </summary>
    public void OnTrapHit(int baseDamage)
    {
        Debug.Log("[PlayerEvent] Hit by trap!");
        TakeDamage(baseDamage);

        // TODO: Play trap hit SFX, camera shake, effects, etc.
        // if (SoundManager.instance != null) SoundManager.instance.PlaySfx(trapSfxIndex);
    }

    /// <summary>
    /// Called when the player answers a multiplication question incorrectly.
    /// </summary>
    public void OnWrongAnswer(int baseDamage)
    {
        Debug.Log("[PlayerEvent] Wrong answer!");
        TakeDamage(baseDamage);

        // TODO: Play wrong-answer SFX, flash red UI, warning effects 
        // if (SoundManager.instance != null) SoundManager.instance.PlaySfx(wrongAnswerSfxIndex);
    }

    /// <summary>
    /// Handles player death when HP reaches zero.
    /// </summary>
    private void Die()
    {
        isDead = true;
        Debug.Log("[PlayerEvent] Player died!");

        // TODO: death animation, disable input, show game over UI, etc.
        // GameManager.instance.GameOver();
    }
}

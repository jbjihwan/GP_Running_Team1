using UnityEngine;

/// <summary>
/// [PlayerEvent]
/// - 플레이어 HP와 데미지 처리를 전부 담당하는 스크립트.
/// - 함정 맞음, 곱셈 문제 틀림 같은 이벤트는 전부 여기 함수만 호출하면 됨.
/// </summary>
public class PlayerEvent : MonoBehaviour
{
    [Header("HP 설정")]
    [Tooltip("플레이어 최대 HP")]
    public int maxHP = 5;

    [Tooltip("현재 HP (시작 시 maxHP로 초기화)")]
    public int currentHP;

    public bool isDead { get; private set; } = false;

    private void Awake()
    {
        currentHP = maxHP;
    }

    /// <summary>
    /// 공통 데미지 처리 함수
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        int finalDamage = Mathf.Max(1, damage); // 최소 1이상

        currentHP -= finalDamage;
        Debug.Log($"[PlayerEvent] 데미지 {finalDamage} 입음 → 현재 HP: {currentHP}");

        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }

        // TODO: HP바 UI 연결하고 싶으면 여기서 슬라이더 업데이트
    }

    /// <summary>
    /// 함정에 맞았을 때 호출
    /// </summary>
    public void OnTrapHit(int baseDamage)
    {
        Debug.Log("[PlayerEvent] 함정 피격!");
        TakeDamage(baseDamage);

        // TODO: 함정 피격 SFX, 화면 흔들기 등
        // if (SoundManager.instance != null) SoundManager.instance.PlaySfx(함정Sfx인덱스);
    }

    /// <summary>
    /// 곱셈 문제를 틀렸을 때 호출
    /// </summary>
    public void OnWrongAnswer(int baseDamage)
    {
        Debug.Log("[PlayerEvent] 곱셈 문제 오답!");
        TakeDamage(baseDamage);

        // TODO: 오답 SFX, 경고 UI 등
        // if (SoundManager.instance != null) SoundManager.instance.PlaySfx(오답Sfx인덱스);
    }

    /// <summary>
    /// HP 0일 때 처리
    /// </summary>
    private void Die()
    {
        isDead = true;
        Debug.Log("[PlayerEvent] 플레이어 사망!");

        // TODO: 사망 애니메이션, 게임오버 UI, 입력 막기 등
        // GameManager.instance.GameOver();
    }
}

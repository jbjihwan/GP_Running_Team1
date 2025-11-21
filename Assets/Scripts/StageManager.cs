using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// [StageManager]
/// - 여러 개의 스테이지(StageRoot)를 시간에 따라 순서대로 전환해주는 매니저.
/// - 이 프로젝트에서는:
///     0번 스테이지: 90초 (1분 30초)
///     1번 스테이지: 120초 (2분)
///     2번 스테이지: 120초 (2분) 후 → 게임 클리어
///
/// - 스테이지 전환 시:
///     1) 이전 스테이지 GameObject 비활성화
///     2) 새로운 스테이지 GameObject 활성화
///     3) SoundManager를 통해 해당 스테이지 BGM 재생
///
/// - 사용법:
///   1) 씬에 빈 GameObject 생성 → StageManager 스크립트 추가
///   2) stageRoots 배열에 스테이지 루트 오브젝트들 드래그 (0,1,2 순서)
///   3) stageDurations 배열에 각 스테이지 지속시간(초)을 넣기
///   4) onGameClear 이벤트에 게임 클리어 UI/로직 연결
/// </summary>
public class StageManager : MonoBehaviour
{
    // 싱글톤으로 접근할 수 있게 하는 정적 인스턴스
    public static StageManager Instance;

    [Header("스테이지 루트 오브젝트들")]
    [Tooltip("각 스테이지의 Root GameObject.\n예) 0: 초원, 1: 던전IN, 2: 던전OUT")]
    public GameObject[] stageRoots;

    [Header("각 스테이지 유지 시간 (초 단위)")]
    [Tooltip("각 스테이지가 유지되는 시간(초).\n배열 길이가 stageRoots 길이와 같거나, 더 길게 설정하는 것을 권장.")]
    // 기본값: [0]=90초, [1]=120초, [2]=120초
    public float[] stageDurations = { 90f, 120f, 120f };

    [Header("게임 클리어 시 호출할 이벤트")]
    [Tooltip("모든 스테이지가 끝났을 때 실행할 이벤트.\n예: Game Clear UI 활성화, 결과 화면, 메인메뉴 전환 등.")]
    public UnityEvent onGameClear;

    // 현재 몇 번째 스테이지인지 (0, 1, 2, ...)
    private int currentStageIndex = 0;

    // 현재 스테이지에서 경과된 시간(초)
    private float stageTimer = 0f;

    private void Awake()
    {
        // 싱글톤 패턴: 인스턴스가 없으면 자신을 등록, 있으면 자신을 제거
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
        // 기본적인 안전 체크
        if (stageRoots == null || stageRoots.Length == 0)
        {
            Debug.LogError("[StageManager] stageRoots가 비어 있습니다. 스테이지 루트 오브젝트를 등록해주세요.");
            return;
        }

        // durations 배열이 비었으면, 매우 큰 값으로 대체하여 사실상 무한 스테이지처럼 됨
        if (stageDurations == null || stageDurations.Length == 0)
        {
            Debug.LogWarning("[StageManager] stageDurations가 비어 있습니다. 기본값(90,120,120)을 사용하는 것을 권장합니다.");
        }

        // 처음 시작은 0번 스테이지
        currentStageIndex = 0;
        stageTimer = 0f;

        // 0번 스테이지를 활성화하고 나머지는 비활성화
        SetStage(currentStageIndex);
    }

    private void Update()
    {
        // 스테이지 루트가 설정되어 있지 않다면 동작 X
        if (stageRoots == null || stageRoots.Length == 0)
            return;

        // 현재 스테이지에서 경과 시간 누적
        stageTimer += Time.deltaTime;

        // 현재 스테이지의 목표 시간을 가져오기
        float duration = GetCurrentStageDuration();

        // 목표 시간을 넘겼다면 다음 스테이지로 전환 시도
        if (stageTimer >= duration)
        {
            GoToNextStage();
        }
    }

    /// <summary>
    /// 현재 스테이지의 목표 지속 시간(초)을 반환.
    /// - stageDurations 길이가 부족하면 마지막 값을 재사용.
    /// </summary>
    private float GetCurrentStageDuration()
    {
        if (stageDurations == null || stageDurations.Length == 0)
        {
            // 방어코드: 설정이 전혀 안 되어 있으면 말도 안되게 큰 값 리턴 (사실상 안 바뀜)
            return 99999f;
        }

        if (currentStageIndex < stageDurations.Length)
        {
            // 정상 범위: 현재 인덱스에 해당하는 값 사용
            return stageDurations[currentStageIndex];
        }
        else
        {
            // 스테이지가 durations보다 많은 경우, 마지막 값 재사용
            return stageDurations[stageDurations.Length - 1];
        }
    }

    /// <summary>
    /// 특정 스테이지(index)를 활성화하고, 나머지는 비활성화.
    /// - 또한 해당 스테이지 BGM을 SoundManager를 통해 재생.
    /// </summary>
    private void SetStage(int index)
    {
        // 모든 스테이지 루트 중에서, index에 해당하는 것만 켜고 나머지는 끄기
        for (int i = 0; i < stageRoots.Length; i++)
        {
            if (stageRoots[i] != null)
            {
                stageRoots[i].SetActive(i == index);
            }
        }

        Debug.Log($"[StageManager] Stage {index} 시작");

        // 스테이지 변경 시 BGM도 함께 변경
        if (SoundManager.instance != null)
        {
            // SoundManager의 stageBgmClips 배열에서 index번에 해당하는 BGM 재생
            SoundManager.instance.PlayStageBgm(index);
        }
    }

    /// <summary>
    /// 다음 스테이지로 전환.
    /// - 다음 스테이지가 있으면 → 전환 & 타이머 리셋
    /// - 다음 스테이지가 없으면 → 모든 스테이지 완료 → 게임 클리어 처리
    /// </summary>
    private void GoToNextStage()
    {
        int nextStage = currentStageIndex + 1;

        // 아직 다음 스테이지가 남아 있는 경우
        if (nextStage < stageRoots.Length)
        {
            currentStageIndex = nextStage;
            stageTimer = 0f;          // 타이머 리셋
            SetStage(currentStageIndex);
        }
        else
        {
            // 여기로 왔다는 건 마지막 스테이지까지 모두 끝났다는 뜻 = 게임 클리어
            Debug.Log("[StageManager] 모든 스테이지 완료! 게임 클리어!");

            // 원한다면 BGM도 멈출 수 있음
            if (SoundManager.instance != null)
            {
                SoundManager.instance.StopBgm();
                // 또는 클리어 전용 BGM이 있다면 여기서 재생해도 됨.
                // SoundManager.instance.PlayStageBgm(클리어BgmIndex);
            }

            // Inspector에서 연결한 Game Clear 이벤트 실행 (UI 띄우기 등)
            if (onGameClear != null)
            {
                onGameClear.Invoke();
            }

            // 스테이지 매니저는 더 이상 필요 없으면 Update 멈추기
            // (원하면 주석 처리해도 됨)
            enabled = false;
        }
    }

    // ------------------------------ public Helper 함수들 ------------------------------

    /// <summary>
    /// 현재 스테이지 인덱스 반환 (0,1,2...)
    /// UI 등에 표시하고 싶을 때 사용 가능.
    /// </summary>
    public int GetCurrentStageIndex()
    {
        return currentStageIndex;
    }

    /// <summary>
    /// 현재 스테이지에서 지나간 시간(초)을 반환.
    /// </summary>
    public float GetCurrentStageElapsedTime()
    {
        return stageTimer;
    }

    /// <summary>
    /// 현재 스테이지에서 남은 시간(초)을 반환.
    /// 타이머 UI 등에 사용 가능.
    /// </summary>
    public float GetCurrentStageRemainTime()
    {
        float duration = GetCurrentStageDuration();
        return Mathf.Max(0f, duration - stageTimer);
    }
}

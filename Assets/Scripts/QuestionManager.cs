using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// [QuestionManager]
/// - "스폰"은 하지 않고, 스폰된 AnswerCube 세트에
///   곱셈 문제와 정답/오답 숫자를 배정하는 역할만 담당.
/// - 또한:
///     * 문제 텍스트 UI 표시
///     * 플레이어가 보기 큐브를 통과했을 때 정답/오답 판정
///     * 오답이면 PlayerEvent를 통해 데미지 처리
///     * 문제 종료 시 보기 큐브 / UI 정리
/// </summary>
public class QuestionManager : MonoBehaviour
{
    public static QuestionManager instance;

    [Header("문제 난이도 설정")]
    [Tooltip("곱셈 문제에서 사용할 최소 숫자 (예: 2단~9단)")]
    public int minNumber = 2;

    [Tooltip("곱셈 문제에서 사용할 최대 숫자")]
    public int maxNumber = 9;

    [Header("UI 설정")]
    [Tooltip("화면 상단에 문제를 보여줄 TextMeshProUGUI")]
    public TextMeshProUGUI questionText;

    [Tooltip("문제 텍스트를 보여줄 시간(초)")]
    public float questionShowTime = 5f;

    [Header("오답 시 데미지 설정")]
    [Tooltip("곱셈 문제를 틀렸을 때 PlayerEvent.OnWrongAnswer로 넘길 데미지 값")]
    public int wrongDamage = 2;

    // 현재 문제: a × b = answer
    private int currentA;
    private int currentB;
    private int currentAnswer;

    // 현재 활성화된 보기 큐브들 (SpawnManager가 전달해 준 세트)
    private readonly List<AnswerCube> activeCubes = new List<AnswerCube>();

    // 문제 텍스트 자동 숨김용 코루틴
    private Coroutine questionUICoroutine;

    // 현재 문제 진행 중인지 여부 (중복 문제 생성/세팅 방지)
    private bool questionActive = false;

    private void Awake()
    {
        // 싱글톤 패턴
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// SpawnManager에서 "이미 스폰해 놓은 AnswerCube 3개"를 넘겨주면,
    /// 이 함수가:
    ///  1) 곱셈 문제 생성
    ///  2) 정답 + 오답 2개 숫자 만들기
    ///  3) 세 큐브에 랜덤하게 숫자/정답 여부 배정
    ///  4) 문제 텍스트 UI 표시
    ///
    /// 예시:
    ///   var cubes = new List<AnswerCube>();
    ///   // SpawnManager에서 Instantiate 한 뒤...
    ///   QuestionManager.instance.SetupQuestion(cubes);
    /// </summary>
    public void SetupQuestion(IList<AnswerCube> cubes)
    {
        if (cubes == null || cubes.Count == 0)
        {
            Debug.LogWarning("[QuestionManager] SetupQuestion 호출됐지만 cubes가 비어 있습니다.");
            return;
        }

        if (questionActive)
        {
            Debug.Log("[QuestionManager] 이미 진행 중인 문제가 있어 새 문제 세팅을 하지 않습니다.");
            return;
        }

        questionActive = true;

        // 이전 문제 잔여 큐브 목록 정리
        activeCubes.Clear();

        // 1) 곱셈 문제 생성
        GenerateQuestion();

        // 2) 정답 + 오답 2개 생성
        int[] answers = GenerateAnswerOptions(currentAnswer);

        // 3) 순서 랜덤 셔플
        ShuffleArray(answers);

        // 4) 전달받은 큐브 수와 답안 배열의 최소 길이만큼만 사용
        int count = Mathf.Min(answers.Length, cubes.Count);

        for (int i = 0; i < count; i++)
        {
            AnswerCube cube = cubes[i];
            if (cube == null) continue;

            int value = answers[i];
            bool isCorrect = (value == currentAnswer);

            // AnswerCube 내부에 숫자와 정답 여부 세팅
            cube.Setup(value, isCorrect);

            activeCubes.Add(cube);
        }

        // 5) 문제 텍스트 UI 표시
        ShowQuestionUI();
    }

    /// <summary>
    /// 곱셈 문제 (a × b)를 생성하고 정답을 저장
    /// </summary>
    private void GenerateQuestion()
    {
        currentA = Random.Range(minNumber, maxNumber + 1);
        currentB = Random.Range(minNumber, maxNumber + 1);
        currentAnswer = currentA * currentB;
    }

    /// <summary>
    /// 정답 1개 + 오답 2개를 포함하는 배열을 생성
    /// </summary>
    private int[] GenerateAnswerOptions(int correctAnswer)
    {
        int[] result = new int[3];
        result[0] = correctAnswer;

        int idx = 1;
        while (idx < 3)
        {
            // 정답 주변의 랜덤 값 생성(-5 ~ +5)
            int offset = Random.Range(-5, 6);
            int candidate = correctAnswer + offset;

            if (candidate <= 0) continue;          // 0 이하는 제외
            if (candidate == correctAnswer) continue;

            // 중복 체크
            bool duplicate = false;
            for (int i = 0; i < idx; i++)
            {
                if (result[i] == candidate)
                {
                    duplicate = true;
                    break;
                }
            }
            if (duplicate) continue;

            result[idx] = candidate;
            idx++;
        }

        return result;
    }

    /// <summary>
    /// int 배열 셔플 (Fisher-Yates)
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

    /// <summary>
    /// Canvas 상단에 문제 텍스트 표시
    /// </summary>
    private void ShowQuestionUI()
    {
        if (questionText == null) return;

        questionText.text = $"{currentA} × {currentB} = ?";
        questionText.gameObject.SetActive(true);

        if (questionUICoroutine != null)
            StopCoroutine(questionUICoroutine);

        questionUICoroutine = StartCoroutine(Co_HideQuestionAfterDelay());
    }

    /// <summary>
    /// 일정 시간이 지나면 문제 텍스트를 숨긴다.
    /// (보기 큐브들은 플레이어가 통과할 때까지 남아 있음)
    /// </summary>
    private IEnumerator Co_HideQuestionAfterDelay()
    {
        yield return new WaitForSeconds(questionShowTime);

        if (questionText != null)
            questionText.gameObject.SetActive(false);
    }

    /// <summary>
    /// AnswerCube에서 플레이어와 충돌 시 호출:
    /// - 정답/오답 판정
    /// - 오답이면 PlayerEvent.OnWrongAnswer 호출
    /// - 문제/보기 큐브 정리
    /// </summary>
    public void ResolveAnswer(AnswerCube cube, PlayerEvent playerEvent)
    {
        if (!questionActive) return;

        bool correct = cube.isCorrect;

        if (correct)
        {
            Debug.Log("[QuestionManager] 정답!");
            // TODO: 정답 SFX, 점수 증가 등
        }
        else
        {
            Debug.Log("[QuestionManager] 오답!");
            if (playerEvent != null)
            {
                playerEvent.OnWrongAnswer(wrongDamage);
            }
        }

        EndQuestion();
    }

    /// <summary>
    /// 현재 문제를 종료하고, 보기 큐브/텍스트를 정리.
    /// AnswerCube 자체에서도 OnTriggerEnter에서 Destroy를 호출하므로,
    /// 여기서는 남아있는 큐브들만 정리해준다.
    /// </summary>
    private void EndQuestion()
    {
        questionActive = false;

        // 문제 텍스트 숨기기
        if (questionText != null)
            questionText.gameObject.SetActive(false);

        // 남아있는 보기 큐브 제거
        foreach (var cube in activeCubes)
        {
            if (cube != null)
                Destroy(cube.gameObject);
        }
        activeCubes.Clear();
    }
}

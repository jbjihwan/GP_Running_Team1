using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [SpawnManager]
/// - 플레이어는 고정이고, 월드(타일/큐브)가 움직이는 구조를 가정.
/// - 이 스크립트가 붙어 있는 위치(transform.position)를 기준으로
///   20초마다 AnswerCube 3개 세트를 스폰한다.
/// - 실제 문제(곱셈 연산)와 정답/오답 숫자 배정은 QuestionManager가 담당.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    [Header("프리팹 참조")]
    [Tooltip("텍스트가 붙어 있는 AnswerCube 프리팹")]
    public AnswerCube answerCubePrefab;

    [Header("라인 / 위치 설정")]
    [Tooltip("중앙(0)을 기준으로 좌우 라인의 X 거리\n예: 2 → -2,0,2 / 4 → -4,0,4")]
    public float laneOffset = 2f;

    [Tooltip("큐브가 생성될 Y 좌표 (바닥 높이에 맞게 설정)")]
    public float spawnY = 1f;

    [Header("스폰 간격 설정")]
    [Tooltip("문제 세트를 스폰하는 간격(초)")]
    public float spawnInterval = 20f;

    [Tooltip("게임 시작 직후 한 번 바로 스폰할지 여부")]
    public bool spawnOnStart = true;

    // 내부 타이머
    private float timer = 0f;

    private void Start()
    {
        // 시작하자마자 한 번 바로 스폰하고 싶으면
        if (spawnOnStart)
        {
            SpawnQuestionSetAtCurrentPosition();
        }

        // 타이머 초기화
        timer = 0f;
    }

    private void Update()
    {
        // 매 프레임마다 시간 누적
        timer += Time.deltaTime;

        // 일정 시간(Interval) 지났으면 문제 세트 스폰
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnQuestionSetAtCurrentPosition();
        }
    }

    /// <summary>
    /// 현재 SpawnManager의 위치(transform.position)를 기준으로
    /// AnswerCube 3개를 세트로 스폰하고,
    /// 그 세트를 QuestionManager에 넘겨서 문제/정답/오답을 설정하도록 한다.
    /// </summary>
    private void SpawnQuestionSetAtCurrentPosition()
    {
        if (answerCubePrefab == null)
        {
            Debug.LogError("[SpawnManager] answerCubePrefab이 비어 있습니다!");
            return;
        }

        if (QuestionManager.instance == null)
        {
            Debug.LogError("[SpawnManager] QuestionManager 인스턴스가 없습니다!");
            return;
        }

        // 1) 스폰 기준 Z좌표 = SpawnManager가 씬에서 서 있는 위치의 z
        float spawnZ = transform.position.z;

        // 2) AnswerCube 3개를 담을 리스트 준비
        List<AnswerCube> cubes = new List<AnswerCube>();

        // 3) X 좌표: -laneOffset, 0, +laneOffset
        float[] laneX = { -laneOffset, 0f, laneOffset };

        for (int i = 0; i < 3; i++)
        {
            Vector3 pos = new Vector3(laneX[i], spawnY, spawnZ);

            // AnswerCube 프리팹 인스턴스 생성
            AnswerCube cube = Instantiate(answerCubePrefab, pos, Quaternion.identity);

            cubes.Add(cube);
        }

        // 4) 생성된 3개 세트를 QuestionManager에게 넘겨서
        //    숫자/정답 여부/텍스트를 세팅하게 한다.
        QuestionManager.instance.SetupQuestion(cubes);
    }
}

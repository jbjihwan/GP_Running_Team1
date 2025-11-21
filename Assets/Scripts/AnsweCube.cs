using UnityEngine;
using TMPro;

public class AnswerCube : MonoBehaviour
{
    [Header("UI Text")]
    public TextMeshPro text;

    [Header("값 / 정답 여부")]
    public int answerValue;
    public bool isCorrect;

    [Header("이동 설정")]
    public float moveSpeed = 8f;     // 큐브가 플레이어 쪽으로 움직이는 속도
    public float destroyZ = -10f;    // 화면 뒤로 지나가면 삭제

    private bool used = false;

    private void Update()
    {
        // ● 큐브를 앞으로 이동 (-Z 방향으로)
        transform.position += Vector3.back * moveSpeed * Time.deltaTime;

        // ● 너무 뒤로 지나가면 자동 삭제
        if (transform.position.z < destroyZ)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// QuestionManager가 숫자와 정답 여부를 입력해줌
    /// </summary>
    public void Setup(int value, bool isCorrect)
    {
        this.answerValue = value;
        this.isCorrect = isCorrect;

        // 텍스트 표시
        if (text != null)
            text.text = value.ToString();
    }

    /// <summary>
    /// 플레이어가 큐브와 충돌했을 때 정답/오답 판정
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (used) return;
        if (!other.CompareTag("Player")) return;

        used = true;

        // PlayerEvent 가져오기
        PlayerEvent playerEvent = other.GetComponent<PlayerEvent>();
        if (playerEvent == null)
            playerEvent = other.GetComponentInParent<PlayerEvent>();

        // QuestionManager로 판정 전달
        if (QuestionManager.instance != null && playerEvent != null)
        {
            QuestionManager.instance.ResolveAnswer(this, playerEvent);
        }

        // 정답이든 오답이든 밟은 큐브는 바로 삭제
        Destroy(gameObject);
    }
}

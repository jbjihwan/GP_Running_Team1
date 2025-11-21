using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trap : MonoBehaviour
{
    [Header("이동 속도")]
    [Tooltip("트랩이 앞으로(자기 forward 방향 기준 반대) 움직이는 속도")]
    public float speed = 2f;

    [Header("대미지 설정")]
    [Tooltip("플레이어와 충돌했을 때 줄 데미지량")]
    public int contactDamage = 1;

    private void Update()
    {
        // 트랩이 계속 앞으로 이동 (맵이 -Z로 흐르는 구조라면 이 방향 맞춰 조정)
        transform.position -= transform.forward * speed * Time.deltaTime;
    }

    /*private void OnTriggerEnter(Collider other)
    {
        // 플레이어와 충돌했을 때만 처리
        if (!other.CompareTag("Player"))
            return;

         Player 컴포넌트 찾기 (자식에 붙어 있는 경우도 대비)
        Player player = other.GetComponent<Player>();
        if (player == null)
            player = other.GetComponentInParent<Player>();

        if (player != null)
        {
            // 플레이어 체력 깎기
            //player.TakeDamage(contactDamage);

            //  효과음 재생 (나중에 사용할 예정이면 인덱스 맞춰서 주석 풀면 됨)
            // if (SoundManager.instance != null)
            // {
            //     // 예: 4번 인덱스에 "함정 맞을 때" SFX를 넣어둔 경우
            //     SoundManager.instance.PlaySfx(4);
            // }
        }

        // 트랩은 플레이어에게 한 번 닿으면 바로 사라짐
        Destroy(gameObject);
    }*/
}
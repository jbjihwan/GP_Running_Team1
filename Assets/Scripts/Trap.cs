using UnityEngine;

public class Trap : MonoBehaviour
{
    [Header("이동 속도")]
    public float speed = 2f;

    [Header("대미지 설정")]
    public int contactDamage = 1;

    private void Update()
    {
        // 트랩 앞으로 이동
        transform.position -= transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // PlayerEvent 찾기
        PlayerEvent playerEvent = other.GetComponent<PlayerEvent>();
        if (playerEvent == null)
            playerEvent = other.GetComponentInParent<PlayerEvent>();

        if (playerEvent != null)
        {
            playerEvent.OnTrapHit(contactDamage);
        }

        // 트랩은 한 번 부딪히면 사라짐
        Destroy(gameObject);
    }
}

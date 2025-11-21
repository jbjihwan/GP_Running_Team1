using UnityEngine;

/// <summary>
/// [Tile]
/// - 타일 하나의 '실제 길이'를 계산해서 TileManager가 사용할 수 있게 해주는 스크립트.
/// - 프리팹의 크기/스케일이 바뀌어도 자동으로 Z 길이를 계산해서 tileLength에 저장.
/// </summary>
public class Tile : MonoBehaviour
{
    [Tooltip("이 타일의 실제 Z 길이 (자동 계산됨)")]
    public float tileLength = 10f;

    private void Awake()
    {
        // 이 오브젝트와 자식들에 있는 Renderer들을 전부 찾는다.
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            // 첫 번째 Renderer 기준으로 Bounds 시작
            Bounds bounds = renderers[0].bounds;

            // 나머지 Renderer들의 Bounds를 모두 합치기
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            // 최종 Bounds의 Z 방향 길이 = 타일 하나의 실제 길이
            tileLength = bounds.size.z;
        }
    }

    /// <summary>
    /// 타일 하나의 Z 길이 리턴
    /// </summary>
    public float GetLength()
    {
        return tileLength;
    }
}
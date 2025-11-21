using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [TileManager]
/// - 플레이어 앞에 타일들을 이어 붙여서 "무한 맵"처럼 보이게 만드는 스크립트.
/// - 타일 프리팹 배열(tilePrefabs)을 1,2,3,4 순서대로 배치하고,
///   맨 뒤로 사라진 타일을 맨 앞으로 보내서 계속 반복.
/// 
/// 전제 조건:
/// - 각 타일 프리팹에는 반드시 `Tile` 스크립트가 붙어 있어야 하고,
///   거기서 `GetLength()`로 타일 하나의 길이를 돌려줘야 함.
/// 
/// 사용 예:
///   1) 빈 GameObject 만들고 TileManager 붙이기
///   2) player에 Player Transform 드래그
///   3) tilePrefabs에 [0]=Tile1, [1]=Tile2, [2]=Tile3, [3]=Tile4 넣기
///   4) startTileCount = 4 로 두면 1,2,3,4 순서로 시작해서 계속 반복
/// </summary>
public class TileManager : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("타일 기준이 될 플레이어 Transform (플레이어 위치를 기준으로 리사이클 거리 계산)")]
    public Transform player;

    [Header("타일 설정")]
    [Tooltip("순서대로 반복할 타일 프리팹들 (0→1→2→...→0→1→2... 반복)")]
    public Tile[] tilePrefabs;        // 타일 프리팹들
    [Tooltip("처음에 한 줄로 깔아둘 타일 개수.\ntilePrefabs.Length와 같게 두면 1세트가 한 번씩 다 나옴.")]
    public int startTileCount = 4;    // 보통 tilePrefabs.Length와 맞추는 걸 추천

    // 타일 하나의 길이 (Z방향). 첫 번째 타일 생성 시 GetLength()로 자동 세팅.
    private float tileLength = 10f;

    [Header("움직임 설정")]
    [Tooltip("타일이 -Z 방향으로 이동하는 속도")]
    public float moveSpeed = 5f;
    [Tooltip("플레이어 뒤로 이 정도 거리만큼 벗어난 타일은 맨 앞으로 재배치")]
    public float recycleDistance = 10f;

    // 현재 씬에서 활성화된(화면에 깔려있는) 타일들
    private readonly List<Tile> activeTiles = new List<Tile>();

    // 다음에 배치할 타일 프리팹 인덱스 (0→1→2→...→0 순환)
    private int nextIndex = 0;

    private void Start()
    {
        // 필수 참조 체크
        if (player == null)
        {
            Debug.LogError("TileManager: Player가 설정되지 않음!");
            return;
        }

        if (tilePrefabs == null || tilePrefabs.Length == 0)
        {
            Debug.LogError("TileManager: tilePrefabs 비어 있음!");
            return;
        }

        // 플레이어 위치 기준으로 앞쪽부터 타일을 배치하기 위한 시작 Z값
        float spawnZ = player.position.z;

        // -------------------------
        // 1) 첫 번째 타일 생성
        // -------------------------
        // 일단 0번 타일을 첫 타일로 사용
        Tile firstTile = CreateTile(tilePrefabs[0], spawnZ);

        // Tile 스크립트에서 계산한 타일 길이 가져오기
        tileLength = firstTile.GetLength();

        // 활성 타일 목록에 추가
        activeTiles.Add(firstTile);

        // 다음 타일은 타일 길이만큼 앞쪽에 배치
        spawnZ += tileLength;

        // 다음 인덱스는 1번부터 시작
        nextIndex = 1;

        // -------------------------
        // 2) 나머지 타일들 순서대로 생성
        // -------------------------
        for (int i = 1; i < startTileCount; i++)
        {
            // 현재 nextIndex에 해당하는 프리팹으로 타일 생성
            Tile newTile = CreateTile(tilePrefabs[nextIndex], spawnZ);

            activeTiles.Add(newTile);
            spawnZ += tileLength;

            // nextIndex는 0~(tilePrefabs.Length-1) 범위 내에서 순환
            nextIndex = (nextIndex + 1) % tilePrefabs.Length;
        }

        // recycleDistance가 0 이하로 설정되어 있으면 자동으로 타일 길이로 세팅
        if (recycleDistance <= 0f)
            recycleDistance = tileLength;
    }

    private void Update()
    {
        MoveTiles();
        RecycleTilesIfNeeded();
    }

    /// <summary>
    /// 주어진 프리팹을 zPos 위치에 생성하는 함수.
    /// x,y는 0으로 고정, z만 바꿔서 줄 세우는 형태.
    /// </summary>
    private Tile CreateTile(Tile prefab, float zPos)
    {
        Vector3 pos = new Vector3(0f, 0f, zPos);
        return Instantiate(prefab, pos, Quaternion.identity, transform);
    }

    /// <summary>
    /// 모든 타일들을 -Z 방향으로 이동시킴.
    /// → 실제로는 플레이어가 앞으로 가는 게 아니라, 맵이 뒤로 흘러가는 느낌.
    /// </summary>
    private void MoveTiles()
    {
        Vector3 move = Vector3.back * moveSpeed * Time.deltaTime;

        for (int i = 0; i < activeTiles.Count; i++)
        {
            activeTiles[i].transform.position += move;
        }
    }

    /// <summary>
    /// 플레이어 뒤로 너무 벗어난 타일을 맨 앞으로 재배치하여 "무한 맵"처럼 보이게 하는 함수.
    /// </summary>
    private void RecycleTilesIfNeeded()
    {
        if (activeTiles.Count == 0) return;

        // 리스트 맨 앞에 있는 타일 = 현재 가장 뒤쪽에 있는 타일이라고 가정
        Tile firstTile = activeTiles[0];
        Tile lastTile = activeTiles[activeTiles.Count - 1];

        // '플레이어 기준으로 얼마나 뒤로 가야 재활용할지' 기준 Z 값 계산
        float recycleZ = player.position.z - recycleDistance;

        // firstTile의 Z 위치가 이 기준보다 더 뒤로 갔으면 재배치
        if (firstTile.transform.position.z < recycleZ)
        {
            // 새로운 Z 위치 = 마지막 타일의 Z + 타일 길이
            float newZ = lastTile.transform.position.z + tileLength;

            // firstTile의 위치를 맨 앞으로 옮김
            Vector3 pos = firstTile.transform.position;
            pos.z = newZ;
            firstTile.transform.position = pos;

            // 리스트에서도 맨 앞에서 빼서 맨 뒤로 넣기 → 순서 유지 (1,2,3,4→2,3,4,1...)
            activeTiles.RemoveAt(0);
            activeTiles.Add(firstTile);
        }
    }
}

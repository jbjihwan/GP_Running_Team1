using UnityEngine;

/// <summary>
/// [SoundManager]
/// - BGM(배경음악) + SFX(효과음)를 한 곳에서 관리하는 싱글톤 매니저.
/// - "인덱스 방식"으로 SFX/BGM 재생.
/// - BGM / SFX 각각 **마스터 볼륨**이 있고,
///   그 안에서 **클립마다 개별 볼륨(localVolume)** 도 따로 조절 가능.
///   
/// 사용 예)
///   // SFX 재생
///   SoundManager.instance.PlaySfx(3);          // sfxList[3] 재생
///
///   // 스테이지 BGM 재생
///   SoundManager.instance.PlayStageBgm(1);     // stageBgms[1] 재생
///
///   // UI Slider와 연동해서 마스터 볼륨 개별 조절
///   //  - BGM 슬라이더 → SoundManager.instance.SetBgmVolume(value);
///   //  - SFX 슬라이더 → SoundManager.instance.SetSfxVolume(value);
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    /*======================================================
     * 데이터 구조 정의 (클립 + 개별 볼륨)
     *======================================================*/

    [System.Serializable]
    public class StageBgmEntry
    {
        [Tooltip("이 스테이지에서 재생할 BGM 클립")]
        public AudioClip clip;

        [Range(0f, 1f)]
        [Tooltip("이 BGM만의 개별 볼륨 (마스터 BGM 볼륨과 곱해집니다)")]
        public float localVolume = 1f;
    }

    [System.Serializable]
    public class SfxEntry
    {
        [Tooltip("이 SFX의 오디오 클립")]
        public AudioClip clip;

        [Range(0f, 1f)]
        [Tooltip("이 효과음만의 개별 볼륨 (마스터 SFX 볼륨과 곱해집니다)")]
        public float localVolume = 1f;
    }


    /*======================================================
     * BGM (스테이지별)
     *======================================================*/

    [Header("# BGM (스테이지별)")]
    [Tooltip("스테이지 BGM 리스트 (클립 + 이 BGM만의 볼륨)")]
    public StageBgmEntry[] stageBgms;

    [Range(0f, 1f)]
    [Tooltip("BGM 마스터 볼륨 (0=무음, 1=최대)")]
    public float bgmVolume = 0.7f;     // BGM 전체(마스터) 볼륨

    private AudioSource bgmPlayer;
    private int currentBgmIndex = -1;  // 현재 재생중인 BGM 인덱스


    /*======================================================
     * SFX (효과음)
     *======================================================*/

    [Header("# SFX (효과음)")]
    [Tooltip("SFX 리스트 (클립 + 이 효과음만의 볼륨)")]
    public SfxEntry[] sfxList;

    [Range(0f, 1f)]
    [Tooltip("SFX 마스터 볼륨 (0=무음, 1=최대)")]
    public float sfxVolume = 1f;       // SFX 전체(마스터) 볼륨

    [Tooltip("동시에 재생 가능한 SFX 채널 수")]
    public int sfxChannels = 8; 
    // sfxChannels(SFX 채널 수)의 의미는 “동시에 몇 개의 효과음을 재생할 수 있는가”
    // 를 결정하는 오디오 소스 풀(POOL)의 개수

    private AudioSource[] sfxPlayers;
    private int sfxIndex = 0;
    private float lastSfxMasterVolume = 1f; // 슬라이더로 볼륨 조정 시 비율 맞추려고 사용


    /*======================================================
     * 초기화 및 싱글톤 설정
     *======================================================*/

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            Init();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 내부적으로 사용할 AudioSource들을 모두 생성하는 함수.
    /// - BGM용 AudioSource 1개
    /// - SFX용 AudioSource 여러 개 (sfxChannels 개수만큼)
    /// </summary>
    void Init()
    {
        // -------------------------
        // BGM 플레이어 생성
        // -------------------------
        GameObject bgmObj = new GameObject("BgmPlayer");
        bgmObj.transform.parent = transform;

        bgmPlayer = bgmObj.AddComponent<AudioSource>();
        bgmPlayer.playOnAwake = false;
        bgmPlayer.loop = true;
        bgmPlayer.volume = bgmVolume; // 개별 BGM 볼륨과 곱해서 사용 예정

        // -------------------------
        // SFX 플레이어 풀 생성
        // -------------------------
        GameObject sfxObj = new GameObject("SfxPlayers");
        sfxObj.transform.parent = transform;

        sfxPlayers = new AudioSource[sfxChannels];

        for (int i = 0; i < sfxChannels; i++)
        {
            AudioSource src = sfxObj.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.volume = sfxVolume; // 실제 재생 시 마스터 * 개별 볼륨으로 다시 세팅

            sfxPlayers[i] = src;
        }

        lastSfxMasterVolume = sfxVolume;
    }


    /*======================================================
     * BGM 메서드 (스테이지 번호 기반)
     *======================================================*/

    /// <summary>
    /// 스테이지 번호에 맞는 BGM을 재생.
    /// stageBgms[stageIndex] 의 clip + localVolume 사용.
    /// StageManager에서 스테이지 변경 시 자동 호출하도록 구성하면 됨.
    ///
    ///     SoundManager.instance.PlayStageBgm(스테이지번호);
    /// </summary>
    public void PlayStageBgm(int stageIndex)
    {
        if (stageBgms == null || stageBgms.Length == 0) return;

        // 인덱스를 안전한 범위로 제한
        int idx = Mathf.Clamp(stageIndex, 0, stageBgms.Length - 1);
        StageBgmEntry entry = stageBgms[idx];

        if (entry == null || entry.clip == null) return;

        // 이미 같은 BGM이 재생 중이면 다시 틀 필요 없음
        if (currentBgmIndex == idx && bgmPlayer.clip == entry.clip && bgmPlayer.isPlaying)
            return;

        currentBgmIndex = idx;

        bgmPlayer.Stop();
        bgmPlayer.clip = entry.clip;

        // 마스터 BGM 볼륨 * 이 BGM만의 개별 볼륨
        bgmPlayer.volume = bgmVolume * entry.localVolume;
        bgmPlayer.Play();
    }

    /// <summary>
    /// 현재 재생 중인 BGM을 정지.
    /// </summary>
    public void StopBgm()
    {
        if (bgmPlayer != null)
            bgmPlayer.Stop();
    }

    /// <summary>
    /// BGM 마스터 볼륨 설정 (0~1).
    /// - UI Slider와 연결해서 사용 가능.
    /// - 현재 재생중인 BGM이 있다면, 그 BGM의 개별 볼륨(localVolume)과 곱해서 즉시 반영.
    /// </summary>
    public void SetBgmVolume(float v)
    {
        bgmVolume = Mathf.Clamp01(v);

        if (bgmPlayer == null) return;

        if (currentBgmIndex >= 0 &&
            stageBgms != null &&
            currentBgmIndex < stageBgms.Length &&
            stageBgms[currentBgmIndex] != null)
        {
            float local = stageBgms[currentBgmIndex].localVolume;
            bgmPlayer.volume = bgmVolume * local;
        }
        else
        {
            // 혹시 현재 곡 인덱스를 모를 경우, 마스터만 먼저 반영
            bgmPlayer.volume = bgmVolume;
        }
    }


    /*======================================================
     * SFX 메서드 (인덱스 방식, 클립별 개별 볼륨)
     *======================================================*/

    /// <summary>
    /// SFX 재생 (인덱스로 호출)
    /// 사용 예)
    ///     SoundManager.instance.PlaySfx(0);  // sfxList[0]
    ///     SoundManager.instance.PlaySfx(3);  // sfxList[3]
    ///
    /// 최종 볼륨 = SFX 마스터 볼륨 * 해당 SFX의 localVolume
    /// </summary>
    public void PlaySfx(int index)
    {
        if (sfxList == null || index < 0 || index >= sfxList.Length) return;

        SfxEntry entry = sfxList[index];
        if (entry == null || entry.clip == null) return;

        float finalVolume = sfxVolume * entry.localVolume;

        // 빈 오디오 채널 찾기
        for (int i = 0; i < sfxChannels; i++)
        {
            int loopIndex = (sfxIndex + i) % sfxChannels;

            if (!sfxPlayers[loopIndex].isPlaying)
            {
                sfxPlayers[loopIndex].clip = entry.clip;
                sfxPlayers[loopIndex].volume = finalVolume;
                sfxPlayers[loopIndex].Play();

                sfxIndex = loopIndex;
                return;
            }
        }

        // 모든 채널이 재생 중이면 기본 채널(0번)에 덮어쓰기
        sfxPlayers[0].clip = entry.clip;
        sfxPlayers[0].volume = finalVolume;
        sfxPlayers[0].Play();
    }

    /// <summary>
    /// SFX 마스터 볼륨 설정 (0~1).
    /// - UI Slider와 연결해서 사용 가능.
    /// - 현재 재생중인 채널들의 볼륨도 비율에 맞춰서 같이 조정.
    /// </summary>
    public void SetSfxVolume(float v)
    {
        float old = sfxVolume;
        sfxVolume = Mathf.Clamp01(v);

        if (sfxPlayers == null) return;

        // 이전 마스터 볼륨에 비해 몇 배 바뀌었는지 비율 계산
        if (old > 0f)
        {
            float factor = sfxVolume / old;

            foreach (var src in sfxPlayers)
            {
                if (src != null)
                    src.volume *= factor; // 현재 재생중인 소리도 비율에 따라 조정
            }
        }
        else
        {
            // 이전 볼륨이 0이었으면, 새 마스터 볼륨값으로 통일
            foreach (var src in sfxPlayers)
            {
                if (src != null)
                    src.volume = sfxVolume;
            }
        }

        lastSfxMasterVolume = sfxVolume;
    }
}

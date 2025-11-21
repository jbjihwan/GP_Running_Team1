using UnityEngine;

/// <summary>
/// [SoundManager]
/// - BGM(배경음악) + SFX(효과음)를 한 곳에서 관리하는 싱글톤 매니저.
/// -  "인덱스 방식"으로 SFX를 재생.
///   
/// 사용 예)
///   SoundManager.instance.PlaySfx(3);     // sfxClips[3] 재생
///   SoundManager.instance.PlayStageBgm(1); // 스테이지 2번 BGM 재생
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    /*======================================================
     * BGM (스테이지별)
     *======================================================*/

    [Header("# BGM (스테이지별)")]
    [Tooltip("스테이지 번호에 맞는 BGM.\n예) 0=1스테이지, 1=2스테이지, 2=3스테이지")]
    public AudioClip[] stageBgmClips;

    [Range(0f, 1f)]
    [Tooltip("BGM 전체 볼륨")]
    public float bgmVolume = 0.7f;

    private AudioSource bgmPlayer;


    /*======================================================
     * SFX (효과음)
     *======================================================*/

    [Header("# SFX (효과음)")]
    [Tooltip("효과음 클립 배열. 인덱스 기반으로 사용합니다.")]
    public AudioClip[] sfxClips;

    [Range(0f, 1f)]
    [Tooltip("SFX 전체 볼륨")]
    public float sfxVolume = 1f;

    [Tooltip("동시에 재생 가능한 SFX 채널 수")]
    public int sfxChannels = 8;

    private AudioSource[] sfxPlayers;
    private int sfxIndex = 0;


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
        bgmPlayer.volume = bgmVolume;

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
            src.volume = sfxVolume;

            sfxPlayers[i] = src;
        }
    }


    /*======================================================
     * BGM 메서드 (스테이지 번호 기반)
     *======================================================*/

    /// <summary>
    /// 스테이지 번호에 맞는 BGM을 재생.
    /// StageManager에서 스테이지 변경 시 자동 호출하도록 구성하면 됨.
    ///
    ///     SoundManager.instance.PlayStageBgm(스테이지번호);
    /// </summary>
    public void PlayStageBgm(int stageIndex)
    {
        if (stageBgmClips == null || stageBgmClips.Length == 0) return;

        // 인덱스를 안전한 범위로 제한
        int idx = Mathf.Clamp(stageIndex, 0, stageBgmClips.Length - 1);
        AudioClip clip = stageBgmClips[idx];

        if (clip == null) return;

        // 이미 같은 BGM이 재생 중이면 다시 틀 필요 없음
        if (bgmPlayer.clip == clip && bgmPlayer.isPlaying)
            return;

        bgmPlayer.Stop();
        bgmPlayer.clip = clip;
        bgmPlayer.volume = bgmVolume;
        bgmPlayer.Play();
    }

    public void StopBgm()
    {
        bgmPlayer.Stop();
    }

    public void SetBgmVolume(float v)
    {
        bgmVolume = Mathf.Clamp01(v);
        bgmPlayer.volume = bgmVolume;
    }


    /*======================================================
     * SFX 메서드 (인덱스 방식)
     *======================================================*/

    /// <summary>
    /// SFX 재생 (인덱스로 호출)
    /// 사용 예)
    ///     SoundManager.instance.PlaySfx(0);  // sfxClips[0]
    ///     SoundManager.instance.PlaySfx(3);  // sfxClips[3]
    /// </summary>
    public void PlaySfx(int index)
    {
        if (sfxClips == null || index < 0 || index >= sfxClips.Length) return;

        AudioClip clip = sfxClips[index];
        if (clip == null) return;

        // 빈 오디오 채널 찾기
        for (int i = 0; i < sfxChannels; i++)
        {
            int loopIndex = (sfxIndex + i) % sfxChannels;

            if (!sfxPlayers[loopIndex].isPlaying)
            {
                sfxPlayers[loopIndex].clip = clip;
                sfxPlayers[loopIndex].volume = sfxVolume;
                sfxPlayers[loopIndex].Play();

                sfxIndex = loopIndex;
                return;
            }
        }

        // 모든 채널이 재생 중이면 기본 채널(0번)에 덮어쓰기
        sfxPlayers[0].clip = clip;
        sfxPlayers[0].volume = sfxVolume;
        sfxPlayers[0].Play();
    }

    public void SetSfxVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);

        // 모든 채널에 적용
        foreach (var src in sfxPlayers)
            src.volume = sfxVolume;
    }
}

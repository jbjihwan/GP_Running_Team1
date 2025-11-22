using UnityEngine;

/// <summary>
/// [SoundManager]
/// - Singleton manager handling both BGM (background music) and SFX (sound effects).
/// - Uses index-based playback for both BGM and SFX.
/// - Each category has:
///   - A master volume (BGM / SFX)
///   - A local volume per clip (individual volume)
///
/// Example:
///     // Play SFX
///     SoundManager.instance.PlaySfx(3);            // Plays sfxList[3]
///
///     // Play Stage BGM
///     SoundManager.instance.PlayStageBgm(1);       // Plays stageBgms[1]
///
///     // UI Slider example
///     SoundManager.instance.SetBgmVolume(value);   // BGM master volume
///     SoundManager.instance.SetSfxVolume(value);   // SFX master volume
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    /*======================================================
     * Data Structure (Clip + Local Volume)
     *======================================================*/

    [System.Serializable]
    public class StageBgmEntry
    {
        [Tooltip("BGM clip for this stage")]
        public AudioClip clip;

        [Range(0f, 1f)]
        [Tooltip("Individual volume for this BGM (multiplied by master BGM volume)")]
        public float localVolume = 1f;
    }

    [System.Serializable]
    public class SfxEntry
    {
        [Tooltip("Audio clip for this SFX")]
        public AudioClip clip;

        [Range(0f, 1f)]
        [Tooltip("Individual volume for this SFX (multiplied by master SFX volume)")]
        public float localVolume = 1f;
    }


    /*======================================================
     * BGM (Per Stage)
     *======================================================*/

    [Header("# BGM (Stage-based)")]
    [Tooltip("List of stage BGMs (Clip + Individual Volume)")]
    public StageBgmEntry[] stageBgms;

    [Range(0f, 1f)]
    [Tooltip("Master BGM Volume (0 = mute, 1 = max)")]
    public float bgmVolume = 0.7f;

    private AudioSource bgmPlayer;
    private int currentBgmIndex = -1;


    /*======================================================
     * SFX (Sound Effects)
     *======================================================*/

    [Header("# SFX (Sound Effects)")]
    [Tooltip("List of SFX entries (Clip + Individual Volume)")]
    public SfxEntry[] sfxList;

    [Range(0f, 1f)]
    [Tooltip("Master SFX Volume (0 = mute, 1 = max)")]
    public float sfxVolume = 1f;

    [Tooltip("Number of simultaneous SFX channels allowed")]
    public int sfxChannels = 8;
    // Meaning:
    // sfxChannels = How many SFX can be played at the same time (AudioSource pool size)

    private AudioSource[] sfxPlayers;
    private int sfxIndex = 0;
    private float lastSfxMasterVolume = 1f;


    /*======================================================
     * Initialization and Singleton Setup
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
    /// Creates AudioSources used internally:
    /// - One AudioSource for BGM
    /// - Multiple AudioSources for SFX (pool)
    /// </summary>
    void Init()
    {
        // -------------------------
        // Create BGM Player
        // -------------------------
        GameObject bgmObj = new GameObject("BgmPlayer");
        bgmObj.transform.parent = transform;

        bgmPlayer = bgmObj.AddComponent<AudioSource>();
        bgmPlayer.playOnAwake = false;
        bgmPlayer.loop = true;
        bgmPlayer.volume = bgmVolume;

        // -------------------------
        // Create SFX Player Pool
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

        lastSfxMasterVolume = sfxVolume;
    }


    /*======================================================
     * BGM Methods (Stage-based)
     *======================================================*/

    /// <summary>
    /// Plays the BGM for the given stage index.
    /// Uses stageBgms[index].clip and its localVolume.
    ///
    /// Example:
    ///     SoundManager.instance.PlayStageBgm(stageIndex);
    /// </summary>
    public void PlayStageBgm(int stageIndex)
    {
        if (stageBgms == null || stageBgms.Length == 0) return;

        int idx = Mathf.Clamp(stageIndex, 0, stageBgms.Length - 1);
        StageBgmEntry entry = stageBgms[idx];

        if (entry == null || entry.clip == null) return;

        if (currentBgmIndex == idx &&
            bgmPlayer.clip == entry.clip &&
            bgmPlayer.isPlaying)
            return;

        currentBgmIndex = idx;

        bgmPlayer.Stop();
        bgmPlayer.clip = entry.clip;
        bgmPlayer.volume = bgmVolume * entry.localVolume;
        bgmPlayer.Play();
    }

    /// <summary>
    /// Stops currently playing BGM.
    /// </summary>
    public void StopBgm()
    {
        if (bgmPlayer != null)
            bgmPlayer.Stop();
    }

    /// <summary>
    /// Sets the master BGM volume (0 to 1).
    /// Updates currently playing BGM immediately.
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
            bgmPlayer.volume = bgmVolume;
        }
    }


    /*======================================================
     * SFX Methods (Index-based, with individual volume)
     *======================================================*/

    /// <summary>
    /// Plays an SFX by index.
    ///
    /// Example:
    ///     SoundManager.instance.PlaySfx(0);
    ///     SoundManager.instance.PlaySfx(3);
    ///
    /// Final volume = SFX master volume * SFX local volume
    /// </summary>
    public void PlaySfx(int index)
    {
        if (sfxList == null || index < 0 || index >= sfxList.Length) return;

        SfxEntry entry = sfxList[index];
        if (entry == null || entry.clip == null) return;

        float finalVolume = sfxVolume * entry.localVolume;

        // Find an available SFX channel
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

        // If all channels are busy, overwrite channel 0
        sfxPlayers[0].clip = entry.clip;
        sfxPlayers[0].volume = finalVolume;
        sfxPlayers[0].Play();
    }

    /// <summary>
    /// Sets the master SFX volume (0 to 1).
    /// Adjusts currently playing SFX proportionally.
    /// </summary>
    public void SetSfxVolume(float v)
    {
        float old = sfxVolume;
        sfxVolume = Mathf.Clamp01(v);

        if (sfxPlayers == null) return;

        // If old volume > 0, scale currently playing volumes
        if (old > 0f)
        {
            float factor = sfxVolume / old;

            foreach (var src in sfxPlayers)
            {
                if (src != null)
                    src.volume *= factor;
            }
        }
        else
        {
            // If previous volume was 0, set all volumes to the new value
            foreach (var src in sfxPlayers)
            {
                if (src != null)
                    src.volume = sfxVolume;
            }
        }

        lastSfxMasterVolume = sfxVolume;
    }
}

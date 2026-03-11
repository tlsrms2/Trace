using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("BGM Clips")]
    [SerializeField] private AudioClip titleBgm;
    [SerializeField] private AudioClip ingameBgm;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip buttonClickSfx;
    [SerializeField] private AudioClip laserPlaceSfx;
    [SerializeField] private AudioClip laserChargeSfx;
    [SerializeField] private AudioClip playerDeathSfx;
    [SerializeField] private AudioClip enemyDeathSfx;
    [SerializeField] private AudioClip bossAppearSfx;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #region BGM
    public void PlayBgm(AudioClip clip)
    {
        if (bgmSource.clip == clip) return; // 같은 BGM이면 재생하지 않음
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void PlayTitleBgm() => PlayBgm(titleBgm);
    public void PlayIngameBgm() => PlayBgm(ingameBgm);
    #endregion

    #region SFX
    public void PlaySfx(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }

    public void PlayButtonClick() => PlaySfx(buttonClickSfx);
    public void PlayLaserPlace() => PlaySfx(laserPlaceSfx);
    public void PlayLaserCharge() => PlaySfx(laserChargeSfx);
    public void PlayPlayerDeath() => PlaySfx(playerDeathSfx);
    public void PlayEnemyDeath() => PlaySfx(enemyDeathSfx);
    public void PlayBossAppear() => PlaySfx(bossAppearSfx);
    #endregion
}
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    // ==================== Constants ====================
    private const float DefaultSoundBaseVolume = 1f;
    private const float DefaultMusicBaseVolume = 0.3f;
    private const float VolumeMin = 0f;
    private const float VolumeMax = 1f;

    // ==================== Singleton ====================
    public static SoundManager instance { get; private set; }

    // ==================== Private Fields ====================
    private AudioSource _soundSource;
    private AudioSource _musicSource;

    // ==================== Serialized Fields ====================
    [SerializeField] public AudioClip menuMusic;  // Serialized for Inspector
    [SerializeField] public AudioClip level1Music; // Serialized for Inspector

    // ==================== Unity Lifecycle ====================
    private void Awake()
    {
        _soundSource = GetComponent<AudioSource>();
        _musicSource = transform.GetChild(0).GetComponent<AudioSource>();
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
        ChangeMusicVolume(0);
        ChangeSoundVolume(0);
    }

    // ==================== Public Methods ====================
    public void PlaySound(AudioClip sound, GameObject caller)
    {
        if (IsObjectVisible(caller))
        {
            _soundSource.PlayOneShot(sound);
        }
    }

    public void ChangeSoundVolume(float change)
    {
        ChangeSourceVolume(DefaultSoundBaseVolume, "soundVolume", change, _soundSource);
    }
    public void ChangeMusicVolume(float change)
    {
        ChangeSourceVolume(DefaultMusicBaseVolume, "musicVolume", change, _musicSource);
    }
    public float GetCurrentMusicVolume()
    {
        return PlayerPrefs.GetFloat("musicVolume", VolumeMax);
    }
    public float GetCurrentSoundVolume()
    {
        return PlayerPrefs.GetFloat("soundVolume", VolumeMax);
    }
    public void ChangeMusic(AudioClip newClip)
    {
        if (_musicSource.clip == newClip) return; // Prevent restarting same music
        _musicSource.clip = newClip;
        _musicSource.Play();
    }

    // ==================== Private Methods ====================
    private bool IsObjectVisible(GameObject obj)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return false;
        SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.isVisible)
        {
            return true;
        }
        SpriteRenderer[] childRenderers = obj.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer childRenderer in childRenderers)
        {
            if (childRenderer.isVisible)
            {
                return true;
            }
        }
        return false;
    }

    private void ChangeSourceVolume(float baseVolume, string volumeName, float change, AudioSource source)
    {
        float currentVolume = PlayerPrefs.GetFloat(volumeName, VolumeMax);
        currentVolume += change;
        // Ensure it wraps around between min and max
        if (currentVolume > VolumeMax)
        {
            currentVolume = VolumeMin;
        }
        else if (currentVolume < VolumeMin)
        {
            currentVolume = VolumeMax;
        }
        source.volume = currentVolume * baseVolume;
        PlayerPrefs.SetFloat(volumeName, currentVolume);
    }
}

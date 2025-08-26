using UnityEngine;

/// <summary>
/// Manages all sound and music playback, volume control, and music switching for the game.
/// Implements a singleton pattern for global access.
/// </summary>
public class SoundManager : MonoBehaviour
{
    // ==================== Constants ====================
    private const float DefaultSoundBaseVolume = 1f;
    private const float DefaultMusicBaseVolume = 0.3f;
    private const float VolumeMin = 0f;
    private const float VolumeMax = 1f;

    // ==================== Singleton ====================
    /// <summary>
    /// The global instance of the SoundManager.
    /// </summary>
    public static SoundManager instance { get; private set; }

    // ==================== Private Fields ====================
    private AudioSource _soundSource;
    private AudioSource _musicSource;

    // ==================== Serialized Fields ====================
    [Header("Music Clips")]
    [Tooltip("Audio clip for the menu background music.")]
    [SerializeField] private AudioClip menuMusic;
    [Tooltip("Audio clip for the Level 1 background music.")]
    [SerializeField] private AudioClip levelMusic;
    [SerializeField] private bool isSoundLocal = true;

    /// <summary>
    /// Gets the menu music audio clip.
    /// </summary>
    public AudioClip MenuMusic => menuMusic;
    /// <summary>
    /// Gets the Level 1 music audio clip.
    /// </summary>
    public AudioClip levelMusic => levelMusic;

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
        // Initialize volumes to saved values or defaults
        ChangeMusicVolume(0);
        ChangeSoundVolume(0);
    }

    // ==================== Public Methods ====================
    /// <summary>
    /// Plays a sound effect if the calling object is visible on screen.
    /// </summary>
    /// <param name="sound">The audio clip to play.</param>
    /// <param name="caller">The GameObject requesting the sound.</param>
    public void PlaySound(AudioClip sound, GameObject caller)
    {
        if (!isSoundLocal)
        {
            _soundSource.PlayOneShot(sound);
        }
        else
        {
            if (IsObjectVisible(caller))
            {
                _soundSource.PlayOneShot(sound);
            }
        }

    }

    /// <summary>
    /// Changes the sound effects volume by a specified amount.
    /// </summary>
    /// <param name="change">The amount to change the volume by.</param>
    public void ChangeSoundVolume(float change)
    {
        ChangeSourceVolume(DefaultSoundBaseVolume, "soundVolume", change, _soundSource);
    }

    /// <summary>
    /// Changes the music volume by a specified amount.
    /// </summary>
    /// <param name="change">The amount to change the volume by.</param>
    public void ChangeMusicVolume(float change)
    {
        ChangeSourceVolume(DefaultMusicBaseVolume, "musicVolume", change, _musicSource);
    }

    /// <summary>
    /// Gets the current music volume from PlayerPrefs.
    /// </summary>
    public float GetCurrentMusicVolume()
    {
        return PlayerPrefs.GetFloat("musicVolume", VolumeMax);
    }

    /// <summary>
    /// Gets the current sound effects volume from PlayerPrefs.
    /// </summary>
    public float GetCurrentSoundVolume()
    {
        return PlayerPrefs.GetFloat("soundVolume", VolumeMax);
    }

    /// <summary>
    /// Changes the currently playing music to a new audio clip.
    /// </summary>
    /// <param name="newClip">The new music audio clip.</param>
    public void ChangeMusic(AudioClip newClip)
    {
        if (_musicSource.clip == newClip) return; // Prevent restarting same music
        _musicSource.clip = newClip;
        _musicSource.Play();
    }

    // ==================== Private Methods ====================
    /// <summary>
    /// Determines if the given GameObject or any of its children are visible by the main camera.
    /// </summary>
    /// <param name="obj">The GameObject to check.</param>
    /// <returns>True if visible, false otherwise.</returns>
    private bool IsObjectVisible(GameObject obj)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return false;
        SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
        // Maybe first frame explison isn't visable!
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

    /// <summary>
    /// Changes the volume of a given AudioSource, wrapping between min and max, and saves the value in PlayerPrefs.
    /// </summary>
    /// <param name="baseVolume">The base volume multiplier.</param>
    /// <param name="volumeName">The PlayerPrefs key for this volume.</param>
    /// <param name="change">The amount to change the volume by.</param>
    /// <param name="source">The AudioSource to modify.</param>
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
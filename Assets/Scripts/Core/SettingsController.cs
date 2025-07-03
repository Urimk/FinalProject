using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the settings UI, specifically music and sound volume sliders.
/// Handles initialization, value changes, and cleanup of slider listeners.
/// </summary>
public class SettingsController : MonoBehaviour
{
    // ==================== Constants ====================
    private const float DefaultVolume = 1f;

    // ==================== Serialized Fields ====================
    [Header("Volume Sliders")]
    [Tooltip("Slider controlling the music volume.")]
    [SerializeField] private Slider _musicVolumeSlider;

    [Tooltip("Slider controlling the sound effects volume.")]
    [SerializeField] private Slider _audioVolumeSlider;

    // ==================== Unity Lifecycle ====================
    private void Start()
    {
        // Initialize sliders with current volume levels from PlayerPrefs (or defaults)
        _musicVolumeSlider.value = PlayerPrefs.GetFloat("musicVolume", DefaultVolume);
        _audioVolumeSlider.value = PlayerPrefs.GetFloat("soundVolume", DefaultVolume);

        // Add listeners for slider value changes
        _musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        _audioVolumeSlider.onValueChanged.AddListener(SetSoundVolume);

        // Update sliders to reflect the base volume applied in SoundManager
        _musicVolumeSlider.value = SoundManager.instance.GetCurrentMusicVolume();
        _audioVolumeSlider.value = SoundManager.instance.GetCurrentSoundVolume();
    }

    // ==================== Public Methods ====================
    /// <summary>
    /// Called when the music volume slider value changes. Updates the music volume in SoundManager.
    /// </summary>
    /// <param name="value">The new slider value for music volume.</param>
    public void SetMusicVolume(float value)
    {
        // Update the SoundManager
        // Subtracts the saved PlayerPrefs value to get the delta for SoundManager
        SoundManager.instance.ChangeMusicVolume(value - PlayerPrefs.GetFloat("musicVolume", DefaultVolume));
    }

    /// <summary>
    /// Called when the sound volume slider value changes. Updates the sound volume in SoundManager.
    /// </summary>
    /// <param name="value">The new slider value for sound effects volume.</param>
    public void SetSoundVolume(float value)
    {
        // Update the SoundManager
        // Subtracts the saved PlayerPrefs value to get the delta for SoundManager
        SoundManager.instance.ChangeSoundVolume(value - PlayerPrefs.GetFloat("soundVolume", DefaultVolume));
    }

    // ==================== Unity Cleanup ====================
    /// <summary>
    /// Removes slider listeners to avoid memory leaks when the object is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        _musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
        _audioVolumeSlider.onValueChanged.RemoveListener(SetSoundVolume);
    }
}

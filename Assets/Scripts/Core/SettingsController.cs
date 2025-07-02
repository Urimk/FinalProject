using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    // ==================== Constants ====================
    private const float DefaultVolume = 1f;

    // ==================== Serialized Fields ====================
    [SerializeField] private Slider _musicVolumeSlider; // Assign the Music Volume Slider in the Inspector
    [SerializeField] private Slider _audioVolumeSlider; // Assign the Audio Volume Slider in the Inspector

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
    public void SetMusicVolume(float value)
    {
        // Update the SoundManager
        SoundManager.instance.ChangeMusicVolume(value - PlayerPrefs.GetFloat("musicVolume", DefaultVolume));
    }

    public void SetSoundVolume(float value)
    {
        // Update the SoundManager
        SoundManager.instance.ChangeSoundVolume(value - PlayerPrefs.GetFloat("soundVolume", DefaultVolume));
    }

    // ==================== Unity Cleanup ====================
    private void OnDestroy()
    {
        // Remove listeners to avoid memory leaks
        _musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
        _audioVolumeSlider.onValueChanged.RemoveListener(SetSoundVolume);
    }
}

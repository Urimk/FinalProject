using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    [SerializeField] private Slider musicVolumeSlider; // Assign the Music Volume Slider in the Inspector
    [SerializeField] private Slider audioVolumeSlider; // Assign the Audio Volume Slider in the Inspector

    private void Start()
    {
        // Initialize sliders with current volume levels from PlayerPrefs (or defaults)
        musicVolumeSlider.value = PlayerPrefs.GetFloat("musicVolume", 1f); // Default to 1
        audioVolumeSlider.value = PlayerPrefs.GetFloat("soundVolume", 1f);

        // Add listeners for slider value changes
        musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        audioVolumeSlider.onValueChanged.AddListener(SetSoundVolume);

        // Update sliders to reflect the base volume applied in SoundManager
        musicVolumeSlider.value = SoundManager.instance.GetCurrentMusicVolume();
        audioVolumeSlider.value = SoundManager.instance.GetCurrentSoundVolume();
    }

    public void SetMusicVolume(float value)
    {
        // Update the SoundManager
        SoundManager.instance.ChangeMusicVolume(value - PlayerPrefs.GetFloat("musicVolume", 1f));
    }

    public void SetSoundVolume(float value)
    {
        // Update the SoundManager
        SoundManager.instance.ChangeSoundVolume(value - PlayerPrefs.GetFloat("soundVolume", 1f));
    }

    private void OnDestroy()
    {
        // Remove listeners to avoid memory leaks
        musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
        audioVolumeSlider.onValueChanged.RemoveListener(SetSoundVolume);
    }
}

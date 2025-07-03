using System;

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays and updates the volume value as text in the UI.
/// </summary>
public class VolumeText : MonoBehaviour
{
    // === Inspector Fields ===
    [Header("Volume Display Settings")]
    [Tooltip("PlayerPrefs key for the volume value.")]
    [SerializeField] private string _volumeName;
    [Tooltip("Introductory text to display before the volume value.")]
    [SerializeField] private string _textIntro;

    // === Private State ===
    private Text _txt;

    /// <summary>
    /// Caches the Text component reference.
    /// </summary>
    private void Awake()
    {
        _txt = GetComponent<Text>();
    }

    /// <summary>
    /// Updates the volume display every frame.
    /// </summary>
    private void Update()
    {
        UpdateVolume();
    }

    /// <summary>
    /// Retrieves the volume value from PlayerPrefs and updates the text.
    /// </summary>
    private void UpdateVolume()
    {
        const float VolumeMultiplier = 100f;
        float volumeValue = PlayerPrefs.GetFloat(_volumeName) * VolumeMultiplier;
        _txt.text = _textIntro + volumeValue.ToString();
    }
}

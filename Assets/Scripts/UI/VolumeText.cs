using System;

using UnityEngine;
using UnityEngine.UI;


public class VolumeText : MonoBehaviour
{
    [SerializeField] private string _volumeName;
    [SerializeField] private string _textIntro;

    private Text _txt;

    private void Awake()
    {
        _txt = GetComponent<Text>();
    }

    private void Update()
    {
        UpdateVolume();
    }

    private void UpdateVolume()
    {
        float volumeValue = PlayerPrefs.GetFloat(_volumeName) * 100;
        _txt.text = _textIntro + volumeValue.ToString();
    }
}

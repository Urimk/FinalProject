using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance { get; private set; }
    
    private AudioSource soundSource;
    private AudioSource musicSource;

    [SerializeField] public AudioClip menuMusic;  // Serialized for Inspector
    [SerializeField] public AudioClip level1Music; // Serialized for Inspector



    private void Awake() 
    {
        soundSource = GetComponent<AudioSource>();
        musicSource = transform.GetChild(0).GetComponent<AudioSource>();
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

    public void PlaySound(AudioClip sound, GameObject caller)
    {
        if (IsObjectVisible(caller))
        {
            soundSource.PlayOneShot(sound);
        }
    }

    private bool IsObjectVisible(GameObject obj)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return false;
        
        // Check the object itself
        SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.isVisible)
        {
            return true;
        }
        
        // Check all children
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

    public void ChangeSoundVolume(float _change)
    {
        ChangeSourceVolume(1, "soundVolume", _change, soundSource);
    }
    public void ChangeMusicVolume(float _change)
    {
        ChangeSourceVolume(0.3f, "musicVolume", _change, musicSource);
    }
    private void ChangeSourceVolume(float baseVolume, string volumeName, float change, AudioSource source)
    {
        // Get the current logical volume (0 to 1) from PlayerPrefs, defaulting to 1 if not set
        float currentVolume = PlayerPrefs.GetFloat(volumeName, 1);

        // Adjust the volume
        currentVolume += change;

        // Ensure it wraps around between 0 and 1
        if (currentVolume > 1) 
        {
            currentVolume = 0;
        } 
        else if (currentVolume < 0)
        {
            currentVolume = 1;
        }

        // Set the audio source volume (scaled by baseVolume)
        source.volume = currentVolume * baseVolume;

        // Save the logical volume (not scaled)
        PlayerPrefs.SetFloat(volumeName, currentVolume);
    }
    public float GetCurrentMusicVolume()
    {
        return PlayerPrefs.GetFloat("musicVolume", 1);
    }

    public float GetCurrentSoundVolume()
    {
        return PlayerPrefs.GetFloat("soundVolume", 1);
    }

    public void ChangeMusic(AudioClip newClip)
    {
        if (musicSource.clip == newClip) return; // Prevent restarting same music

        musicSource.clip = newClip;
        musicSource.Play();
    }

}


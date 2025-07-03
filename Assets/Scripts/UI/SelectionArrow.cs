using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the selection arrow for menu navigation and interaction.
/// </summary>
public class SelectionArrow : MonoBehaviour
{
    // === Inspector Fields ===
    [Header("Menu Options")]
    [Tooltip("Array of RectTransforms representing selectable menu options.")]
    [SerializeField] private RectTransform[] _options;

    [Header("Sounds")]
    [Tooltip("Sound to play when changing selection.")]
    [SerializeField] private AudioClip _changeSound;

    [Tooltip("Sound to play when interacting with a menu option.")]
    [SerializeField] private AudioClip _interactSound;

    // === Private State ===
    private RectTransform _rect;
    private int _currentPosition;

    /// <summary>
    /// Caches the RectTransform component.
    /// </summary>
    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    /// <summary>
    /// Handles input for changing selection and interaction.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            ChangePosition(-1);
        }
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            ChangePosition(1);
        }
        if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
        {
            Interact();
        }
    }

    /// <summary>
    /// Invokes the selected option's button and plays sound.
    /// </summary>
    private void Interact()
    {
        SoundManager.instance.PlaySound(_interactSound, gameObject);
        _options[_currentPosition].GetComponent<Button>().onClick.Invoke();
    }

    /// <summary>
    /// Changes the current selection position and updates arrow position.
    /// </summary>
    private void ChangePosition(int change)
    {
        _currentPosition += change;
        if (change != 0)
        {
            SoundManager.instance.PlaySound(_changeSound, gameObject);
        }
        if (_currentPosition < 0)
        {
            _currentPosition = _options.Length - 1;
        }
        else if (_currentPosition > _options.Length - 1)
        {
            _currentPosition = 0;
        }
        _rect.position = new Vector3(_rect.position.x, _options[_currentPosition].position.y, 0);
    }
}

using UnityEngine;
using UnityEngine.UI;

public class SelectionArrow : MonoBehaviour
{
    [SerializeField] private RectTransform[] _options;
    [SerializeField] private AudioClip _changeSound;
    [SerializeField] private AudioClip _interactSound;

    private RectTransform _rect;
    private int _currentPosition;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

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

    private void Interact()
    {
        SoundManager.instance.PlaySound(_interactSound, gameObject);
        _options[_currentPosition].GetComponent<Button>().onClick.Invoke();
    }

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

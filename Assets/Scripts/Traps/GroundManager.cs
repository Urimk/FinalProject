using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

/// <summary>
/// Manages the ground, spikes, and sorting sequences for the spike sorting puzzle.
/// Handles player death, respawn, and the logic for various sorting algorithms.
/// </summary>
public class GroundManager : MonoBehaviour
{
    // === Constants ===
    private const float DefaultSpikeMoveTime = 0.4f;
    private const float DefaultSwapTime = 0.2f;
    private const int DefaultTimesToSort = 3;
    private const float SortingWaitTime = 0.8f;
    private const float SpikeMoveYOffset = 2f;
    private const float FinalSpikeMoveTime = 0.5f;
    private const int MinSortsBeforeReset = 2;
    private const int RadixDigitCount = 10;

    // === Inspector Fields ===
    [Header("Platform Settings")]
    [Tooltip("Reference to the falling platform for reset behavior.")]
    [SerializeField] private FallingPlatform _fallingPlatform;

    [Header("Sorting Settings")]
    [Tooltip("Number of times to sort the spikes before finishing.")]
    [SerializeField] private int _timesToSort = DefaultTimesToSort;

    [Tooltip("Trophy GameObject to activate when sorting is complete.")]
    [SerializeField] private GameObject _trophy;

    [Header("Spike Animation Settings")]
    [FormerlySerializedAs("SpikeMoveTime")]
    [Tooltip("Time in seconds for a spike to move up or down.")]
    [SerializeField] private float _spikeMoveTime = DefaultSpikeMoveTime;
    public float SpikeMoveTime { get => _spikeMoveTime; set => _spikeMoveTime = value; }

    [FormerlySerializedAs("SwapTime")]
    [Tooltip("Time in seconds for a spike swap animation.")]
    [SerializeField] private float _swapTime = DefaultSwapTime;
    public float SwapTime { get => _swapTime; set => _swapTime = value; }

    [Header("Spike References")]
    [FormerlySerializedAs("Spikes")]
    [Tooltip("List of all SortedSpike objects managed by this GroundManager.")]
    [SerializeField] private List<SortedSpike> _spikes = new List<SortedSpike>();
    public List<SortedSpike> Spikes { get => _spikes; set => _spikes = value; }

    [Header("UI References")]
    [FormerlySerializedAs("SortingText")]
    [Tooltip("UI Text element for displaying the current sorting algorithm.")]
    [SerializeField] private Text _sortingText;
    public Text SortingText { get => _sortingText; set => _sortingText = value; }

    // === Private State ===
    private bool _isPlayerDead = false;
    private bool _isFinishingSequence = false;
    private Coroutine _currentSortingCoroutine;
    private int _currentSortingIndex = 0;
    private List<(System.Func<IEnumerator> method, string name)> _currentSortingMethods;

    /// <summary>
    /// Initializes the ground and spike indices.
    /// </summary>
    private void Start()
    {
        InitializeGround();
    }

    /// <summary>
    /// Randomizes spike indices and assigns them to spikes.
    /// </summary>
    private void InitializeGround()
    {
        List<int> indexes = Enumerable.Range(0, Spikes.Count).ToList();
        ShuffleList(indexes);
        for (int i = 0; i < Spikes.Count; i++)
        {
            Spikes[i].SetIndex(indexes[i]);
        }
    }

    /// <summary>
    /// Handles logic when the player dies.
    /// </summary>
    public void OnPlayerDeath()
    {
        _isPlayerDead = true;
        _fallingPlatform.ResetPlatform();
        if (!_isFinishingSequence)
        {
            StopAllCoroutines();
            _currentSortingCoroutine = null;
        }
        if (SortingText != null)
        {
            SortingText.text = string.Empty;
        }
        if (!_isFinishingSequence)
        {
            foreach (var spike in Spikes)
            {
                spike.ResetToBasePosition();
            }
        }
    }

    /// <summary>
    /// Handles logic when the player respawns.
    /// </summary>
    public void OnPlayerRespawn()
    {
        _isPlayerDead = false;
    }

    /// <summary>
    /// Initiates a spike swap between two indices.
    /// </summary>
    public void SwapSpikes(int indexA, int indexB)
    {
        StartCoroutine(SwapRoutine(indexA, indexB));
    }

    /// <summary>
    /// Coroutine to swap two spikes, including animation and position logic.
    /// </summary>
    private IEnumerator SwapRoutine(int indexA, int indexB)
    {
        if (_isPlayerDead) yield break;
        SortedSpike spikeA = Spikes[indexA];
        SortedSpike spikeB = Spikes[indexB];
        bool isSpikeA = spikeA.IsActive();
        bool isSpikeB = spikeB.IsActive();
        if (isSpikeA != isSpikeB)
        {
            if (isSpikeA)
            {
                yield return StartCoroutine(spikeA.MoveSpikeY(-SpikeMoveYOffset, SpikeMoveTime));
                if (_isPlayerDead) yield break;
            }
            if (isSpikeB)
            {
                yield return StartCoroutine(spikeB.MoveSpikeY(-SpikeMoveYOffset, SpikeMoveTime));
                if (_isPlayerDead) yield break;
            }
        }
        if (_isPlayerDead) yield break;
        Vector3 spikeAnewPos = new Vector3(spikeB.transform.position.x, spikeA.transform.position.y, spikeA.transform.position.z);
        Vector3 spikeBnewPos = new Vector3(spikeA.transform.position.x, spikeB.transform.position.y, spikeB.transform.position.z);
        spikeA.transform.position = spikeAnewPos;
        spikeB.transform.position = spikeBnewPos;
        Spikes[indexA] = spikeB;
        Spikes[indexB] = spikeA;
        if (_isPlayerDead) yield break;
        if (isSpikeA != isSpikeB)
        {
            if (isSpikeA)
            {
                yield return StartCoroutine(spikeA.MoveSpikeY(SpikeMoveYOffset, SpikeMoveTime));
                if (_isPlayerDead) yield break;
            }
            if (isSpikeB)
            {
                yield return StartCoroutine(spikeB.MoveSpikeY(SpikeMoveYOffset, SpikeMoveTime));
                if (_isPlayerDead) yield break;
            }
        }
        if (_isPlayerDead) yield break;
        yield return new WaitForSeconds(SwapTime);
    }

    /// <summary>
    /// Shuffles a list in place using Fisher-Yates algorithm.
    /// </summary>
    private void ShuffleList(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }

    /// <summary>
    /// Starts the sorting sequence.
    /// </summary>
    public void StartSorting()
    {
        if (_isPlayerDead) return;
        _currentSortingCoroutine = StartCoroutine(RandomSortingSequence());
    }

    /// <summary>
    /// Coroutine to run a random sequence of sorting algorithms.
    /// </summary>
    private IEnumerator RandomSortingSequence()
    {
        if (_currentSortingMethods == null)
        {
            _currentSortingMethods = new List<(System.Func<IEnumerator> method, string name)>
            {
                (BubbleSortRoutine, "Bubble Sort"),
                (SelectionSortRoutine, "Selection Sort"),
                (() => QuickSortRoutine(0, Spikes.Count - 1), "Quick Sort"),
                (HeapSort, "Heap Sort"),
                (RadixSort, "Radix Sort")
            };
            _currentSortingMethods = _currentSortingMethods.OrderBy(x => Random.value).ToList();
            _currentSortingIndex = 0;
        }
        for (int i = _currentSortingIndex; i < _timesToSort; i++)
        {
            if (_isPlayerDead) yield break;
            yield return new WaitForSeconds(SortingWaitTime);
            if (_isPlayerDead) yield break;
            SortingText.text = _currentSortingMethods[i].name;
            yield return StartCoroutine(_currentSortingMethods[i].method());
            if (_isPlayerDead) yield break;
            _currentSortingIndex = i + 1;
            if (i < MinSortsBeforeReset)
            {
                yield return new WaitForSeconds(SortingWaitTime);
                if (_isPlayerDead) yield break;
                InitializeGround();
            }
            if (_isPlayerDead) yield break;
            SortingText.text = string.Empty;
        }
        _currentSortingIndex = 0;
        _currentSortingMethods = null;
        _isFinishingSequence = true;
        if (!_isPlayerDead)
        {
            List<Coroutine> spikeCoroutines = new List<Coroutine>();
            foreach (var spike in Spikes)
            {
                spikeCoroutines.Add(StartCoroutine(spike.MoveSpikeY(-SpikeMoveYOffset, FinalSpikeMoveTime)));
            }
            foreach (var coroutine in spikeCoroutines)
            {
                yield return coroutine;
                if (_isPlayerDead) yield break;
            }
        }
        _isFinishingSequence = false;
        _currentSortingCoroutine = null;
        _trophy.SetActive(true);
    }

    /// <summary>
    /// Performs a bubble sort on the spikes.
    /// </summary>
    private IEnumerator BubbleSortRoutine()
    {
        int n = Spikes.Count;
        bool swapped;

        do
        {
            if (_isPlayerDead) yield break;

            swapped = false;
            for (int i = 0; i < n - 1; i++)
            {
                if (_isPlayerDead) yield break;

                if (Spikes[i].GetIndex() > Spikes[i + 1].GetIndex())
                {
                    yield return StartCoroutine(SwapRoutine(i, i + 1));
                    swapped = true;
                }
            }
            n--;
        } while (swapped && !_isPlayerDead);
    }

    /// <summary>
    /// Performs a selection sort on the spikes.
    /// </summary>
    private IEnumerator SelectionSortRoutine()
    {
        int n = Spikes.Count;

        for (int i = 0; i < n - 1; i++)
        {
            if (_isPlayerDead) yield break;

            int minIndex = i;

            for (int j = i + 1; j < n; j++)
            {
                if (_isPlayerDead) yield break;

                if (Spikes[j].GetIndex() < Spikes[minIndex].GetIndex())
                {
                    minIndex = j;
                }
            }

            if (minIndex != i)
            {
                yield return StartCoroutine(SwapRoutine(i, minIndex));
            }
        }
    }

    /// <summary>
    /// Partition function for quick sort.
    /// </summary>
    private IEnumerator Partition(int low, int high, System.Action<int> callback)
    {
        if (_isPlayerDead) yield break;

        SortedSpike pivot = Spikes[high];
        int i = low - 1;

        for (int j = low; j < high; j++)
        {
            if (_isPlayerDead) yield break;

            if (Spikes[j].GetIndex() <= pivot.GetIndex())
            {
                i++;
                if (i != j)
                {
                    yield return StartCoroutine(SwapRoutine(i, j));
                }
            }
        }

        if (!(_isPlayerDead))
        {
            yield return StartCoroutine(SwapRoutine(i + 1, high));
            callback(i + 1);
        }
    }

    /// <summary>
    /// Performs a quick sort on the spikes.
    /// </summary>
    private IEnumerator QuickSortRoutine(int low, int high)
    {
        if (low < high && !_isPlayerDead)
        {
            int pivotIndex = -1;
            yield return StartCoroutine(Partition(low, high, result => pivotIndex = result));

            if (!(_isPlayerDead))
            {
                yield return StartCoroutine(QuickSortRoutine(low, pivotIndex - 1));
                yield return StartCoroutine(QuickSortRoutine(pivotIndex + 1, high));
            }
        }
    }

    /// <summary>
    /// Performs a radix sort on the spikes.
    /// </summary>
    private IEnumerator RadixSort()
    {
        int maxIndex = Spikes.Count;
        int exp = 1;

        while (maxIndex / exp > 0 && !_isPlayerDead)
        {
            yield return StartCoroutine(CountingSort(exp));
            exp *= 10;
        }
    }

    /// <summary>
    /// Performs a counting sort for radix sort.
    /// </summary>
    private IEnumerator CountingSort(int exp)
    {
        if (_isPlayerDead) yield break;

        int n = Spikes.Count;
        List<SortedSpike> output = new List<SortedSpike>(new SortedSpike[n]);
        int[] count = new int[RadixDigitCount];

        for (int i = 0; i < n; i++)
        {
            if (_isPlayerDead) yield break;

            int digit = (Spikes[i].GetIndex() / exp) % RadixDigitCount;
            count[digit]++;
        }

        for (int i = 1; i < RadixDigitCount; i++)
        {
            count[i] += count[i - 1];
        }

        for (int i = n - 1; i >= 0; i--)
        {
            if (_isPlayerDead) yield break;

            int digit = (Spikes[i].GetIndex() / exp) % RadixDigitCount;
            output[count[digit] - 1] = Spikes[i];
            count[digit]--;
        }

        for (int i = 0; i < n; i++)
        {
            if (_isPlayerDead) yield break;

            if (Spikes[i] != output[i])
            {
                int targetIndex = Spikes.IndexOf(output[i]);
                yield return StartCoroutine(SwapRoutine(i, targetIndex));
            }
        }
    }

    /// <summary>
    /// Performs a heap sort on the spikes.
    /// </summary>
    private IEnumerator HeapSort()
    {
        int n = Spikes.Count;

        for (int i = n / 2 - 1; i >= 0; i--)
        {
            if (_isPlayerDead) yield break;
            yield return StartCoroutine(Heapify(n, i));
        }

        for (int i = n - 1; i > 0; i--)
        {
            if (_isPlayerDead) yield break;

            yield return StartCoroutine(SwapRoutine(0, i));
            yield return StartCoroutine(Heapify(i, 0));
        }
    }

    /// <summary>
    /// Heapify function for heap sort.
    /// </summary>
    private IEnumerator Heapify(int n, int i)
    {
        if (_isPlayerDead) yield break;

        int largest = i;
        int left = 2 * i + 1;
        int right = 2 * i + 2;

        if (left < n && Spikes[left].GetIndex() > Spikes[largest].GetIndex())
        {
            largest = left;
        }

        if (right < n && Spikes[right].GetIndex() > Spikes[largest].GetIndex())
        {
            largest = right;
        }

        if (largest != i && !(_isPlayerDead))
        {
            yield return StartCoroutine(SwapRoutine(i, largest));
            yield return StartCoroutine(Heapify(n, largest));
        }
    }
}

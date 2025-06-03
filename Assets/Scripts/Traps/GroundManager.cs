using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public class GroundManager : MonoBehaviour
{
    public float spikeMoveTime = 0.4f;
    public float swapTime = 0.2f;
    [SerializeField] private FallingPlatform fallingPlatform;
    [SerializeField] private GameObject trophy;

    public List<SortedSpike> spikes = new List<SortedSpike>();
    public Text sortingText; // Reference to your TextMeshPro UI Text

    // Variables to handle player death and sorting state
    private bool isPlayerDead = false;
    private bool isFinishingSequence = false; // Track if we're in the final spike lowering phase

    private Coroutine currentSortingCoroutine;
    private int currentSortingIndex = 0;
    private List<(System.Func<IEnumerator> method, string name)> currentSortingMethods;

    void Start()
    {
        InitializeGround();
    }

    void InitializeGround()
    {
        List<int> indexes = Enumerable.Range(0, spikes.Count).ToList();
        ShuffleList(indexes); // Shuffle indexes so they are not sorted

        for (int i = 0; i < spikes.Count; i++)
        {
            spikes[i].SetIndex(indexes[i]);
        }
    }

    // Method to be called when player dies
    public void OnPlayerDeath()
    {
        isPlayerDead = true;
        fallingPlatform.ResetPlatform();
        if (!isFinishingSequence)
        {
            // Stop ALL coroutines on this MonoBehaviour to ensure nothing continues
            StopAllCoroutines();
            currentSortingCoroutine = null;
        }
        
        // Clear the sorting text
            if (sortingText != null)
            {
                sortingText.text = "";
            }
        
        // Optional: Reset any spikes that might be in motion to a consistent state
        if (!isFinishingSequence)
        {
            foreach (var spike in spikes)
            {
                spike.ResetToBasePosition();
            }
        }
    }

    // Method to be called when player respawns or game restarts
    public void OnPlayerRespawn()
    {
        isPlayerDead = false;
    }

    public void SwapSpikes(int indexA, int indexB)
    {
        StartCoroutine(SwapRoutine(indexA, indexB));
    }

    IEnumerator SwapRoutine(int indexA, int indexB)
{
    // Check if player is dead before proceeding
    if (isPlayerDead) yield break;

    SortedSpike spikeA = spikes[indexA];
    SortedSpike spikeB = spikes[indexB];

    bool isSpikeA = spikeA.IsActive();
    bool isSpikeB = spikeB.IsActive();

    if (isSpikeA != isSpikeB)
    {
        // Lower active spike before swapping
        if (isSpikeA) 
        {
            yield return StartCoroutine(spikeA.MoveSpikeY(-2, spikeMoveTime));
            if (isPlayerDead) yield break; // Check after each major operation
        }
        if (isSpikeB) 
        {
            yield return StartCoroutine(spikeB.MoveSpikeY(-2, spikeMoveTime));
            if (isPlayerDead) yield break; // Check after each major operation
        }
    }

    // Check before swapping positions
    if (isPlayerDead) yield break;

    // Swap positions
    Vector3 spikeAnewPos = new Vector3(spikeB.transform.position.x, spikeA.transform.position.y, spikeA.transform.position.z);
    Vector3 spikeBnewPos = new Vector3(spikeA.transform.position.x, spikeB.transform.position.y, spikeB.transform.position.z);

    spikeA.transform.position = spikeAnewPos;
    spikeB.transform.position = spikeBnewPos;

    // Swap list order
    spikes[indexA] = spikeB;
    spikes[indexB] = spikeA;

    // Check before raising spikes
    if (isPlayerDead) yield break;

    // Raise active spike after swapping
    if (isSpikeA != isSpikeB)
    {
        if (isSpikeA) 
        {
            yield return StartCoroutine(spikeA.MoveSpikeY(2, spikeMoveTime));
            if (isPlayerDead) yield break; // Check after each major operation
        }
        if (isSpikeB) 
        {
            yield return StartCoroutine(spikeB.MoveSpikeY(2, spikeMoveTime));
            if (isPlayerDead) yield break; // Check after each major operation
        }
    }

    // Check before final wait
    if (isPlayerDead) yield break;

    yield return new WaitForSeconds(swapTime);
}

    void ShuffleList(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }

    public void StartSorting()
    {
        // Don't start sorting if player is dead
        if (isPlayerDead) return;

        currentSortingCoroutine = StartCoroutine(RandomSortingSequence());
    }

    IEnumerator RandomSortingSequence()
    {
        // Initialize sorting methods if not already done or if starting fresh
        if (currentSortingMethods == null)
        {
            currentSortingMethods = new List<(System.Func<IEnumerator> method, string name)>
            {
                (BubbleSortRoutine, "Bubble Sort"),
                (SelectionSortRoutine, "Selection Sort"),
                (() => QuickSortRoutine(0, spikes.Count - 1), "Quick Sort"),
                (HeapSort, "Heap Sort"),
                (RadixSort, "Radix Sort")
            };

            // Shuffle the list
            currentSortingMethods = currentSortingMethods.OrderBy(x => Random.value).ToList();
            currentSortingIndex = 0;
        }

        // Continue from where we left off
        for (int i = currentSortingIndex; i < 3; i++)
        {
            if (isPlayerDead) yield break;

            yield return new WaitForSeconds(0.8f);

            if (isPlayerDead) yield break;

            sortingText.text = currentSortingMethods[i].name;

            // Start the sorting coroutine
            yield return StartCoroutine(currentSortingMethods[i].method());

            if (isPlayerDead) yield break;

            currentSortingIndex = i + 1;

            if (i < 2)
            {
                yield return new WaitForSeconds(0.8f);
                if (isPlayerDead) yield break;
                InitializeGround();
            }

            if (isPlayerDead) yield break;
            sortingText.text = "";
        }

        // Reset for next cycle
        currentSortingIndex = 0;
        currentSortingMethods = null;

        isFinishingSequence = true;

        // Move all spikes down simultaneously
        if (!isPlayerDead)
        {
            List<Coroutine> spikeCoroutines = new List<Coroutine>();
            foreach (var spike in spikes)
            {
                spikeCoroutines.Add(StartCoroutine(spike.MoveSpikeY(-2, 0.5f)));
            }

            // Wait for all spikes to finish moving down
            foreach (var coroutine in spikeCoroutines)
            {
                yield return coroutine;
                if (isPlayerDead) yield break;
            }
        }
        isFinishingSequence = false;
        currentSortingCoroutine = null;
        trophy.SetActive(true);
    }

    IEnumerator BubbleSortRoutine()
    {
        int n = spikes.Count;
        bool swapped;

        do
        {
            if (isPlayerDead) yield break;
            
            swapped = false;
            for (int i = 0; i < n - 1; i++)
            {
                if (isPlayerDead) yield break;
                
                if (spikes[i].GetIndex() > spikes[i + 1].GetIndex())
                {
                    yield return StartCoroutine(SwapRoutine(i, i + 1));
                    swapped = true;
                }
            }
            n--;
        } while (swapped && !isPlayerDead);
    }

    IEnumerator SelectionSortRoutine()
    {
        int n = spikes.Count;

        for (int i = 0; i < n - 1; i++)
        {
            if (isPlayerDead) yield break;
            
            int minIndex = i;

            for (int j = i + 1; j < n; j++)
            {
                if (isPlayerDead) yield break;
                
                if (spikes[j].GetIndex() < spikes[minIndex].GetIndex())
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

    IEnumerator Partition(int low, int high, System.Action<int> callback)
    {
        if (isPlayerDead) yield break;
        
        SortedSpike pivot = spikes[high];
        int i = low - 1; 

        for (int j = low; j < high; j++)
        {
            if (isPlayerDead) yield break;
            
            if (spikes[j].GetIndex() <= pivot.GetIndex())
            {
                i++;
                if (i != j)
                {
                    yield return StartCoroutine(SwapRoutine(i, j));
                }
            }
        }

        if (!isPlayerDead)
        {
            yield return StartCoroutine(SwapRoutine(i + 1, high));
            callback(i + 1);
        }
    }

    IEnumerator QuickSortRoutine(int low, int high)
    {
        if (low < high && !isPlayerDead)
        {
            int pivotIndex = -1;
            yield return StartCoroutine(Partition(low, high, result => pivotIndex = result));

            if (!isPlayerDead)
            {
                yield return StartCoroutine(QuickSortRoutine(low, pivotIndex - 1));
                yield return StartCoroutine(QuickSortRoutine(pivotIndex + 1, high));
            }
        }
    }

    IEnumerator RadixSort()
    {
        int maxIndex = spikes.Count;
        int exp = 1;

        while (maxIndex / exp > 0 && !isPlayerDead)
        {
            yield return StartCoroutine(CountingSort(exp));
            exp *= 10;
        }
    }

    IEnumerator CountingSort(int exp)
    {
        if (isPlayerDead) yield break;
        
        int n = spikes.Count;
        List<SortedSpike> output = new List<SortedSpike>(new SortedSpike[n]);
        int[] count = new int[10];

        for (int i = 0; i < n; i++)
        {
            if (isPlayerDead) yield break;
            
            int digit = (spikes[i].GetIndex() / exp) % 10;
            count[digit]++;
        }

        for (int i = 1; i < 10; i++)
        {
            count[i] += count[i - 1];
        }

        for (int i = n - 1; i >= 0; i--)
        {
            if (isPlayerDead) yield break;
            
            int digit = (spikes[i].GetIndex() / exp) % 10;
            output[count[digit] - 1] = spikes[i];
            count[digit]--;
        }

        for (int i = 0; i < n; i++)
        {
            if (isPlayerDead) yield break;
            
            if (spikes[i] != output[i])
            {
                int targetIndex = spikes.IndexOf(output[i]);
                yield return StartCoroutine(SwapRoutine(i, targetIndex));
            }
        }
    }

    IEnumerator HeapSort()
    {
        int n = spikes.Count;

        for (int i = n / 2 - 1; i >= 0; i--)
        {
            if (isPlayerDead) yield break;
            yield return StartCoroutine(Heapify(n, i));
        }

        for (int i = n - 1; i > 0; i--)
        {
            if (isPlayerDead) yield break;
            
            yield return StartCoroutine(SwapRoutine(0, i));
            yield return StartCoroutine(Heapify(i, 0));
        }
    }

    IEnumerator Heapify(int n, int i)
    {
        if (isPlayerDead) yield break;
        
        int largest = i;
        int left = 2 * i + 1;
        int right = 2 * i + 2;

        if (left < n && spikes[left].GetIndex() > spikes[largest].GetIndex())
        {
            largest = left;
        }

        if (right < n && spikes[right].GetIndex() > spikes[largest].GetIndex())
        {
            largest = right;
        }

        if (largest != i && !isPlayerDead)
        {
            yield return StartCoroutine(SwapRoutine(i, largest));
            yield return StartCoroutine(Heapify(n, largest));
        }
    }
}
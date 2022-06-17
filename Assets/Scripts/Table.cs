using System.Collections;
using UnityEngine;
using EPOOutline;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class Table : MonoBehaviour
{
    public SitPos[] sitPositions;
    public GameObject foodSpawnPoint;

    [Space(2)]
    [Header("UI")]
    public GameObject waitTimerUI;
    public Image waitFill;
    public TextMeshPro tableNumber;

    private Customer _currentCustomer;
    private Outlinable _outlinable;
    private GameManager _gameManager;
    private bool _init = false;
    private List<SitPos> _availableSits = new List<SitPos>();

    // properties
    public bool IsBusy { get; private set; }
    public int TableNumber { get; private set; }

    [System.Serializable]
    public class SitPos
    {
        public GameObject sitPos;
        public GameObject standPos;
    }

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        if (_init)
            return;

        _outlinable = gameObject.AddComponent<Outlinable>();
        _outlinable.AddAllChildRenderersToRenderingList();
        _gameManager = FindObjectOfType<GameManager>();
        _availableSits = sitPositions.ToList();

        HideTimer();
        _outlinable.enabled = false;

        _init = true;
    }

    public void SetTableNumber(int num)
    {
        tableNumber.text = num.ToString();
        TableNumber = num;
    }

    public bool CheckTableNumber(int num)
    {
        if (TableNumber == num)
        {
            return true;
        }

        StartCoroutine(BusyOrNotEnoughSpaceWarnCoroutine());

        return false;
    }

    public Table Select(int customersCount)
    {
        if (IsBusy || customersCount > sitPositions.Length)
        {
            StartCoroutine(BusyOrNotEnoughSpaceWarnCoroutine());
            return null;
        }

        StartCoroutine(AvailableWarnCoroutine());

        return this;
    }

    private IEnumerator AvailableWarnCoroutine()
    {
        _outlinable.enabled = true;

        var lerpSpeed = 4f;
        var errorColor = _gameManager.okayOutlineColor;
        errorColor.a = 0;

        while (errorColor.a < 1)
        {
            errorColor.a = Mathf.MoveTowards(errorColor.a, 1, Time.deltaTime * lerpSpeed);
            _outlinable.OutlineParameters.Color = errorColor;
            yield return null;
        }

        while (errorColor.a > 0)
        {
            errorColor.a = Mathf.MoveTowards(errorColor.a, 0, Time.deltaTime * lerpSpeed);
            _outlinable.OutlineParameters.Color = errorColor;
            yield return null;
        }

        _outlinable.enabled = false;
    }

    private IEnumerator BusyOrNotEnoughSpaceWarnCoroutine()
    {
        _outlinable.enabled = true;

        var lerpSpeed = 4f;
        var errorColor = _gameManager.errorOutlineColor;
        errorColor.a = 0;

        while (errorColor.a < 1)
        {
            errorColor.a = Mathf.MoveTowards(errorColor.a, 1, Time.deltaTime * lerpSpeed);
            _outlinable.OutlineParameters.Color = errorColor;
            yield return null;
        }

        while (errorColor.a > 0)
        {
            errorColor.a = Mathf.MoveTowards(errorColor.a, 0, Time.deltaTime * lerpSpeed);
            _outlinable.OutlineParameters.Color = errorColor;
            yield return null;
        }

        _outlinable.enabled = false;
    }

    public void SetBusyMode(Customer customer)
    {
        _currentCustomer = customer;
        IsBusy = true;
    }

    public void StartTimer(float busyTime)
    {
        StartCoroutine(BusyModeCoroutine(busyTime));
    }

    private IEnumerator BusyModeCoroutine(float busyTime)
    {
        var time = busyTime;
        var currentTime = 0f;

        while (currentTime < time)
        {
            currentTime += Time.deltaTime;
            ShowTimer(currentTime / time);

            yield return null;
        }

        HideTimer();
        _currentCustomer.StandUp();
    }

    public void LeaveTable()
    {
        _currentCustomer = null;
        IsBusy = false;
        _availableSits = sitPositions.ToList();
    }

    private void ShowTimer(float fillAmount)
    {
        waitTimerUI.SetActive(true);
        waitFill.fillAmount = fillAmount;
        waitFill.color = _gameManager.tableTimeColor;
    }

    private void HideTimer()
    {
        waitTimerUI.SetActive(false);
    }

    public SitPos GetAvailableSitPosition()
    {
        if (_availableSits.Count == 0)
            return null;

        var rand = Random.Range(0, _availableSits.Count);
        var selectedSit = _availableSits[rand];
        _availableSits.RemoveAt(rand);

        return selectedSit;
    }

    public void SpawnFoods()
    {
        if (_gameManager.foodPrefabs.Length == 0 || foodSpawnPoint == null)
            return;

        var foodRand = Random.Range(0, _gameManager.foodPrefabs.Length);
        Instantiate(_gameManager.foodPrefabs[foodRand], foodSpawnPoint.transform.position, foodSpawnPoint.transform.rotation, foodSpawnPoint.transform);
    }

    public void DestroyFoods()
    {
        if (foodSpawnPoint == null)
            return;

        foreach (Transform t in foodSpawnPoint.transform)
            Destroy(t.gameObject);
    }
}
using System.Collections;
using UnityEngine;
using EPOOutline;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class Table : MonoBehaviour
{
    public SitPos[] sitPositions;

    [Space(2)]
    [Header("UI")]
    public GameObject waitTimerUI;
    public Image waitFill;

    private Customer _currentCustomer;
    private Outlinable _outlinable;
    private GameManager _gameManager;
    private bool _init = false;
    private List<SitPos> _availableSits = new List<SitPos> ();

    // properties
    public bool IsBusy { get; private set; }

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
            ShowTimer(currentTime / time, _gameManager.timerFillGradient.Evaluate(currentTime / time));

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

    private void ShowTimer(float fillAmount, Color color)
    {
        waitTimerUI.SetActive(true);
        waitFill.fillAmount = fillAmount;
        waitFill.color = color;
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
}
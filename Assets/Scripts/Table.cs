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
    [Header("Filters")]
    public bool isReserved;
    [Space(1)]
    public bool isNumbered;
    public int tableNumber;
    [Space(1)]
    public bool isFoodFiltered;
    public FoodType foodType;

    [Space(2)]
    [Header("UI")]
    public Image waitFill;
    public TextMeshPro tableNumberText;
    public GameObject reserveTableMark;
    public SpriteRenderer foodTypeImage;

    private Customer _currentCustomer;
    private Outlinable _outlinable;
    private GameManager _gameManager;
    private bool _init = false;
    private List<SitPos> _availableSits = new List<SitPos>();

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
        ReserveTheTables();
        NumberTheTables();
        FoodFilterTheTables();

        _init = true;
    }

    // Filters
    private void ReserveTheTables()
    {
        if (!isReserved)
        {
            reserveTableMark.gameObject.SetActive(false);
            return;
        }

        reserveTableMark.gameObject.SetActive(true);
    }

    private void NumberTheTables()
    {
        if (!isNumbered)
        {
            tableNumberText.gameObject.SetActive(false);
            return;
        }

        tableNumberText.gameObject.SetActive(true);
        tableNumberText.text = tableNumber.ToString();
    }

    private void FoodFilterTheTables()
    {
        if (!isFoodFiltered)
        {
            foodTypeImage.gameObject.SetActive(false);
            return;
        }

        foodTypeImage.gameObject.SetActive(true);
        foodTypeImage.sprite = _gameManager.foodFilters.SingleOrDefault(x => x.FoodType == foodType).foodIcon;
    }

    public bool CheckTableNumber(int num)
    {
        if ( tableNumber == num)
        {
            return true;
        }

        StartCoroutine(BusyOrNotEnoughSpaceWarnCoroutine());

        return false;
    }

    public bool CheckReservedTable()
    {
        if (isReserved)
        {
            return true;
        }

        StartCoroutine(BusyOrNotEnoughSpaceWarnCoroutine());

        return false;
    }

    public bool CheckFoodFilter(FoodType foodType)
    {
        if(this.foodType == foodType)
        {
            return true;
        }

        StartCoroutine(BusyOrNotEnoughSpaceWarnCoroutine());

        return false;
    }
    ///

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

    public IEnumerator BusyOrNotEnoughSpaceWarnCoroutine()
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
        waitFill.gameObject.SetActive(true);
        waitFill.fillAmount = fillAmount;
        waitFill.color = _gameManager.tableTimeColor;
    }

    private void HideTimer()
    {
        waitFill.gameObject.SetActive(false);
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
        var foods = new List<GameObject>();

        if (isFoodFiltered)
        {
            foods = _gameManager.foodFilters.SingleOrDefault(x => x.FoodType == foodType).foodPrefabs.ToList();
        }
        else
        {
            foreach (var foodType in _gameManager.foodFilters)
                foods.AddRange(foodType.foodPrefabs);
        }

        if (foods.Count == 0 || foodSpawnPoint == null)
            return;

        var foodRand = Random.Range(0, foods.Count);
        Instantiate(foods[foodRand], foodSpawnPoint.transform.position, foodSpawnPoint.transform.rotation, foodSpawnPoint.transform);
    }

    public void DestroyFoods()
    {
        if (foodSpawnPoint == null)
            return;

        foreach (Transform t in foodSpawnPoint.transform)
            Destroy(t.gameObject);
    }
}
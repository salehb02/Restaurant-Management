using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using EPOOutline;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;

public class Customer : MonoBehaviour
{
    public enum CustomerType { Single = 1, Couple = 2, Triple = 3, Quadruple = 4, VIP }
    public CustomerType customerType;

    public GameObject pivot;

    [Header("Using Table Mode")]
    public float eatTime = 15f;

    [Space(2)]
    [Header("Boredom settings")]
    public float idleTime = 2f;
    public float leaveTime = 10f;

    [Space(2)]
    [Header("Animation")]
    public int numberOfAngryModes;
    public float movementLerpMuliplier = 1.5f;
    public float sitDownAnimationLength;
    public float standUpAnimationLength;
    public float startEatDelay;

    [Space(2)]
    [Header("UI")]
    public GameObject customerCanvas;
    [Space(2)]
    public GameObject waitTimerUI;
    public Image waitFill;
    public bool showMoney;
    public TextMeshProUGUI moneyAmountText;
    [Space(2)]
    public bool showHorde;
    public TextMeshProUGUI hordeCountText;
    [Space(2)]
    public GameObject tableNumberFilter;
    public TextMeshProUGUI tableNumberFilterText;
    [Space(2)]
    public GameObject reserveTableFilter;
    [Space(2)]
    public GameObject foodFilter;
    public Image foodFilterImage;

    private Coroutine _waitCoroutine;
    private int _prizeAmount;
    private Vector3 _exitPosition;
    private GameObject _sitPosition;
    private GameObject _standPosition;
    private bool _init = false;
    private bool _leaving = false;
    private bool _goingToSit = false;

    // filters
    private bool _wantsNumbererdTable = false;
    private int _tableNumberFilter;
    private bool _wantsReservedTable;
    private bool _wantsSpecificFood;
    private FoodType _specificFoodType;

    // properties
    public bool IsSelected { get; private set; }
    public bool IsSelectable { get; private set; }
    public bool IsBusy { get; private set; }
    public bool IsVIP { get; private set; }
    public Table TargetTable { get; private set; }
    public bool IsFollower { get; set; }
    public Customer ToFollow { get; set; }
    public List<Follower> Followers { get; set; } = new List<Follower>();
    public GameManager.Gate FilledGate { get; set; }

    // components
    private NavMeshAgent _agent;
    private Camera _camera;
    private Outlinable _outlinable;
    private Animator _animator;
    private GameManager _gameManager;
    private Table _currentTable;

    [System.Serializable]
    public class Follower
    {
        public Customer follower;
        public Vector3 offset;

        public Follower(Customer follower, Vector3 offset)
        {
            this.follower = follower;
            this.offset = offset;
        }
    }

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        if (_init)
            return;

        // get components
        _animator = GetComponent<Animator>();
        _camera = Camera.main;
        _agent = GetComponent<NavMeshAgent>();
        _gameManager = FindObjectOfType<GameManager>();

        // add outline
        _outlinable = gameObject.AddComponent<Outlinable>();
        _outlinable.AddAllChildRenderersToRenderingList();

        customerCanvas.gameObject.SetActive(true);
        reserveTableFilter.gameObject.SetActive(false);
        IsSelectable = true;
        UnSelect();
        HideTimer();
        GetCustomerPrize();
        HideTableNumberFilter();
        HideFoodFilterUI();

        if (showHorde)
        {
            hordeCountText.gameObject.SetActive(true);
            hordeCountText.text = "<sprite index=2>" + (Followers.Count + 1).ToString();
        }
        else
        {
            hordeCountText.gameObject.SetActive(false);
        }

        _waitCoroutine = StartCoroutine(EnterWaitModeCoroutine());

        _init = true;
    }

    private void Update()
    {
        if (_gameManager.useTouch)
        {
            if (IsSelected)
            {
                if (Input.touchCount > 0)
                {
                    var touch = Input.touches[0];

                    if (Physics.Raycast(_camera.ScreenPointToRay(touch.position), out var hit, Mathf.Infinity))
                    {
                        SelectTable(hit);
                    }
                }
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (IsSelected)
                {
                    if (Physics.Raycast(_camera.ScreenPointToRay(Input.mousePosition), out var hit, Mathf.Infinity))
                    {
                        SelectTable(hit);
                    }
                }
            }
        }

        // set animation blend value by agent speed
        _animator.SetFloat("Speed", Mathf.Lerp(_animator.GetFloat("Speed"), _agent.velocity.magnitude, Time.deltaTime * movementLerpMuliplier));

        // check distance to exit 
        if (_leaving)
        {
            if (Vector3.Distance(transform.position, _exitPosition) <= 0.5f)
            {
                if (!IsFollower)
                    _gameManager.RemoveFromWaiters(this);

                Destroy(gameObject);
            }
        }

        if (_goingToSit)
        {
            if (Vector3.Distance(transform.position, _standPosition.transform.position) <= 0.2f)
            {
                SitDown();
                _goingToSit = false;
            }
        }
    }

    private void SelectTable(RaycastHit hit)
    {
        var table = hit.transform.GetComponentInParent<Table>();

        if (!table)
            return;

        var checkFilters = table.CheckTheFilters(_wantsNumbererdTable, _tableNumberFilter, _wantsReservedTable, _wantsSpecificFood, _specificFoodType);

        if (checkFilters == false)
            return;

        if (table.Select((int)customerType) == null)
            return;

        _currentTable = table;
        var sitPostion = table.GetAvailableSitPosition();
        _sitPosition = sitPostion.sitPos;
        _standPosition = sitPostion.standPos;

        _agent.SetDestination(_standPosition.transform.position);
        EndWaitMode();
        table.SetBusyMode(this);
        UnSelect();

        if (_gameManager.SelectedTarget == this)
            _gameManager.SelectedTarget = null;

        if (_gameManager.useGateWaitPosition && FilledGate != null)
        {
            _gameManager.AddAvailableGate(FilledGate);
            FilledGate = null;
        }

        _goingToSit = true;

        // Order followers
        foreach (var follower in Followers)
        {
            var sitPostionF = table.GetAvailableSitPosition();
            follower.follower._sitPosition = sitPostionF.sitPos;
            follower.follower._standPosition = sitPostionF.standPos;

            follower.follower._agent.SetDestination(follower.follower._standPosition.transform.position);

            follower.follower._goingToSit = true;
        }
    }

    private void GetCustomerPrize()
    {
        switch (customerType)
        {
            case CustomerType.Single:
                _prizeAmount = Random.Range(5, 20);
                break;
            case CustomerType.Couple:
                _prizeAmount = Random.Range(5, 20);
                break;
            case CustomerType.Triple:
                _prizeAmount = Random.Range(6, 22);
                break;
            case CustomerType.Quadruple:
                _prizeAmount = Random.Range(10, 25);
                break;
            case CustomerType.VIP:
                _prizeAmount = Random.Range(40, 50);
                IsVIP = true;
                break;
            default:
                break;
        }

        if (showMoney)
        {
            moneyAmountText.gameObject.SetActive(true);
            moneyAmountText.text = "<sprite index=0>" + _prizeAmount;
        }
        else
        {
            moneyAmountText.gameObject.SetActive(false);
        }
    }

    public void Select()
    {
        IsSelected = true;
        SelectOutline();

        foreach (var follower in Followers)
            follower.follower.SelectOutline();
    }

    private void SelectOutline()
    {
        _outlinable.OutlineParameters.Color = _gameManager.okayOutlineColor;
        _outlinable.enabled = true;
    }

    public void UnSelect()
    {
        IsSelected = false;
        UnSelectOutline();

        foreach (var follower in Followers)
            follower.follower.UnSelectOutline();
    }

    private void UnSelectOutline()
    {
        _outlinable.enabled = false;
    }

    public void MoveToLocation(Vector3 pos)
    {
        _agent.SetDestination(pos);

        foreach (var follower in Followers)
            follower.follower.MoveToLocation(pos + follower.offset);
    }

    public void SetExitPosition(Vector3 pos)
    {
        _exitPosition = pos;

        foreach (var follower in Followers)
            follower.follower.SetExitPosition(pos + follower.offset);
    }

    private void SitDown()
    {
        pivot.transform.SetParent(_sitPosition.transform);
        _agent.enabled = false;
        transform.rotation = _standPosition.transform.rotation;
        transform.position = _standPosition.transform.position;

        _animator.SetTrigger("Sit Down");

        pivot.transform.DOLocalMove(Vector3.zero, sitDownAnimationLength).OnComplete(() =>
        {
            if (!IsFollower)
            {
                _currentTable.StartTimer(eatTime);
            }

            StartCoroutine(EatAnimationCoroutine());
        });

        pivot.transform.DOLocalRotateQuaternion(Quaternion.identity, sitDownAnimationLength);
    }

    public void StandUp()
    {
        if (!IsFollower)
            _currentTable.LeaveTable();

        pivot.transform.SetParent(transform);
        _animator.SetTrigger("Stand Up");

        pivot.transform.DOLocalMove(Vector3.zero, standUpAnimationLength).OnComplete(() =>
        {
            if (!IsFollower)
                _gameManager.AddMoney(_prizeAmount);
            _agent.enabled = true;
            Leave();
        });

        pivot.transform.DOLocalRotateQuaternion(Quaternion.identity, standUpAnimationLength);

        foreach (var follower in Followers)
            follower.follower.StandUp();
    }

    private IEnumerator EatAnimationCoroutine()
    {
        yield return new WaitForSeconds(startEatDelay);

        _animator.SetTrigger("Eat");

        yield return new WaitForSeconds(eatTime - startEatDelay - 2f);

        _animator.SetTrigger("Give Up Eating");
    }

    public void PlayAngryAnimation()
    {
        _animator.SetFloat("Angry Mode", Random.Range(0, numberOfAngryModes + 1));
        _animator.SetTrigger("Be Angry");
    }

    public void EndWaitMode()
    {
        if (!IsFollower)
            _gameManager.RemoveFromWaiters(this);

        StopCoroutine(_waitCoroutine);
        HideTimer();
        customerCanvas.gameObject.SetActive(false);

        foreach (var follower in Followers)
            follower.follower.EndWaitMode();
    }

    bool playedAngryAnimation = false;

    private IEnumerator EnterWaitModeCoroutine()
    {
        // idle wait
        yield return new WaitForSeconds(idleTime);

        var timer = 0f;
        var angryAnimationTime = Random.Range(leaveTime / 3f, leaveTime / 1.5f);

        // get bored
        while (timer < leaveTime)
        {
            timer += Time.deltaTime;

            if (timer >= angryAnimationTime && !playedAngryAnimation)
            {
                playedAngryAnimation = true;
                PlayAngryAnimation();
            }

            ShowTimer(timer / leaveTime, _gameManager.timerFillGradient.Evaluate(timer / leaveTime));

            yield return null;
        }

        // leave place
        HideTimer();
        UnSelect();
        Leave();
    }

    public void Leave()
    {
        IsSelectable = false;
        customerCanvas.gameObject.SetActive(false);
        _agent.SetDestination(_exitPosition);
        _leaving = true;

        if (_gameManager.useGateWaitPosition && FilledGate != null)
        {
            _gameManager.AddAvailableGate(FilledGate);
            FilledGate = null;
        }
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

    public void FollowCustomer(Customer toFollow, Follower follower)
    {
        IsFollower = true;
        ToFollow = toFollow;
        toFollow.Followers.Add(follower);

        customerCanvas.gameObject.SetActive(false);
    }

    // Filters
    public void SetTableNumberFilter(int number)
    {
        tableNumberFilter.SetActive(true);
        tableNumberFilterText.text = number.ToString();

        _wantsNumbererdTable = true;
        _tableNumberFilter = number;
    }

    private void HideTableNumberFilter()
    {
        tableNumberFilter.SetActive(false);
    }

    public void ReserveTable()
    {
        reserveTableFilter.gameObject.SetActive(true);
        _wantsReservedTable = true;
    }

    public void SetFoodFilter(FoodType foodType)
    {
        _wantsSpecificFood = true;
        _specificFoodType = foodType;

        foodFilter.gameObject.SetActive(true);
        foodFilterImage.sprite = _gameManager.foodFilters.SingleOrDefault(x => x.FoodType == foodType).foodIcon;
    }

    private void HideFoodFilterUI()
    {
        foodFilter.gameObject.SetActive(false);
    }
}